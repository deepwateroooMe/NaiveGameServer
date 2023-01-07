using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MyServer.net;

namespace MyServer {

    public class EventHandler {

        // 在客户端异常时,我们会调用这个方法,那么客户端,又如何得知,此时应该处理掉线的用户呢?
        // 我们在调用OnDisconnect的时候,广播一个Leave协议,告诉所有的客户端,某个客户端下线了.
        public static void OnDisConnect(ClientState c) {
            Console.WriteLine("Disconnect");
            string desc = c.socket.RemoteEndPoint.ToString();
            string sendStr = "Leave|" + desc + ",";
            foreach (ClientState cs in Program.clients.Values) {
                Program.Send(cs, sendStr);
            }
        }

        public static void OnTimer() {
        }

        // 发送
        public static void Send(ClientState cs, MsgBase msg) {
            // 状态判断
            if (cs == null)
                return;
            if (!cs.socket.Connected)
                return;
            // 数据编码
            byte[] nameBytes = MsgBase.EncodeName(msg);
            byte[] bodyBytes = MsgBase.Encode(msg);
            int len = nameBytes.Length + bodyBytes.Length;
            byte[] sendBytes = new byte[2 + len];
            // 组装长度
            sendBytes[0] = (byte)(len % 256);
            sendBytes[1] = (byte)(len / 256);
            // 组装名字
            Array.Copy(nameBytes, 0, sendBytes, 2, nameBytes.Length);
            // 组装消息体
            Array.Copy(bodyBytes, 0, sendBytes, 2 + nameBytes.Length, bodyBytes.Length);
            // 简化代码,不设置回调
            try {
                cs.socket.BeginSend(sendBytes, 0, sendBytes.Length, 0, null, null);
            }
            catch (SocketException e ) {
                Console.WriteLine("Socket close onBeginsend" + e.ToString());
            }
        }
    }
}
