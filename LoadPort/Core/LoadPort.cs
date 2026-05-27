using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using LoadPort.Models;
using LoadPort.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZCCommunication.Profinet.Melsec;

namespace LoadPort
{
    public class Loadport : SerialManager
    {
        public string Name { get; private set; }
        public string PortName { get; private set; }
        public string Result { get; set; }

        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        private readonly ConcurrentDictionary<
            string,
            TaskCompletionSource<string>
        > _pendingReplies = new ConcurrentDictionary<string, TaskCompletionSource<string>>();

        public LoadPortConfig LoadPortConfig { get; set; }

        public List<PlcCommandConfig> Commands { get; set; } = new List<PlcCommandConfig>();
        public bool Bypass => LoadPortConfig?.Bypass ?? false;

        public Loadport(
            string name,
            string portName,
            int baudRate,
            int dataBits,
            StopBits stopBits,
            Parity parity
        )
            : base(name, portName, baudRate, dataBits, stopBits, parity)
        {
            Name = name;
            PortName = portName;
        }

        protected override void HandleReceivedMessage(string message)
        {
            foreach (var kvp in _pendingReplies)
            {
                foreach (var expected in kvp.Key.Split('|'))
                {
                    if (message.Contains(expected))
                    {
                        kvp.Value.TrySetResult(message);
                        return;
                    }
                }
            }
         
            foreach(var replys in Commands)
            {
                if (replys.Command == "" && replys.ExpectedReply!="")
                {
                    if (message.Contains(replys.ExpectedReply))
                    {
                        Form1.plcMcNet.Write(replys.ReturnBit, true);
                        Logger.LogInfo($"{Name} 收到命令回复：{message}");
                        Logger.LogInfo($"PLC写入{replys.ReturnBit} 为 true");
                        return;
                    }
                }
            }

            

            Logger.LogInfo($"{Name} 收到不做判断的消息: {message}");
        }

        public async Task<bool> SendWithReplyAsync(
            string sendCmd,
            int timeoutMs,
            params string[] expectedReplies
        )
        {
            await _sendLock.WaitAsync();
            try
            {
                var tcs = new TaskCompletionSource<string>();
                string combinedKey = string.Join("|", expectedReplies);
                _pendingReplies[combinedKey] = tcs;

                SendToSerialPort(sendCmd);

                var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));
                _pendingReplies.TryRemove(combinedKey, out _);

