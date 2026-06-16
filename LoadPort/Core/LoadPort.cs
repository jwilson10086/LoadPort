using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LoadPort.Comm;
using LoadPort.Models;

namespace LoadPort
{
    /// <summary>
    /// LoadPort 设备控制类
    /// 与底层通讯通道（串口或 TCP）解耦，通过 ICommChannel 交互
    /// </summary>
    public class Loadport
    {
        // ── 公开属性 ─────────────────────────────────────────────────
        public string Name { get; private set; }
        /// <summary>当前使用的通讯通道描述（串口号 或 IP:Port）</summary>
        public string ChannelInfo { get; private set; }
        public string Result { get; set; }
        public bool Bypass => LoadPortConfig?.Bypass ?? false;
        public bool IsOpen => _channel?.IsOpen ?? false;

        public LoadPortConfig LoadPortConfig { get; set; }
        public List<PlcCommandConfig> Commands { get; set; } = new List<PlcCommandConfig>();

        // ── 内部成员 ──────────────────────────────────────────────────
        private readonly ICommChannel _channel;
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        private readonly ConcurrentDictionary<string, TaskCompletionSource<string>>
            _pendingReplies = new ConcurrentDictionary<string, TaskCompletionSource<string>>();

        private readonly Dictionary<string, bool> _lastTriggerState = new Dictionary<string, bool>();
        private readonly Dictionary<string, int> _trueCount = new Dictionary<string, int>();

        // ── 构造函数 ──────────────────────────────────────────────────
        public Loadport(string name, ICommChannel channel)
        {
            Name = name;
            _channel = channel;
            ChannelInfo = channel.ChannelName;
            _channel.MessageReceived += HandleReceivedMessage;
        }

        // ── 消息处理 ──────────────────────────────────────────────────
        private void HandleReceivedMessage(string message)
        {
            // 1. 优先匹配异步等待中的期望回复
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

            // 2. 主动上报（Command 为空，有期望回复 → 写 PLC 位）
            foreach (var replys in Commands)
            {
                if (replys.Command == "" && replys.ExpectedReply != "")
                {
                    if (message.Contains(replys.ExpectedReply))
                    {
                        Form1.plcMcNet.Write(replys.ReturnBit, true);
                        Logger.LogInfo($"{Name} 收到主动上报：{message}，写 PLC {replys.ReturnBit}=true");
                        return;
                    }
                }
            }

            Logger.LogInfo($"{Name} 收到未匹配消息: {message}");
        }

        // ── 发送并等待回复 ────────────────────────────────────────────
        public async Task<bool> SendWithReplyAsync(string sendCmd, int timeoutMs,
            params string[] expectedReplies)
        {
            await _sendLock.WaitAsync();
            try
            {
                var tcs = new TaskCompletionSource<string>();
                string combinedKey = string.Join("|", expectedReplies);
                _pendingReplies[combinedKey] = tcs;

                _channel.Send(sendCmd);

                var completed = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));
                _pendingReplies.TryRemove(combinedKey, out _);

