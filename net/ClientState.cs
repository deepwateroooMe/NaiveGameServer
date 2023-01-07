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

        //玩家数据后面添加
        //最后一次ping的时间
        public long lastPingTime = 0;
        
        // public Socket socket;
        // public byte[] readBuff = new byte[1024];
        public float hp;
        public float x;
        public float y;
        public float z;
        public float eulY;
    }
}
