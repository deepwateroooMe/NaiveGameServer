using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MyServer.net {

    public class ClientState {

        public Socket socket;
        public ByteArray readBuff = new ByteArray();
        
        // public Socket socket;
        // public byte[] readBuff = new byte[1024];
        public float hp;
        public float x;
        public float y;
        public float z;
        public float eulY;
    }
}
