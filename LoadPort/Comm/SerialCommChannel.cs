using System;
using System.IO.Ports;
using System.Text;

namespace LoadPort.Comm
{
    /// <summary>
    /// 串口通讯通道（RS-232/RS-485）
    /// 报文以 \r\n 为结束符，自动缓冲拼包
    /// </summary>
    public class SerialCommChannel : ICommChannel
    {
        private readonly SerialPort _port;
        private readonly StringBuilder _buffer = new StringBuilder();
        private const string MessageEnd = "\r\n";

        public string ChannelName { get; }
        public bool IsOpen => _port?.IsOpen ?? false;

        public event Action<string> MessageReceived;

        public SerialCommChannel(string name, string portName, int baudRate,
            int dataBits, StopBits stopBits, Parity parity)
        {
            ChannelName = name;
            try
            {
                _port = new SerialPort(portName, baudRate, parity, dataBits, stopBits)
                {
                    RtsEnable = true
                };
                _port.DataReceived += OnDataReceived;
                _port.ErrorReceived += OnErrorReceived;
                _port.Open();
                Logger.LogInfo($"[{ChannelName}] 串口 {portName} 已打开");
            }
            catch (Exception ex)
            {
                Logger.LogError($"[{ChannelName}] 串口 {portName} 打开失败: {ex.Message}");
            }
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string data = _port.ReadExisting();
                _buffer.Append(data);
                FlushBuffer();
            }
            catch (Exception ex)
            {
                Logger.LogError($"[{ChannelName}] 串口接收异常: {ex.Message}");
            }
        }

        private void OnErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            Logger.LogError($"[{ChannelName}] 串口错误: {e.EventType}");
        }

        private void FlushBuffer()
        {
            while (true)
            {
                string buf = _buffer.ToString();
                int idx = buf.IndexOf(MessageEnd, StringComparison.Ordinal);
                if (idx < 0) break;

                string msg = buf.Substring(0, idx).Trim();
                _buffer.Remove(0, idx + MessageEnd.Length);

                if (!string.IsNullOrEmpty(msg))
                {
                    Logger.LogInfo($"[{ChannelName}] 收到: {msg}");
                    MessageReceived?.Invoke(msg);
                }
            }
        }

        public void Send(string message)
        {
            try
            {
                if (_port != null && _port.IsOpen)
                {
                    string full = $"{message}{MessageEnd}";
                    _port.Write(full);
                    Logger.LogInfo($"[{ChannelName}] 发送: {message}");
                }
                else
                {
                    Logger.LogError($"[{ChannelName}] 串口未打开，无法发送: {message}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[{ChannelName}] 发送失败: {ex.Message}");
            }
        }

        public void Close()
        {
            try
            {
                if (_port != null && _port.IsOpen)
                {
                    _port.Close();
                    Logger.LogInfo($"[{ChannelName}] 串口已关闭");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[{ChannelName}] 关闭串口失败: {ex.Message}");
            }
        }

        public void Dispose() => Close();
    }
}
