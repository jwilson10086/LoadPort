using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadPort
{
    /// <summary>
    /// 通讯模式枚举
    /// </summary>
    public enum CommMode
    {
        /// <summary>RS-232/RS-485 串口</summary>
        Serial = 0,
        /// <summary>TCP/IP 以太网</summary>
        Tcp = 1
    }

    public class LoadPortConfig
    {
        public string Name { get; set; }
        public string Type { get; set; }  // LP or UNLP

        // ─── 通讯模式 ────────────────────────────────────────────────
        /// <summary>通讯方式：Serial（串口）或 Tcp（TCP/IP）</summary>
        public CommMode CommMode { get; set; } = CommMode.Serial;

        // ─── 串口参数 ────────────────────────────────────────────────
        public string PortName { get; set; }
        public int BaudRate { get; set; } = 9600;
        public int DataBits { get; set; } = 8;
        public int StopBits { get; set; } = 1;
        public int Parity { get; set; } = 0;

        // ─── TCP 参数 ─────────────────────────────────────────────────
        /// <summary>TCP 目标 IP（CommMode=Tcp 时有效）</summary>
        public string TcpIp { get; set; } = "192.168.1.100";
        /// <summary>TCP 目标端口（CommMode=Tcp 时有效）</summary>
        public int TcpPort { get; set; } = 10001;

        // ─── 通用 ─────────────────────────────────────────────────────
        public bool Bypass { get; set; }
    }
}
