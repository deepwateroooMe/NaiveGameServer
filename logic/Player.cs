using MyServer.net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyServer.logic {

    //网络传输中需要的数据类
	public class Player {
        public string id = "";
        public ClientState state;
        //临时数据
        public int x;
        public int y;
        public int z;
        //数据库数据
        public PlayerData data;
        //构造函数
        public Player(ClientState state)
            {
                this.state = state;
            }
        //发送消息 (这里方便玩家找玩家,比如互相攻击)
        public void Send(MsgBase msgBase)
            {
                NetManager.Send(state, msgBase.ToString());
            }
	}
}
