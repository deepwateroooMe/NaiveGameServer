using MyServer.net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyServer.logic {

    class SysMsgHandler {}

    public partial class MsgHandler {
        public static void MshPing(ClientState c, MsgBase msgBase) {
            Console.WriteLine("MsgPing");
        }
    }
}
