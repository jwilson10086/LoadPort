using System;

namespace LoadPort.Comm
{
    /// <summary>
    /// 通讯通道抽象接口，支持串口和 TCP 两种模式
    /// </summary>
    public interface ICommChannel : IDisposable
    {
        /// <summary>通道名称（用于日志）</summary>
        string ChannelName { get; }

        /// <summary>通道是否已连接/打开</summary>
        bool IsOpen { get; }

        /// <summary>收到完整报文时触发（已按 \r\n 分包）</summary>
        event Action<string> MessageReceived;

        /// <summary>发送文本报文（内部自动追加 \r\n）</summary>
        void Send(string message);

        /// <summary>关闭通道</summary>
        void Close();
    }
}