                if (completed == tcs.Task)
                {
                    Result = tcs.Task.Result;
                    Logger.LogInfo($"{Name} 收到期望回复：{Result}");
                    return true;
                }
                else
                {
                    Logger.LogError($"{Name} 等待 [{sendCmd}] 回复超时（{timeoutMs}ms）");
                    return false;
                }
            }
            finally
            {
                _sendLock.Release();
            }
        }

        // ── 上升沿检测 + 防抖 ─────────────────────────────────────────
        private bool IsRisingEdge(string bit, bool current, int debounce = 1)
        {
            if (!_lastTriggerState.ContainsKey(bit))
            {
                _lastTriggerState[bit] = current;
                _trueCount[bit] = current ? debounce : 0;
                return false;
            }

            bool last = _lastTriggerState[bit];
            _trueCount[bit] = current ? (_trueCount.ContainsKey(bit) ? _trueCount[bit] + 1 : 1) : 0;

            bool stableTrue = _trueCount[bit] >= debounce;
            _lastTriggerState[bit] = stableTrue;
            return !last && stableTrue;
        }

        // ── PLC 轮询主循环 ────────────────────────────────────────────
        public async Task ExecuteCommandsAsync()
        {
            if (!Form1.isConnected) return;

            foreach (var cmd in Commands)
            {
                try
                {
                    if (string.IsNullOrEmpty(cmd.TriggerBit)) continue;

                    bool trigger = Form1.plcMcNet.ReadBool(cmd.TriggerBit).Content;
                    if (!IsRisingEdge(cmd.TriggerBit, trigger, debounce: 3)) continue;

                    Logger.LogInfo($"{Name} [{cmd.TriggerBit}] 上升沿，执行：{cmd.Command}");

                    bool success = await SendWithReplyAsync(
                        cmd.Command, cmd.TimeoutMs,
                        cmd.ExpectedReply.Split('|'));

                    if (success)
                    {
                        // 处理回复数据
                        if (!string.IsNullOrEmpty(cmd.ReturnAddress) && !string.IsNullOrEmpty(Result))
                        {
                            var addresses = cmd.ReturnAddress.Split('|').ToList();
                            switch (cmd.Command)
                            {
                                case "FSR FC=2":
                                    if (addresses.Count > 1)
                                        MapCalculate(Result, addresses[0], addresses[1]);
                                    else
                                        MapCalculate(Result, addresses[0]);
                                    break;
                                case "HCS RDRF SG01 16":
                                case "HCS RDRF SG01 08":
                                    Form1.plcMcNet.Write(cmd.ReturnAddress, RfidCalculate(Result));
                                    break;
                                case "FSR FC=15":
                                    Form1.plcMcNet.Write(cmd.ReturnAddress,
                                        Result.Contains("MANUAL") ? 1 : 2);
                                    break;
                                case "HCS E84_PIOSTATU":
                                    Form1.plcMcNet.Write(cmd.ReturnAddress,
                                        Result.Contains("STATUS_ES=TRUE") ? 1 : 2);
                                    break;
                            }
                            Logger.LogInfo($"Write PLC {cmd.ReturnAddress}");
                        }

                        if (!string.IsNullOrEmpty(cmd.ReturnBit))
                        {
                            Form1.plcMcNet.Write(cmd.ReturnBit, true);
                            Logger.LogInfo($"Write PLC {cmd.ReturnBit} = true");
                        }
                        Logger.LogInfo($"执行成功：{cmd.Remark}");
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(cmd.ReturnBitFail))
                        {
                            Form1.plcMcNet.Write(cmd.ReturnBitFail, true);
                            Logger.LogInfo($"Write PLC {cmd.ReturnBitFail} = true（失败位）");
                        }
                        Logger.LogError($"执行失败或超时：{cmd.Remark}");
                    }
                }
                catch (Exception ex)
                {
                    Form1.isConnected = false;
                    Logger.LogError($"命令执行异常：{cmd.Remark}, {ex.Message}");
                }
            }
        }

        // ── 关闭通道 ──────────────────────────────────────────────────
        public void Close() => _channel?.Close();
        public void Dispose() => _channel?.Dispose();

        // ── Mapping 结果解析 ──────────────────────────────────────────
        #region Mapping 处理
        private void MapCalculate(string input, string address)
        {
            int baseAddr = Convert.ToInt32(address.Replace("D", ""));
            string trimmed = input.Replace("FSD2 ", "");
            int nullCount = 0;
            string[] parts = trimmed.Split(' ');
            string[] sValues = new string[50];
            for (int i = 0; i < parts.Length; i++)
                sValues[i] = parts[i].Split('=')[1];
            for (int i = 0; i < parts.Length; i++)
                WriteMapSlot(baseAddr + i, sValues[i], ref nullCount);
            Form1.plcMcNet.Write($"D{baseAddr + parts.Length}", parts.Length - nullCount);
        }

        private void MapCalculate(string input, string address, string countAddress)
        {
            int baseAddr = Convert.ToInt32(address.Replace("D", ""));
            int countAddr = Convert.ToInt32(countAddress.Replace("D", ""));
            string trimmed = input.Replace("FSD2 ", "");
            int nullCount = 0;
            string[] parts = trimmed.Split(' ');
            string[] sValues = new string[50];
            for (int i = 0; i < parts.Length; i++)
                sValues[i] = parts[i].Split('=')[1];
            for (int i = 0; i < parts.Length; i++)
                WriteMapSlot(baseAddr + i, sValues[i], ref nullCount);
            int waferCount = parts.Length - nullCount;
            Form1.plcMcNet.Write($"D{countAddr}", waferCount);
            Logger.LogInfo($"晶圆有效片数={waferCount}，写 D{countAddr}");
        }

        private void WriteMapSlot(int addr, string val, ref int nullCount)
        {
            switch (val)
            {
                case "F": Form1.plcMcNet.Write($"D{addr}", 1); break;
                case "C": Form1.plcMcNet.Write($"D{addr}", 2); break;
                case "D": Form1.plcMcNet.Write($"D{addr}", 3); break;
                case "E": Form1.plcMcNet.Write($"D{addr}", 4); nullCount++; break;
            }
        }
        #endregion

        // ── RFID 结果解析 ─────────────────────────────────────────────
        #region RFID 处理
        private string RfidCalculate(string str)
        {
            const string key = "HCA DATA=";
            int start = str.IndexOf(key, StringComparison.Ordinal);
            if (start < 0) return null;
            start += key.Length;
            int end = str.IndexOf('\r', start);
            if (end < 0) end = str.Length;
            return str.Substring(start, end - start);
        }
        #endregion
    }
}
