using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyServer.net;

namespace MyServer.logic {

    public partial class MsgHandler {

        // 首先是消息的拆分,前文说过,这条消息包含玩家的位置,旋转; 然后是对这个玩家的消息进行广播        
        public static void MsgEnter(ClientState c, string msgArgs) {
            string[] split = msgArgs.Split(',');
            string desc = split[0];
            float x = float.Parse(split[1]);
            float y = float.Parse(split[2]);
            float z = float.Parse(split[3]);
            float eulY = float.Parse(split[4]);
            // 赋值
            c.hp = 100;
            c.x = x;
            c.y = y;
            c.z = z;
            c.eulY = eulY;
            //// 广播
            //string sendStr = "Enter|" + msgArgs;
            //foreach (ClientState cs in Program.clients.Values) {
            //    Program.Send(cs, sendStr);
            //}
        }

        // 服务器中要做的就是遍历所有连接着的客户端,并把它们打包成一条信息,然后广播出去:
        public static void MsgList(ClientState c, string msgArgs) {
            string sendStr = "List|";
            foreach (ClientState cs in Program.clients.Values) {
                sendStr += cs.socket.RemoteEndPoint.ToString() + ",";
                sendStr += cs.x.ToString() + ",";
                sendStr += cs.y.ToString() + ",";
                sendStr += cs.z.ToString() + ",";
                sendStr += cs.eulY.ToString() + ",";
                sendStr += cs.hp.ToString() + ",";
            }
            Program.Send(c, sendStr);
        }

        //  服务接收到来自任何客户端的　移动　消息,当然要调用相对应的MsgMove方法
        public static void MsgMove(ClientState c, string msgArgs) {
            string[] split = msgArgs.Split(',');
            string desc = split[0];
            float x = float.Parse(split[1]);
            float y = float.Parse(split[2]);
            float z = float.Parse(split[3]);
            c.x = x; // 为什么要给C赋值,因为如果再有新的客户端加入进来,服务器广播的就是C的新位置
            c.y = y;
            c.z = z;
            // 组合消息
            string sendStr = "Move|" + msgArgs;
            foreach (ClientState cs in Program.clients.Values) {
                Program.Send(cs, sendStr);
            }
        }

        // 已经把攻击的具体信息传给了服务器,其实服务器并不需要做太多工作,仅仅时记录攻击时的转向,然后再广播给所有客户端.
        public static void MsgAttack(ClientState c, string msgArgs) {
            string sendStr = "Attack|" + msgArgs;
            foreach (ClientState cs in Program.clients.Values) {
                Program.Send(cs, sendStr);
            }
        }

        // 攻击hit
        public static void MsgHit(ClientState c, string msgArgs) {
            // 解析参数
            string[] split = msgArgs.Split(',');
            string attDesc = split[0];
            string hitDesc = split[1];
            // 找出被攻击的角色
            ClientState hitCS = null;
            foreach (ClientState cs in Program.clients.Values) {
                if (cs.socket.RemoteEndPoint.ToString() == hitDesc) {
                    hitCS = cs;
                }
            }
            if (hitCS == null) {
                return;
            }
            hitCS.hp -= 25;
            // 死亡
            if (hitCS.hp <= 0) {
                string sendStr = "Die|" + hitCS.socket.RemoteEndPoint.ToString();
                foreach (ClientState cs in Program.clients.Values) {
                    Program.Send(cs, sendStr);
                }
            }
        }

        public static void MsgPing(ClientState c, MsgBase msgBase) {
            Console.WriteLine("MsgPing");
            c.lastPingTime = NetManager.GetTimeStamp();
            MsgPong msgPong = new MsgPong();
            NetManager.Send(c, msgPong.ToString());
        }
        
        public static void OnTimer() {
            CheckPing();
        }

        //Ping检查
        public static void CheckPing() {
            //现在的时间戳
            long timeNow = NetManager.GetTimeStamp();
            //遍历所有的客户端,把超时的删除
            foreach (ClientState s in NetManager.clients.Values) {
                //4次心跳都没有回应
                if (timeNow - s.lastPingTime > NetManager.PingInterval * 4) {
                    Console.WriteLine("Ping Close " + s.socket.RemoteEndPoint.ToString()) ;
                    NetManager.Close(s);
                    return;
                }
            }
        }
    }
}