                if (completedTask == tcs.Task)
                {
                    Result = tcs.Task.Result;
                    Logger.LogInfo($"{Name} 收到期望回复：{Result}");
                    return true;
                }
                else
                {
                    Logger.LogError($"{Name} 等待 {sendCmd} 的回复 {combinedKey} 超时");
                    return false;
                }
            }
            finally
            {
                _sendLock.Release();
            }
        }

        private void SendToSerialPort(string message)
        {
            string message1 = string.IsNullOrEmpty(message) ? "" : $"{message}{_messageEnd}";
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Write(message1);
                Logger.LogInfo($"{Name} 发送报文：{message1}");
            }
            else
            {
                Logger.LogError($"{PortName} 串口未打开，无法发送报文：{message1}");
            }
        }

        // 防抖计数器：防止 PLC 信号抖动造成误触发
        private readonly Dictionary<string, int> _trueCount = new Dictionary<string, int>();

        /// <summary>
        /// 上升沿检测 + 防抖
        /// </summary>
        private bool IsRisingEdge(string bit, bool current, int debounce = 1)
        {
            if (!_lastTriggerState.ContainsKey(bit))
            {
                _lastTriggerState[bit] = current;
                _trueCount[bit] = current ? debounce : 0;
                Logger.LogInfo($"[{bit}] 初始化状态：{current}");
                return false; // 第一次只初始化，不触发
            }

            bool last = _lastTriggerState[bit];

            // 防抖计数
            if (current)
            {
                if (!_trueCount.ContainsKey(bit))
                    _trueCount[bit] = 0;
                _trueCount[bit]++;
            }
            else
            {
                _trueCount[bit] = 0;
            }

            bool stableTrue = _trueCount[bit] >= debounce;

            // 更新状态
            _lastTriggerState[bit] = stableTrue;

            bool rising = !last && stableTrue;

            //Logger.LogInfo(
            //    $"[{bit}] Last={last}, Now={current}, Stable={stableTrue}, Rising={rising}"
            //);

            return rising;
        }

        private Dictionary<string, bool> _lastTriggerState = new Dictionary<string, bool>();

        /// <summary>
        /// 执行 LoadPort 的所有命令，根据触发位判断是否执行
        /// </summary>

        public async Task ExecuteCommandsAsync()
        {
            if (Form1.isConnected)
            {
                foreach (var cmd in Commands)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(cmd.TriggerBit))
                            continue;

                        bool trigger = Form1.plcMcNet.ReadBool(cmd.TriggerBit).Content;

                        // 调用封装好的上升沿检测
                        if (IsRisingEdge(cmd.TriggerBit, trigger, debounce: 3))
                        {
                            Logger.LogInfo($"{cmd.TriggerBit} 上升沿触发，执行命令 {cmd.Command}");

                            bool success = await SendWithReplyAsync(
                                cmd.Command,
                                cmd.TimeoutMs,
                                cmd.ExpectedReply.Split('|')
                            );

                            if (success)
                            {
                                if (
                                    !string.IsNullOrEmpty(cmd.ReturnAddress)
                                    && !string.IsNullOrEmpty(Result)
                                )
                                {
                                    var addresses = cmd.ReturnAddress.Split('|').ToList();
                                    switch (cmd.Command)
                                    {
                                        case "FSR FC=2":
                                            if (addresses.Count > 1)
                                            {
                                                MapCalculate(Result, addresses[0], addresses[1]);
                                            }
                                            else
                                            {
                                                MapCalculate(Result, addresses[0]);
                                            }
                                            break;
                                        case "HCS RDRF SG01 16":
                                            Form1.plcMcNet.Write(
                                                cmd.ReturnAddress,
                                                RfidCalculate(Result)
                                            );
                                            break;
                                        case "HCS RDRF SG01 08":
                                            Form1.plcMcNet.Write(
                                                cmd.ReturnAddress,
                                                RfidCalculate(Result)
                                            );
                                            break;
                                        case "FSR FC=15":
                                            if (Result.Contains("MANUAL"))
                                            {
                                                Form1.plcMcNet.Write(cmd.ReturnAddress, 1);
                                            }
                                            else
                                            {
                                                Form1.plcMcNet.Write(cmd.ReturnAddress, 2);
                                            }
                                            break;
                                        case "HCS E84_PIOSTATU":
                                            if (Result.Contains("STATUS_ES=TRUE"))
                                            {
                                                Form1.plcMcNet.Write(cmd.ReturnAddress, 1);
                                            }
                                            else
                                            {
                                                Form1.plcMcNet.Write(cmd.ReturnAddress, 2);
                                            }
                                            break;
                                        default:
                                            break;
                                    }
                                    //Form1.plcMcNet.Write(cmd.ReturnAddress, Result);
                                    Logger.LogInfo($"Write PLC {cmd.ReturnAddress} {Result}");
                                }
                                if (!string.IsNullOrEmpty(cmd.ReturnBit))
                                {
                                    Form1.plcMcNet.Write(cmd.ReturnBit, true);
                                    Logger.LogInfo($"Write PLC {cmd.ReturnBit} {true}");
                                }
                                else
                                {
                                    Logger.LogError($"命令 {cmd.Remark} 成功，但没有配置成功返回位");
                                    return;
                                }

                                Logger.LogInfo($"执行成功：{cmd.Remark}");
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(cmd.ReturnBitFail))
                                {
                                    Form1.plcMcNet.Write(cmd.ReturnBitFail, true);
                                    Logger.LogInfo($"Write PLC {cmd.ReturnBitFail} {true}");
                                }

                                //if (!string.IsNullOrEmpty(cmd.FailValue))
                                //{
                                //    //Form1.plcMcNet.Write(cmd.FailValue, "FAIL");

                                //}
                                Logger.LogError($"执行失败或超时：{cmd.Remark}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Form1.isConnected = false;
                        Logger.LogError($"命令执行异常：{cmd.Remark}, {ex.Message}");
                    }
                }
            }
            else
            {
                Logger.LogError($"PLC 未连接，无法执行命令");
            }
        }

        #region mapping结果处理
        private void MapCalculate(string input, string address)
        {
            int innput = 0;
            int innputCount = 0;
            innput = Convert.ToInt32(address.Replace("D", ""));

            // Remove "FSD2 " from the input string
            string trimmedInput = input.Replace("FSD2 ", "");

            //Wafer pcs
            int NullWaferPcs = 0;

            // Split the string by spaces
            string[] parts = trimmedInput.Substring(0, trimmedInput.Length).Split(' ');

            // Create a new string array to store the values of S1 to S25
            string[] sValues = new string[50];

            // Iterate over the parts array and store the values in sValues array
            for (int i = 0; i < parts.Length; i++)
            {
                sValues[i] = parts[i].Split('=')[1];
            }
            // Print the values to verify
            for (int i = 0; i < sValues.Length; i++)
            {
                switch (sValues[i])
                {
                    case "F":
                        Form1.plcMcNet.Write($"D{innput + i}", 1);
                        break;
                    case "C":
                        Form1.plcMcNet.Write($"D{innput + i}", 2);
                        break;
                    case "D":
                        Form1.plcMcNet.Write($"D{innput + i}", 3);
                        //NullWaferPcs++;
                        break;
                    case "E":
                        Form1.plcMcNet.Write($"D{innput + i}", 4);
                        NullWaferPcs++;
                        break;
                }
            }
            innputCount = innput + parts.Length;
            Form1.plcMcNet.Write($"D{innputCount}", (parts.Length - NullWaferPcs));
            //if (Form1.lbl_Count.InvokeRequired)
            //{
            //    Invoke(new Action(() => Form1.lbl_Count.Text = (25 - NullWaferPcs).ToString()));
            //}
            //else
            //{
            //    Form1.lbl_Count.Text = (25 - NullWaferPcs).ToString();
            //}
        }

        private void MapCalculate(string input, string address, string countaddress)
        {
            int innput = 0;
            int innputCount = 0;
            innput = Convert.ToInt32(address.Replace("D", ""));
            innputCount = Convert.ToInt32(countaddress.Replace("D", ""));
            // Remove "FSD2 " from the input string
            string trimmedInput = input.Replace("FSD2 ", "");

            //Wafer pcs
            int NullWaferPcs = 0;

            // Split the string by spaces
            string[] parts = trimmedInput.Substring(0, trimmedInput.Length).Split(' ');

            // Create a new string array to store the values of S1 to S25
            string[] sValues = new string[50];

            // Iterate over the parts array and store the values in sValues array
            for (int i = 0; i < parts.Length; i++)
            {
                sValues[i] = parts[i].Split('=')[1];
            }
            // Print the values to verify
            for (int i = 0; i < sValues.Length; i++)
            {
                switch (sValues[i])
                {
                    case "F":
                        Form1.plcMcNet.Write($"D{innput + i}", 1);
                        break;
                    case "C":
                        Form1.plcMcNet.Write($"D{innput + i}", 2);
                        break;
                    case "D":
                        Form1.plcMcNet.Write($"D{innput + i}", 3);
                        //NullWaferPcs++;
                        break;
                    case "E":
                        Form1.plcMcNet.Write($"D{innput + i}", 4);
                        NullWaferPcs++;
                        break;
                }
            }

            Form1.plcMcNet.Write($"D{innputCount}", (parts.Length - NullWaferPcs));
            Logger.LogInfo($"晶圆总数为{(parts.Length - NullWaferPcs)} Write PLC D{innputCount} {(parts.Length - NullWaferPcs)}");
            //if (Form1.lbl_Count.InvokeRequired)
            //{
            //    Invoke(new Action(() => Form1.lbl_Count.Text = (25 - NullWaferPcs).ToString()));
            //}
            //else
            //{
            //    Form1.lbl_Count.Text = (25 - NullWaferPcs).ToString();
            //}
        }

        #endregion

        #region RFID结果处理
        private string RfidCalculate(string Str)
        {
            string key = "HCA DATA=";
            int startIndex = Str.IndexOf(key);

            if (startIndex != -1)
            {
                // 计算值的开始位置
                startIndex += key.Length;
                // 查找换行符的位置
                int endIndex = Str.IndexOf('\r', startIndex);
                if (endIndex == -1) // 如果没有找到换行符，取到字符串的结尾
                {
                    endIndex = Str.Length;
                }
                // 截取值
                string value = Str.Substring(startIndex, endIndex - startIndex);
                return value; // 输出
            }
            else
            {
                return null;
            }
        }
        #endregion
    }
}
