using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EIRC
{
    internal class Utils
    {
        public static void SendNotice(string channel, string notice, StreamWriter sw)
        {
            sw.WriteLine($"NOTICE {channel} :{notice}");
            sw.Flush();
        }
        public static void SendPart(string channel,  StreamWriter sw)
        {
            sw.WriteLine($"PART {channel}");
            sw.Flush();
        }
    }
}
