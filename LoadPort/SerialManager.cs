// ================================================================
// SerialManager.cs — 保留为工具/枚举集合类
// 注意：Loadport 类已改为使用 ICommChannel 接口（LoadPort\Comm\），
//       SerialManager 不再作为基类使用，仅保留枚举和工具方法供参考。
// ================================================================
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LoadPort
{
    public class SerialManager
    {
        #region 变量属性

        protected SerialPort _serialPort;
        protected string _name;
        protected StringBuilder _receiveBuffer = new StringBuilder();
        protected string _messageEnd = "\r\n"; // 可根据协议改为自定义的结束符号，如 <ETX>

        private string _portName = "COM1"; //串口号，默认COM1
        private SerialPortBaudRates _baudRate = SerialPortBaudRates.BaudRate_9600; //波特率
        private SerialPortDatabits _dataBits = SerialPortDatabits.EightBits; //数据位
        private Parity _parity = Parity.None; //校验位
        private StopBits _stopBits = StopBits.One; //停止位

        //public SerialPort _serialPort = new SerialPort();
        #endregion
        public void chuandi(string port1, string rate1, string databits1, int parity1, int stop1)
        {
            _portName = port1;
            _baudRate = (SerialPortBaudRates)Convert.ToInt32(rate1);
            _parity = (Parity)parity1;
            _stopBits = (StopBits)stop1;
            _dataBits = (SerialPortDatabits)Convert.ToInt32(databits1);
        }

        #region//获取数据
        /// <summary>
        /// 获取当前串口
        /// </summary>
        /// <param name="k">引用combobox的name</param>
        /// <param name="a">要选的索引：0串口,1波特,2数据位,3校验位,4停止位</param>
        public void gainchuan(ComboBox k, int a)
        {
            try
            {
                k.Items.Clear();
                switch (a)
                {
                    case 1: //串口名称
                        k.Items.AddRange(SerialPort.GetPortNames());
                        break;
                    case 2: //波特率
                        foreach (SerialPortBaudRates m1 in Enum.GetValues(typeof(SerialPortBaudRates)))
                        {
                            k.Items.Add((int)m1);
                        }
                        break;
                    case 3: //数据位
                        foreach (SerialPortDatabits m2 in Enum.GetValues(typeof(SerialPortDatabits)))
                        {
                            k.Items.Add((int)m2);
                        }
                        break;
                    case 4: //校检位
                        k.Items.AddRange(Enum.GetNames(typeof(Parity)));
                        break;
                    case 5: //停止位
                        k.Items.AddRange(Enum.GetNames(typeof(StopBits)));
                        break;
                }

                k.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        #endregion
        }

        #region 一些数据
        /// <summary>
        /// 串口波特率列表。
        /// 75,110,150,300,600,1200,2400,4800,9600,14400,19200,28800,38400,56000,57600,
        /// 115200,128000,230400,256000
        /// </summary>
        public enum SerialPortBaudRates : int
        {
            BaudRate_75 = 75,
            BaudRate_110 = 110,
            BaudRate_150 = 150,
            BaudRate_300 = 300,
            BaudRate_600 = 600,
            BaudRate_1200 = 1200,
            BaudRate_2400 = 2400,
            BaudRate_4800 = 4800,
            BaudRate_9600 = 9600,
            BaudRate_14400 = 14400,
            BaudRate_19200 = 19200,
            BaudRate_28800 = 28800,
            BaudRate_38400 = 38400,
            BaudRate_56000 = 56000,
            BaudRate_57600 = 57600,
            BaudRate_115200 = 115200,
            BaudRate_128000 = 128000,
            BaudRate_230400 = 230400,
            BaudRate_256000 = 256000
        }

        /// <summary>
        /// 串口数据位列表（5,6,7,8）
        /// </summary>
        public enum SerialPortDatabits : int
        {
            FiveBits = 5,
            SixBits = 6,
            SeventBits = 7,
            EightBits = 8
        }

        #endregion
        #region 开关发送
        /// <summary>
        /// 判断端口状态并关闭
        /// </summary>
        public void ClosePort()
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }
        }

        /// <summary>
        /// 打开串口
        /// </summary>
        public bool OPenPort()
        {
            if (_serialPort.IsOpen)
                _serialPort.Close();

            _serialPort.PortName = _portName;
            _serialPort.BaudRate = (int)_baudRate;
            _serialPort.Parity = _parity;
            _serialPort.DataBits = (int)_dataBits;
            _serialPort.StopBits = _stopBits;
            _serialPort.RtsEnable = true;

            try
            {
                _serialPort.Open();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "提示信息", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// 端口是否已经打开
        /// </summary>
        public bool IsOpen
        {
            get { return _serialPort.IsOpen; }
        }

        public void send(bool a, string k)
        {
            if (_serialPort.IsOpen)
            {
                if (a)
                {
                    WriteData(HexToByte(k));
                }
                else
                {
                    byte[] array = Encoding.Default.GetBytes(k);
                    _serialPort.Write(array, 0, array.Length);
                    //_serialPort.Write(k);
                }
            }
        #endregion
        }

        /// <summary>
        /// 写入数据
        /// </summary>
        /// <param name="msg">写入端口的字节数组</param>
        public void WriteData(byte[] msg)
        {
            //if (!(_serialPort.IsOpen)) _serialPort.Open();
            try
            {
                _serialPort.Write(msg, 0, msg.Length);
            }
            catch (Exception) { }
        }

        #region 格式转换
        /// <summary>
        /// 转换十六进制字符串到字节数组
        /// </summary>
        /// <param name="msg">待转换字符串</param>
        /// <returns>字节数组</returns>
        public static byte[] HexToByte(string msg)
        {
            // 移除空格
            msg = msg.Replace(" ", "");
            byte[] comBuffer = new byte[msg.Length / 2];
            for (int i = 0; i < msg.Length; i += 2)
            {
                comBuffer[i / 2] = (byte)Convert.ToByte(msg.Substring(i, 2), 16);
            }
            return comBuffer;
        }

        /// <summary>
        /// 转换字节数组到十六进制字符串
        /// </summary>
        /// <param name="comByte">待转换字节数组</param>
        /// <returns>十六进制字符串</returns>
        public static string ByteToHex(byte[] comByte)
        {
            string returnStr = "";
            if (comByte != null)
            {
                for (int i = 0; i < comByte.Length; i++)
                {
                    returnStr += comByte[i].ToString("X2") + " ";
                }
            }
            return returnStr;
        }

        /// <summary>
        /// 16进制字符串先转字节数组
        /// </summary>
        /// <param name="strHEX">16进制字符串</param>
        /// <returns>字节数组</returns>
        public byte[] HexstringToBytes(string strHEX)
        {
            if (strHEX.Length == 0)
            {
                return null;
            }
            strHEX = strHEX.Length % 2 == 0 ? strHEX : "0" + strHEX;

            byte[] Barr = new byte[strHEX.Length / 2];
            for (int i = 0; i < Barr.Length; i++)
            {
                Barr[i] = Convert.ToByte(strHEX.Substring(i * 2, 2), 16);
            }
            return Barr;
        }

        /// <summary>
        /// 16进制字符串转ASCII码字符串
        /// </summary>
        /// <param name="strHEX">16进制字符串</param>
        /// <returns>字符串</returns>
        public string getHexASCIItoStr(string strHEX)
        {
            byte[] Barry = HexstringToBytes(strHEX);
            System.Text.ASCIIEncoding asciiEncoing = new System.Text.ASCIIEncoding();
            string strstr = asciiEncoing.GetString(Barry);
            Console.WriteLine(strstr);
            return strstr;
        }

        #endregion


        private static ushort[] crc16Table_modbus =
        {
            0X0000,
            0XC0C1,
            0XC181,
            0X0140,
            0XC301,
            0X03C0,
            0X0280,
            0XC241,
            0XC601,
            0X06C0,
            0X0780,
            0XC741,
            0X0500,
            0XC5C1,
            0XC481,
            0X0440,
            0XCC01,
            0X0CC0,
            0X0D80,
            0XCD41,
            0X0F00,
            0XCFC1,
            0XCE81,
            0X0E40,
            0X0A00,
            0XCAC1,
            0XCB81,
            0X0B40,
            0XC901,
            0X09C0,
            0X0880,
            0XC841,
            0XD801,
            0X18C0,
            0X1980,
            0XD941,
            0X1B00,
            0XDBC1,
            0XDA81,
            0X1A40,
            0X1E00,
            0XDEC1,
            0XDF81,
            0X1F40,
            0XDD01,
            0X1DC0,
            0X1C80,
            0XDC41,
            0X1400,
            0XD4C1,
            0XD581,
            0X1540,
            0XD701,
            0X17C0,
            0X1680,
            0XD641,
            0XD201,
            0X12C0,
            0X1380,
            0XD341,
            0X1100,
            0XD1C1,
            0XD081,
            0X1040,
            0XF001,
            0X30C0,
            0X3180,
            0XF141,
            0X3300,
            0XF3C1,
            0XF281,
            0X3240,
            0X3600,
            0XF6C1,
            0XF781,
            0X3740,
            0XF501,
            0X35C0,
            0X3480,
            0XF441,
            0X3C00,
            0XFCC1,
            0XFD81,
            0X3D40,
            0XFF01,
            0X3FC0,
            0X3E80,
            0XFE41,
            0XFA01,
            0X3AC0,
            0X3B80,
            0XFB41,
            0X3900,
            0XF9C1,
            0XF881,
            0X3840,
            0X2800,
            0XE8C1,
            0XE981,
            0X2940,
            0XEB01,
            0X2BC0,
            0X2A80,
            0XEA41,
            0XEE01,
            0X2EC0,
            0X2F80,
            0XEF41,
            0X2D00,
            0XEDC1,
            0XEC81,
            0X2C40,
            0XE401,
            0X24C0,
            0X2580,
            0XE541,
            0X2700,
            0XE7C1,
            0XE681,
            0X2640,
            0X2200,
            0XE2C1,
            0XE381,
            0X2340,
            0XE101,
            0X21C0,
            0X2080,
            0XE041,
            0XA001,
            0X60C0,
            0X6180,
            0XA141,
            0X6300,
            0XA3C1,
            0XA281,
            0X6240,
            0X6600,
            0XA6C1,
            0XA781,
            0X6740,
            0XA501,
            0X65C0,
            0X6480,
            0XA441,
            0X6C00,
            0XACC1,
            0XAD81,
            0X6D40,
            0XAF01,
            0X6FC0,
            0X6E80,
            0XAE41,
            0XAA01,
            0X6AC0,
            0X6B80,
            0XAB41,
            0X6900,
            0XA9C1,
            0XA881,
            0X6840,
            0X7800,
            0XB8C1,
            0XB981,
            0X7940,
            0XBB01,
            0X7BC0,
            0X7A80,
            0XBA41,
            0XBE01,
            0X7EC0,
            0X7F80,
            0XBF41,
            0X7D00,
            0XBDC1,
            0XBC81,
            0X7C40,
            0XB401,
            0X74C0,
            0X7580,
            0XB541,
            0X7700,
            0XB7C1,
            0XB681,
            0X7640,
            0X7200,
            0XB2C1,
            0XB381,
            0X7340,
            0XB101,
            0X71C0,
            0X7080,
            0XB041,
            0X5000,
            0X90C1,
            0X9181,
            0X5140,
            0X9301,
            0X53C0,
            0X5280,
            0X9241,
            0X9601,
            0X56C0,
            0X5780,
            0X9741,
            0X5500,
            0X95C1,
            0X9481,
            0X5440,
            0X9C01,
            0X5CC0,
            0X5D80,
            0X9D41,
            0X5F00,
            0X9FC1,
            0X9E81,
            0X5E40,
            0X5A00,
            0X9AC1,
            0X9B81,
            0X5B40,
            0X9901,
            0X59C0,
            0X5880,
            0X9841,
            0X8801,
            0X48C0,
            0X4980,
            0X8941,
            0X4B00,
            0X8BC1,
            0X8A81,
            0X4A40,
            0X4E00,
            0X8EC1,
            0X8F81,
            0X4F40,
            0X8D01,
            0X4DC0,
            0X4C80,
            0X8C41,
            0X4400,
            0X84C1,
            0X8581,
            0X4540,
            0X8701,
            0X47C0,
            0X4680,
            0X8641,
            0X8201,
            0X42C0,
            0X4380,
            0X8341,
            0X4100,
            0X81C1,
            0X8081,
            0X4040
        };

        /// <summary>
        /// CRC16校验
        /// </summary>
        /// <param name="strHEX"></param>
        /// <returns></returns>
        private byte[] GetCrc16_Modbus(string strHEX)
        {
            Byte[] Bytes = HexstringToBytes(strHEX);
            ushort crc16 = 0xFFFF;
            for (int i = 0; i < Bytes.Length; i++)
            {
                crc16 = (ushort)(
                    (crc16 >> 8) ^ crc16Table_modbus[(byte)((crc16 ^ Bytes[i]) & 0x00ff)]
                );
            }
            byte[] crcBytes = new byte[2];
            crcBytes[0] = (byte)crc16; //low crc
            crcBytes[1] = (byte)(crc16 >> 8); //high crc
            return crcBytes;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name"></param>
        /// <param name="portName"></param>
        /// <param name="baudRate"></param>
        /// <param name="dataBits"></param>
        /// <param name="stopBits"></param>
        /// <param name="parity"></param>
        public SerialManager(
            string name,
            string portName,
            int baudRate,
            int dataBits,
            StopBits stopBits,
            Parity parity
        )
        {
            _name = name;

            try
            {
                _serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
                _serialPort.DataReceived += SerialPort_DataReceived;
                _serialPort.ErrorReceived += SerialPort_ErrorReceived;
                _serialPort.Open();
                Log($"{_name} opened on {portName}");
            }
            catch (Exception ex)
            {
                Log($"[ERROR] Failed to open {_name} on {portName}: {ex.Message}");
            }
        }

        private void SerialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            Log($"[ERROR] {_name} Serial error: {e.EventType}");
            // 这里可以尝试自动重连等处理
        }

        protected virtual void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string data = _serialPort.ReadExisting();
                _receiveBuffer.Append(data);

                while (_receiveBuffer.ToString().Contains(_messageEnd))
                {
                    string fullMessage = ExtractMessageFromBuffer();
                    if (!string.IsNullOrEmpty(fullMessage))
                    {
                        Log($"Received: {fullMessage}");
                        HandleReceivedMessage(fullMessage); // 子类中可重写
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"[ERROR] {_name} DataReceived error: {ex.Message}");
            }
        }

        private string ExtractMessageFromBuffer()
        {
            string buffer = _receiveBuffer.ToString();
            int endIndex = buffer.IndexOf(_messageEnd);
            if (endIndex >= 0)
            {
                string fullMsg = buffer.Substring(0, endIndex);
                _receiveBuffer.Remove(0, endIndex + _messageEnd.Length);
                return fullMsg.Trim();
            }
            return null;
        }

        protected virtual void HandleReceivedMessage(string message)
        {
            // 默认回显，可在子类中处理协议
            string reply = $"Echo: {message}{_messageEnd}";
            Send(reply);
        }

        public void Send(string message)
        {
            try
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.Write(message);
                    Log($"Sent: {message}");
                }
            }
            catch (Exception ex)
            {
                Log($"[ERROR] {_name} send failed: {ex.Message}");
            }
        }

        protected void Log(string message)
        {
            try
            {
                //string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                //Directory.CreateDirectory(logDir);
                //string filePath = Path.Combine(logDir, $"{DateTime.Now:yyyy-MM-dd}_LOG.txt");
                //File.AppendAllText(filePath, $"{DateTime.Now:HH:mm:ss} [{_name}] {message}\n");
            }
            catch
            {
                // 避免日志写入失败引发新异常
            }
        }

        public void Dispose()
        {
            if (_serialPort != null)
            {
                _serialPort.Close();
            }
            _serialPort.Dispose();
        }

        public void Close()
        {
            try
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.Close();
                    Log($"{_name} closed.");
                }
            }
            catch (Exception ex)
            {
                Log($"[ERROR] Close port failed: {ex.Message}");
            }
        }

        ///// <summary>
        ///// 默认构造函数
        ///// </summary>
        //public SerialManager()
        //{
        //    _portName = "COM1";
        //    _baudRate = SerialPortBaudRates.BaudRate_9600;
        //    _parity = Parity.None;
        //    _dataBits = SerialPortDatabits.EightBits;
        //    _stopBits = StopBits.One;
        //    _serialPort.DataReceived += new SerialDataReceivedEventHandler(_serialPort_DataReceived);
        //    _serialPort.ErrorReceived += new SerialErrorReceivedEventHandler(_serialPort_ErrorReceived);
        //}

        //public delegate void DataReceivedEventHandler(DataReceivedEventArgs e);
        //public event DataReceivedEventHandler DataReceived;
        //public event SerialErrorReceivedEventHandler Error;

        /// <summary>
        /// 数据接收处理
        /// </summary>
        //void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        //{
        //      await Task.Delay(1000);
        //    {
        //        byte[] receviedBuf = new byte[_serialPort.BytesToRead];

        //        int itemp = _serialPort.BytesToRead;
        //        int rcvByteLen = 0;
        //        try
        //        {
        //            for (int i = 0; i < itemp; i++)
        //            {
        //                receviedBuf[i] = Convert.ToByte(_serialPort.ReadByte());
        //                rcvByteLen++;
        //            }
        //            if (receviedBuf != null)
        //            {
        //                DataReceived(new DataReceivedEventArgs(receviedBuf));
        //            }
        //        }
        //        catch (System.Exception ex) { }
        //    }
        //}

        /// <summary>
        //void _serialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        //{
        //    Error?.Invoke(sender, e);
        //} // 错误处理函数
        /// </summary>
    }
}
