using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using MyServer.logic;

namespace MyServer.net {

    public class NetManager {

        // 监听的socket
        public static Socket listenfd;
        // 所有玩家皆在此字典中
        public static Dictionary<Socket, ClientState> clients = 
            new Dictionary<Socket, ClientState>();
        // select 的检查列表 select 挨个便利socket,筛选出符合规范的
        static List<Socket> checkRead = new List<Socket>();

        //(心跳)Ping的时间间隔
        public static long PingInterval = 30;

        // 获取时间戳
        public static long GetTimeStamp() {
            // 从1970年到如今的时间,数据类型为long型数据
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds);
        }
        
        public static void StartLoop(int ListenPort) {
            // 实例化socket
            listenfd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // Bind
            IPAddress ipAdr = IPAddress.Parse("127.0.0.1");
            IPEndPoint ipEp = new IPEndPoint(ipAdr, ListenPort);
            listenfd.Bind(ipEp);
            // Listen
            listenfd.Listen(0);
            Console.WriteLine("[*服务器启动成功*]");
            // 循环 [Socket select模式]
            while (true) {
                ResetCheckRead();
                Socket.Select(checkRead, null, null, 1000);
                // 检查可读对象 新连接上来的还是已经在线的客户端的消息
                for (int i = checkRead.Count-1; i>=0 ; i--) {
                    Socket s = checkRead[i];
                    if (s == listenfd)
                        ReadListenfd(s);
                    else
                        ReadClientfd(s);
                }
                // 超时
                Timer();
            }
        }
        
        // 每一次循环都会重新设置checkread列表
        public static void ResetCheckRead() {
            checkRead.Clear();
            checkRead.Add(listenfd);
            foreach (ClientState  s in clients.Values ) {
                checkRead.Add(s.socket);
            }   
        }

        // 处理监听消息
        public static void ReadListenfd(Socket listenfd) {
            try {
                Socket clientfd = listenfd.Accept();
                Console.WriteLine("Accept :" + clientfd.RemoteEndPoint.ToString());
                ClientState state = new ClientState();
                state.socket = clientfd;
                clients.Add(clientfd, state);
            }
            catch (SocketException e ) {
                Console.WriteLine(e.ToString());
            }
        }

        // 处理客户端消息
        public static void ReadClientfd(Socket clientfd) {
            ClientState state = clients[clientfd];
            ByteArray readBuff = state.readBuff;
            // 接收
            int count = 0;
            // 缓冲区不够,清除,如果依旧不够,只能返回
            // 缓冲区只有1024,若单条协议超过缓冲区长度会发生错误.根据需求调整长度
            if (readBuff.remain <= 0) {
                OnReceiveData(state);
                readBuff.MoveBytes();
            };
            if (readBuff.remain <= 0) {
                Console.WriteLine("Receive Fail ,maybe msg length >buff capacity");
                Close(state);
                return;
            }
            try {
                count = clientfd.Receive(readBuff.bytes, 
                                         readBuff.writeIdx, readBuff.remain, 0);
            }
            catch (SocketException e ) {
                Console.WriteLine("Receive SocketFail :" + e.ToString());
                Close(state);
                return;
            }
            // 客户端下线
            if (count <= 0) {
                Console.WriteLine("SocketClose " + clientfd.RemoteEndPoint.ToString());
                Close(state);
                return;
            }
            // 消息处理
            readBuff.writeIdx += count;
            // 处理二进制流
            OnReceiveData(state);
            // 移动缓冲区
            readBuff.CheckAndMoveBytes();
        }

        // 关闭连接
        public static void Close(ClientState state) {
            // 事件分发
            MethodInfo mei = typeof(MyServer.EventHandler).GetMethod("OnDisconnect");

			object[] ob = { state };
            mei.Invoke(null, ob);
            // 关闭并移除
            state.socket.Close();
            clients.Remove(state.socket);
        }

        // 消息处理
        public static void OnReceiveData(ClientState state) {
            ByteArray readBuff = state.readBuff;
            // 消息长度
            if (readBuff.length <= 2) {
                return;
            }
            Int16 bodyLength = readBuff.ReadInt16();
            // 消息体
            if (readBuff.length < bodyLength)
                return;
            // 解析协议名
            int nameCount = 0;
            string protoName = MsgBase.DecodeName(readBuff.bytes, 
                                                  readBuff.readIdx, out nameCount);
            if (protoName == "") {
                Console.WriteLine("OnReceiveData MsgBase .DecodeName fail...");
                Close(state);
            }
            readBuff.readIdx += nameCount;
            // 解析协议体
            int bodyCount = bodyLength - nameCount;
            MsgBase msgBase = MsgBase.Decode(protoName, readBuff.bytes,
                                             readBuff.readIdx, bodyCount);
            readBuff.readIdx += bodyCount;
            readBuff.CheckAndMoveBytes();
            // 分发消息
            MethodInfo mi = typeof(MsgHandler).GetMethod(protoName );
            object[] o = { state, msgBase };
            Console.WriteLine("Receive " + protoName);
            if (mi != null) {
                mi.Invoke(null, o);
            } else {
                Console.WriteLine("OnReceiveData Invoke fail " + protoName);
            }
            // 继续读取消息
            if (readBuff.length > 2)
                OnReceiveData(state);
        }

        public static void Timer() {
            MethodInfo mei = typeof(MyServer.EventHandler).GetMethod("OnTimer");
            object[] ob = { };
            mei.Invoke(null, ob);
        }
        
// 我们将客户端的发送代码注释掉了,所以我们也要单独写一个方法,来广播消息: // <<<<<<<<<<<<<<<<<<<< 
// 发送
        public static void Send(ClientState cs, string sendStr) {
            byte[] sendBytes = System.Text.Encoding.Default.GetBytes(sendStr);
            cs.socket.Send(sendBytes);
        }
    }
}
