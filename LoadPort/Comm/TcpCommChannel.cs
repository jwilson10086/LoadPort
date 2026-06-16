using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LoadPort.Comm
{
    /// <summary>
    /// TCP 客户端通讯通道
    /// 报文格式与串口完全相同，以 \r\n 为结束符
    /// 支持断线自动重连
    /// </summary>
    public class TcpCommChannel : ICommChannel
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private readonly StringBuilder _buffer = new StringBuilder();
        private const string MessageEnd = "\r\n";

        private readonly string _ip;
        private readonly int _tcpPort;
        private bool _disposed = false;
        private bool _isOpen = false;

        private CancellationTokenSource _cts = new CancellationTokenSource();

        public string ChannelName { get; }
        public bool IsOpen => _isOpen;

        public event Action<string> MessageReceived;

        public TcpCommChannel(string name, string ip, int port)
        {
            ChannelName = name;
            _ip = ip;
            _tcpPort = port;
            Connect();
        }

        private void Connect()
        {
            try
            {
                _client?.Close();
                _client = new TcpClient();
                _client.Connect(_ip, _tcpPort);
                _stream = _client.GetStream();
                _isOpen = true;
                Logger.LogInfo($"[{ChannelName}] TCP 连接成功 {_ip}:{_tcpPort}");
                // 启动异步接收
                Task.Run(() => ReceiveLoop(_cts.Token));
            }
            catch (Exception ex)
            {
                _isOpen = false;
                Logger.LogError($"[{ChannelName}] TCP 连接失败 {_ip}:{_tcpPort} : {ex.Message}");
                // 5 秒后自动重连
                Task.Delay(5000).ContinueWith(_ =>
                {
                    if (!_disposed) Connect();
                });
            }
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            byte[] buf = new byte[4096];
            try
            {
                while (!token.IsCancellationRequested && _isOpen)
                {
                    int count = await _stream.ReadAsync(buf, 0, buf.Length, token);
                    if (count == 0)
                    {
                        // 连接断开
                        Logger.LogError($"[{ChannelName}] TCP 连接断开，准备重连...");
                        _isOpen = false;
                        if (!_disposed)
                        {
                            await Task.Delay(3000, token);
                            Connect();
                        }
                        return;
                    }

                    string data = Encoding.ASCII.GetString(buf, 0, count);
                    _buffer.Append(data);
                    FlushBuffer();
                }
            }
            catch (OperationCanceledException) { /* 正常退出 */ }
            catch (Exception ex)
            {
                _isOpen = false;
                Logger.LogError($"[{ChannelName}] TCP 接收异常: {ex.Message}");
                if (!_disposed)
                {
                    await Task.Delay(3000);
                    Connect();
                }
            }
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
                if (_isOpen && _stream != null)
                {
                    string full = $"{message}{MessageEnd}";
                    byte[] data = Encoding.ASCII.GetBytes(full);
                    _stream.Write(data, 0, data.Length);
                    Logger.LogInfo($"[{ChannelName}] 发送: {message}");
                }
                else
                {
                    Logger.LogError($"[{ChannelName}] TCP 未连接，无法发送: {message}");
                }
            }
            catch (Exception ex)
            {
                _isOpen = false;
                Logger.LogError($"[{ChannelName}] TCP 发送失败: {ex.Message}");
            }
        }

        public void Close()
        {
            _disposed = true;
            _cts.Cancel();
            _isOpen = false;
            try
            {
                _stream?.Close();
                _client?.Close();
                Logger.LogInfo($"[{ChannelName}] TCP 连接已关闭");
            }
            catch (Exception ex)
            {
                Logger.LogError($"[{ChannelName}] 关闭 TCP 失败: {ex.Message}");
            }
        }

        public void Dispose() => Close();
    }
}
