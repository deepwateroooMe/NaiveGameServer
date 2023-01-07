using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyServer.net
{
    public class MsgPong : MsgBase {

        public MsgPong() {
            protoName = "MsgPong";
        }
    }
}
