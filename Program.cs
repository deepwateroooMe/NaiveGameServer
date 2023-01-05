using MyServer;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Reflection;

namespace DW.Server.App {
    class Program {

// 选用一种查找比较快速的数据结构:字典 服务器就必须要有个[数据结构]用来存放(维护管理)所有连接着它的客户端
// 可是问题是：这不是多纯种不案例的字典吗？他又没有用多纯种案例类型的ConcurrentDictionary之类的？也没有上锁？        
        public static Dictionary<Socket, ClientState> clients = new Dictionary<Socket, ClientState>();

// 用Select来改写服务器端.         
        // checkRead列表
        static List<Socket> checkRead = new List<Socket>();
        
        // public Socket BeginAccept (IAsyncResult asyncResult) { }
        // public Socket EndAccept (IAsyncResult asyncResult) { }
        public static void AcceptCallback(IAsyncResult ar) { // 服务器如果接收到了客户端的连接,并且已经连接成功了,这个回调函数里面应当做些什么呢?
            try {
                Console.WriteLine("接收客户端连接成功 ");
                Socket listenfd = ar.AsyncState as Socket; // 第一应该是接收它socket的一个状态
                Socket clientfd = listenfd.EndAccept(ar);  // 其次,连接好了之后就应该不在做Accept的操作了
                // 3. 要为每一个新连接进来的客户端分配一个ClientState  ,然后将此客户端在服务器中实例的socket作为字典的Key值存入字典中
                ClientState state = new ClientState();
                state.socket = clientfd;
                clients.Add(clientfd, state);
                // 4. 接收客户端发送过来的消息:　当然了,还是用异步方法,本来同步方法是Receive,那么这里肯定是BeginReceive了
                clientfd.BeginReceive(state.readBuff, 0, 1024, SocketFlags.None, ReceiveCallback, state); // <<<<<<<<<<<<<<<<<<<< 
                // 5. 处理完了当前客户端的事儿,接下来就可以 继续Accept其他客户端的连接
                listenfd.BeginAccept(AcceptCallback, listenfd);
            }
            catch (SocketException e ) {
                Console.WriteLine("接收客户端连接错误: " + e.Message);
            }
        }
        
// 要完善BeginReceive的回调函数了.当服务器确认和客户端建立起连接之后,开始回调这个方法
        public static void ReceiveCallback(IAsyncResult ar)　{
            try　{
                Console.WriteLine("接收客户端数据成功: ");
                ClientState state = ar.AsyncState as ClientState;
                Socket clientfd = state.socket;
                int count = clientfd.EndReceive(ar);
                // 客户端关闭
                if (count <= 0) { // 当服务器确认收到了客户端的信息,但是这个信息的长度小于等于0时,表示Socket可以断开
                    clientfd.Close();
                    clients.Remove(clientfd);
                    Console.WriteLine("Socket Closed ");
                    return;
                }
                string readStr = System.Text.Encoding.Default.GetString(state .readBuff, 0, count);
                Console.WriteLine("接收客户端数据成功: " + readStr);
                byte[] sendBytes = System.Text.Encoding.Default.GetBytes(readStr);
                clientfd.Send(sendBytes); // <<<<<<<<<<<<<<<<<<<< 
                clientfd.BeginReceive(state.readBuff, 0, count, SocketFlags.None, ReceiveCallback, state);
            }
            catch (SocketException e) {
                Console.WriteLine("接收客户端消息失败: " + e.Message);
            }
        }
        //public Socket EndAccept (IAsyncResult asyncResult) { }

        public static void ReadListenfd(Socket listenfd) {
            Console.WriteLine("Accpet");
            Socket clientfd = listenfd.Accept();
            ClientState state = new ClientState();
            state.socket = clientfd ;
            clients.Add(clientfd, state);
        }

// 处理每个在线客户端的消息        
        public static bool ReadClientfd(Socket clientfd) {
            ClientState state = clients[clientfd];
            // 接收字节
            int count = 0;
            try {
                count = clientfd.Receive(state.readBuff);
            }
            catch (SocketException e) {
                // 下线时调用反射
                MethodInfo mei = typeof(MyServer.EventHandler).GetMethod("OnDisConnect");
                object[] ob = { state };
                mei.Invoke(null, ob);
                
                clientfd.Close();
                clients.Remove(clientfd);
                Console.WriteLine("异常报告: "+ e.Message);
                return false;
            }
            // 如果客户端强行下线
            if (count <= 0) {
                // 下线时调用反射
                MethodInfo mei = typeof(MyServer.EventHandler).GetMethod("OnDisConnect");
                object[] ob = { state };
                mei.Invoke(null, ob);

                clientfd.Close();
                clients.Remove(clientfd);
                Console.WriteLine("客户端　socket 已关闭");
                return false;
            }
            // 数据处理: 数据的解析
            string recvStr = Encoding.Default.GetString(state.readBuff, 0, count);
            Console.WriteLine("Receive :　"+　recvStr ); // 比如客户端来的消息是:Enter|127.0.0.1:12315
            string[] split = recvStr.Split('|');
            string msgName = split[0];
            string msgArgs = split[1]; // bug here TODO
            string funName = "Msg" + msgName;
            MethodInfo mi = typeof(MsgHandler).GetMethod(funName);
            object[] o = { state, msgArgs };
            mi.Invoke(null, o);
// 将客户端的发送代码注释掉了 // <<<<<<<<<<<<<<<<<<<<           
//             // 需要发送给客户端的
//             // string sendStr = clientfd.RemoteEndPoint.ToString() + ":" + recvStr;
//             // byte[] sendBytes = Encoding.Default.GetBytes(sendStr);
//             string sendStr = recvStr;
//             byte[] sendBytes = Encoding.Default.GetBytes(sendStr);
//             foreach (ClientState item in clients.Values ) {　// 广播: 遍历服务器所维护的所有连接着的客户端,拿到客户端信道索引,异步发消息
// // 这里，它前面说，好像是先发到操作系统底层的缓冲区，然后我们程序员就不用管了 ？ 操作系统会帮我们完成管道发送 ？
//                     item.socket.Send(sendBytes); // 这里应该也是异步的? 还是说对于这个客户端的信道来说,是对于客户端来说的同步收消息呢(对于服务器端来说是异步)?
//             }
            return true;
        }
// 我们将客户端的发送代码注释掉了,所以我们也要单独写一个方法,来广播消息: // <<<<<<<<<<<<<<<<<<<< 
// 发送
        public static void Send(ClientState cs, string sendStr) {
            byte[] sendBytes = System.Text.Encoding.Default.GetBytes(sendStr);
            cs.socket.Send(sendBytes);
        }

