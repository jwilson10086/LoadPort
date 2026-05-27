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
    public class LoadPortConfig
    {
        public string Name { get; set; }
        public string Type { get; set; }  // LP or UNLP
        public string PortName { get; set; }
        public int BaudRate { get; set; }
        public int DataBits { get; set; }
        public int StopBits { get; set; }
        public int Parity { get; set; }
        public bool Bypass { get; set; }
    }
   

}
