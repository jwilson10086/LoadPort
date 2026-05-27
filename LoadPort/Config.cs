using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadPort
{
    public class SerialConfig
    {
        public string PortName { get; set; }
        public int BaudRate { get; set; }
        public int DataBits { get; set; }
        public int StopBits { get; set; }
        public string Parity { get; set; }

    }


}
