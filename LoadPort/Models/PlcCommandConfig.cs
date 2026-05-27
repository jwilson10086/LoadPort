using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadPort.Models
{
    public class PlcCommandConfig
    {
        public string Command { get; set; }
        public string ExpectedReply { get; set; }
        public int TimeoutMs { get; set; }
        public string TriggerBit { get; set; }
        public string ReturnAddress { get; set; }
        public string ReturnBit { get; set; }
        public string ReturnBitFail { get; set; }
        public string FailValue { get; set; }
        public string Remark { get; set; }
    }
}
