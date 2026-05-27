using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadPort
{
    public enum SendMessage
    {
        NONE = 0,
        HCS_AUTO = 1, //  切成自动
        HCS_RECOVERY = 2, //回原
        HCS_LOCK = 3, //锁住PODshell
        HCS_ENABLE_FETCH = 4, //机台开门并伸出平台到位
        HCS_FETCH = 5, //SMIF升起POD shell
        HCS_ENABLE_LOAD = 6, //可以LOAD
        HCS_LOAD = 7, //上料
        HCS_ENABLE_OPEN = 8, //POD可以开
        HCS_ENABLE_UNLOAD = 10, //可以下料
        HCS_UNLOAD = 11, //下料
        HCS_CLOSE = 12, //
        HCS_UNLK = 13, //解锁shell
        HCS_MAP = 14, //&mapping
        HCS_RDRF_SG01_16 = 15, //RFID
        HCS_MANUAL = 16, //手动
        HCS_OPEN = 17, //
        HCS_RESET = 18,//FULL RESET
        HCS_E847AUTO = 19, //E84自动
        HCS_E847MANUAL = 20, //E84手动
    }

}