        static void Main(string [] args) {
            Socket listenfd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress ipAdr = IPAddress.Parse("127.0.0.1");
            IPEndPoint iPEP = new IPEndPoint(ipAdr, 5000);
            listenfd.Bind(iPEP);
            listenfd.Listen(0);
            Console.WriteLine("服务器启动完毕");

            while (true) {
                checkRead.Clear();
// 首先,将监听socket(listenfd)和客户端socket(clients列表)添加到待检测的checkRead列表中.
// 那么为什么要将这2种socket都加入到checkRead中呢?
// 因为checkread只是检查socket是否可读,不管你什么情况下的可读,如果你是要连接的话,那么就调用连接的方法,如果不是,那就调用读取信息的方法.                
                checkRead.Add(listenfd);
                // 遍历所有已连接的客户端
                foreach (var item in clients.Values) {
                    checkRead.Add(item.socket); 
                }
                // 开始select
                Socket.Select(checkRead, null, null, 1); // 其实要将程序中的等待时间设置为1s,也就是说程序”卡”在这里1s,在这1s当中,没有任何消息过来,那么清空该列表.
                // 最后开始遍历所有有可读消息的socket,如果时请求连接的,就调用调用请求连接的方法:ReadListenfd,如果是有消息发过来,那么就调用接收消息的方法:ReadClientfd.
                foreach (var item in checkRead){
                    if (item == listenfd) {
                        ReadListenfd(item);
                    } else  {
                        ReadClientfd(item);
                    }
                }
                // 那么这里并没有使用到CheckWrite和CheckError的列表参数,放心,后面会用到的因为这里发送消息直接放在ReadClientfd,后续做服务器的时候会改写的.
            }
// // 异步模式的服务器
//             while (true) {
//                 // 有客户端请求连接
// // 要给就是Poll中的等待时间为0,如果设置的时间过高,那么服务器就没办法及时处理消息,
//                 // 当客户端连接进来比较多的时候,每一个客户端都需要等待一段时间,那么可能会造成网络拥堵,最后服务器死机.                
//                 if (listenfd.Poll(0, SelectMode.SelectRead)) {
//                     ReadListenfd(listenfd);
//                 }
//                 // 遍历所有客户端,用来分发消息
//                 foreach (var item in clients.Values) {
//                     Socket clientfd = item.socket;
//                     //如果客户端有消息
//                     if (clientfd.Poll(0, SelectMode.SelectRead)) {
//                         if (!ReadClientfd(clientfd)) // 当返回false的时候就代表里面的count为0 ,即断开连接,断开之后,同样会在字典中移除该客户端的缓存
//                             // BUGGY BUGGY BUGGY BUG BUG BUG
//                             break; // 为什么移除了一个断线的客户端却要break掉遍历,那是因为当遍历的字典发生改变之后,遍历会报错.
//                     }
//                     System.Threading.Thread.Sleep(1); // 为什么会给程序1毫秒的暂停,这样做是为了避免程序卡住,给它一个短暂的停歇.
//                 }
//             }

            // // 下面是同步的写法,全部改写成是异步的写法,因为一台服务器可能要同时连10000台客户端    
//             while (true) {
//                 // Accpt
//                 // Socket Client = listenfd.Accept(); // <<<<<<<<<<<<<<<<<<<< 改写成是异步模式: BeginAccept + EndAccept
//                 listenfd.BeginAccept(AcceptCallback, listenfd);
                
//                 // Console.WriteLine("[服务器]Accpet"); // 这里会打印千千万万遍
//                 // Receive
//                 // byte[] readBuff = new byte[1024]; // 这个长度只是从网络看见一般写这么长,我不需要这么长
//                 // int count = Client.Receive(readBuff); // <<<<<<<<<<<<<<<<<<<< 改写为异步的方法
                
//                 // Console.WriteLine(" count = " + count);
//                 // string readStr = System.Text.Encoding.Default.GetString(readBuff, 0, count);
//                 // Console.WriteLine("[服务器接收的消息:]" + readStr);
//                 // 发消息给客户端
//                 // byte[] sendBytes = System.Text.Encoding.Default.GetBytes(readStr);
//                 // Client.Send(sendBytes); // <<<<<<<<<<<<<<<<<<<< 改写为异步的方法
//             }
        }
    }
}