using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Reflection;
using MyServer.logic;
using MyServer.net;
using MyServer.db;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MyServer {

// 那么，现在就从写一个最简单的文件服务器开始，供我存放几个热更新程序热更新包，和资源热更新包就可以了。
// 最开始，也没什么人玩儿我写的游戏，不用对服务器作负载压力测试什么的，先把它运行起来，具备第一个游戏热更新最基本的逻辑就可以了
// 这么看，文件服务器可以狠简单，仍有点儿小家子气，可是我的游戏也不是大型商业网络游戏。。。。。
// 将来弄个服务器可以热更新的热更新服务器比较好    
    class Program {

        private static int count = 0;
        static HttpListener httpobj;

        static void Main(string[] args) {
            // 提供一个简单的、可通过编程方式控制的 HTTP 协议侦听器。此类不能被继承。
            httpobj = new HttpListener();
            // 定义url及端口号，通常设置为配置文件
            httpobj.Prefixes.Add("http://127.0.0.1:8080/");
            // 启动监听器
            httpobj.Start(); // 这里抛了个拒绝访问的异常，需要解决一下
            
            // 异步监听客户端请求，当客户端的网络请求到来时会自动执行Result委托
            // 该委托没有返回值，有一个IAsyncResult接口的参数，可通过该参数获取context对象
            httpobj.BeginGetContext(Result, null);
            Console.WriteLine($"服务端初始化完毕，正在等待客户端请求,时间：{DateTime.Now.ToString()}\r\n");
            Console.ReadKey();
        }
 
        private static void Result(IAsyncResult ar) {
            // 当接收到请求后程序流会走到这里
            // 继续异步监听
            httpobj.BeginGetContext(Result, null);
            var guid = Guid.NewGuid().ToString();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"接到第{++count}个请求:{guid},时间：{DateTime.Now.ToString()}");
            // 获得context对象
            var context = httpobj.EndGetContext(ar);
            var request = context.Request;
            var response = context.Response;
            // 如果是js的ajax请求，还可以设置跨域的ip地址与参数
            // context.Response.AppendHeader("Access-Control-Allow-Origin", "*");// 后台跨域请求，通常设置为配置文件
            // context.Response.AppendHeader("Access-Control-Allow-Headers", "ID,PW");// 后台跨域参数设置，通常设置为配置文件
            // context.Response.AppendHeader("Access-Control-Allow-Method", "post");// 后台跨域请求设置，通常设置为配置文件
            context.Response.ContentType = "text/plain;charset=UTF-8";// 告诉客户端返回的ContentType类型为纯文本格式，编码为UTF-8
            context.Response.AddHeader("Content-type", "text/plain");// 添加响应头信息
            context.Response.ContentEncoding = Encoding.UTF8;
            string returnObj = null;// 定义返回客户端的信息
            if (/*request.HttpMethod == "POST" &&*/ request.InputStream != null) {
                // 处理客户端发送的请求并返回处理信息
                returnObj = HandleRequest(request, response);
            } else {
                returnObj = $"不是post请求或者传过来的数据为空";
            }
       
            var returnByteArr = Encoding.UTF8.GetBytes(returnObj);// 设置客户端返回信息的编码
            var returnJson = Encoding.UTF8.GetBytes(GetJsonFile("../../../BerthEdges.json"));// 设置客户端返回信息的编码
            try {  
                using (var stream = response.OutputStream) {
                    // 把处理信息返回到客户端
                    stream.Write(returnJson, 0, returnJson.Length);
                    // stream.Write(returnByteArr, 0, returnByteArr.Length);
                }
            }
            catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"网络蹦了：{ex.ToString()}");
            }
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"请求处理完成：{guid},时间：{ DateTime.Now.ToString()}\r\n");
        }
        private static string HandleRequest(HttpListenerRequest request, HttpListenerResponse response) {
            string data = null;
            try {
                var byteList = new List<byte>();
                var byteArr = new byte[2048];
                int readLen = 0;
                int len = 0;
                // 接收客户端传过来的数据并转成字符串类型
                do {
                    readLen = request.InputStream.Read(byteArr, 0, byteArr.Length);
                    len += readLen;
                    byteList.AddRange(byteArr);
                
                } while (readLen != 0);
                data = Encoding.UTF8.GetString(byteList.ToArray(),0, len);
                // 获取得到数据data可以进行其他操作
            }
            catch (Exception ex) {
                response.StatusDescription = "404";
                response.StatusCode = 404;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"在接收数据时发生错误:{ex.ToString()}");
                return $"在接收数据时发生错误:{ex.ToString()}";// 把服务端错误信息直接返回可能会导致信息不安全，此处仅供参考
            }
            response.StatusDescription = "200";// 获取或设置返回给客户端的 HTTP 状态代码的文本说明。
            response.StatusCode = 200;// 获取或设置返回给客户端的 HTTP 状态代码。
            Console.ForegroundColor = ConsoleColor.Green;
            var a = request.QueryString["id"];// 获取客户端发送过来的参数
            Console.WriteLine($"客户端通过{request.HttpMethod}方式发来的数据:{data},参数:{request.QueryString["id"]}时间：{DateTime.Now.ToString()}");
            return $"接收数据完成";
        }
        // 读取json文件
        public static string GetJsonFile(string jsonfile) {
            string jsonstr = "";
            using (System.IO.StreamReader file = System.IO.File.OpenText(jsonfile)) {
                using (JsonTextReader reader = new JsonTextReader(file)) {
                    JObject o = (JObject)JToken.ReadFrom(reader);
                    jsonstr = o.ToString(Formatting.None);// 格式化输出Json用Formatting.Indented
                }
            }
            return jsonstr;
        }
        
        
//         static void Main(string [] args) {
//             // 如果数据库连接失败,就不用再往下执行了: 好像是这里的语法写得不对
//             if (!DbManager.Connect("game", "127.0.0.1", 5000, "root", "")) {
//                 // if (!DbManager.Connect("game", "(localdb)\\MSSQLLocalDB", 5000, "root", "")) {
//                 return;
//             }
//             // 测试数据库写入功能
//             if (DbManager.Register("Vincent", "123456")) {
//                 Console.WriteLine("注册成功");
//             }
//             PlayerData playerData = new PlayerData {
//                 coin = 100,
//                 text = "Hello ,Yeon"
//             };
//             bool v = DbManager.UpdatePlayerData("Yeon", playerData); 
//             Console.ReadKey();
            
// //             Socket listenfd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
// //             IPAddress ipAdr = IPAddress.Parse("127.0.0.1");
// //             IPEndPoint iPEP = new IPEndPoint(ipAdr, 5000);
// //             listenfd.Bind(iPEP);
// //             listenfd.Listen(0);
// //             Console.WriteLine("服务器启动完毕");
// //             while (true) {
// //                 checkRead.Clear();
// // // 首先,将监听socket(listenfd)和客户端socket(clients列表)添加到待检测的checkRead列表中.
// // // 那么为什么要将这2种socket都加入到checkRead中呢?
// // // 因为checkread只是检查socket是否可读,不管你什么情况下的可读,如果你是要连接的话,那么就调用连接的方法,如果不是,那就调用读取信息的方法.                
// //                 checkRead.Add(listenfd);
// //                 // 遍历所有已连接的客户端
// //                 foreach (var item in clients.Values) {
// //                     checkRead.Add(item.socket); 
// //                 }
// //                 // 开始select
// //                 Socket.Select(checkRead, null, null, 1); // 其实要将程序中的等待时间设置为1s,也就是说程序”卡”在这里1s,在这1s当中,没有任何消息过来,那么清空该列表.
// //                 // 最后开始遍历所有有可读消息的socket,如果时请求连接的,就调用调用请求连接的方法:ReadListenfd,如果是有消息发过来,那么就调用接收消息的方法:ReadClientfd.
// //                 foreach (var item in checkRead){
// //                     if (item == listenfd) {
// //                         ReadListenfd(item);
// //                     } else  {
// //                         ReadClientfd(item);
// //                     }
// //                 }
// //             }
//         }
        
// 选用一种查找比较快速的数据结构:字典 服务器就必须要有个[数据结构]用来存放(维护管理)所有连接着它的客户端
// 可是问题是：这不是多纯种不案例的字典吗？他又没有用多纯种案例类型的ConcurrentDictionary之类的？也没有上锁？        
        public static Dictionary<Socket, ClientState> clients = new Dictionary<Socket, ClientState>();
// 用Select来改写服务器端.         
        // checkRead列表
        static List<Socket> checkRead = new List<Socket>();
        
//         public static void AcceptCallback(IAsyncResult ar) { // 服务器如果接收到了客户端的连接,并且已经连接成功了,这个回调函数里面应当做些什么呢?
//             try {
//                 Console.WriteLine("接收客户端连接成功 ");
//                 Socket listenfd = ar.AsyncState as Socket; // 第一应该是接收它socket的一个状态
//                 Socket clientfd = listenfd.EndAccept(ar);  // 其次,连接好了之后就应该不在做Accept的操作了
//                 // 3. 要为每一个新连接进来的客户端分配一个ClientState  ,然后将此客户端在服务器中实例的socket作为字典的Key值存入字典中
//                 ClientState state = new ClientState();
//                 state.socket = clientfd;
//                 clients.Add(clientfd, state);
//                 // 4. 接收客户端发送过来的消息:　当然了,还是用异步方法,本来同步方法是Receive,那么这里肯定是BeginReceive了
//                 clientfd.BeginReceive(state.readBuff, 0, 1024, SocketFlags.None, ReceiveCallback, state); // <<<<<<<<<<<<<<<<<<<< 
//                 // 5. 处理完了当前客户端的事儿,接下来就可以 继续Accept其他客户端的连接
//                 listenfd.BeginAccept(AcceptCallback, listenfd);
//             }
//             catch (SocketException e ) {
//                 Console.WriteLine("接收客户端连接错误: " + e.Message);
//             }
//         }
        
// // 要完善BeginReceive的回调函数了.当服务器确认和客户端建立起连接之后,开始回调这个方法
//         public static void ReceiveCallback(IAsyncResult ar)　{
//             try　{
//                 Console.WriteLine("接收客户端数据成功: ");
//                 ClientState state = ar.AsyncState as ClientState;
//                 Socket clientfd = state.socket;
//                 int count = clientfd.EndReceive(ar);
//                 // 客户端关闭
//                 if (count <= 0) { // 当服务器确认收到了客户端的信息,但是这个信息的长度小于等于0时,表示Socket可以断开
//                     clientfd.Close();
//                     clients.Remove(clientfd);
//                     Console.WriteLine("Socket Closed ");
//                     return;
//                 }
//                 string readStr = System.Text.Encoding.Default.GetString(state .readBuff, 0, count);
//                 Console.WriteLine("接收客户端数据成功: " + readStr);
//                 byte[] sendBytes = System.Text.Encoding.Default.GetBytes(readStr);
//                 clientfd.Send(sendBytes); // <<<<<<<<<<<<<<<<<<<< 
//                 clientfd.BeginReceive(state.readBuff, 0, count, SocketFlags.None, ReceiveCallback, state);
//             }
//             catch (SocketException e) {
//                 Console.WriteLine("接收客户端消息失败: " + e.Message);
//             }
//         }
        //public static void ReadListenfd(Socket listenfd) {
        //    Console.WriteLine("Accpet");
        //    Socket clientfd = listenfd.Accept();
        //    ClientState state = new ClientState();
        //    state.socket = clientfd ;
        //    clients.Add(clientfd, state);
        //}
// // 处理每个在线客户端的消息        
//         public static bool ReadClientfd(Socket clientfd) {
//             ClientState state = clients[clientfd];
//             // 接收字节
//             int count = 0;
//             try {
//                 count = clientfd.Receive(state.readBuff);
//             }
//             catch (SocketException e) {
//                 // 下线时调用反射
//                 MethodInfo mei = typeof(logic.EventHandler).GetMethod("OnDisConnect");
//                 object[] ob = { state };
//                 mei.Invoke(null, ob);
                
//                 clientfd.Close();
//                 clients.Remove(clientfd);
//                 Console.WriteLine("异常报告: "+ e.Message);
//                 return false;
//             }
//             // 如果客户端强行下线
//             if (count <= 0) {
//                 // 下线时调用反射
//                 MethodInfo mei = typeof(logic.EventHandler).GetMethod("OnDisConnect");
//                 object[] ob = { state };
//                 mei.Invoke(null, ob);
//                 clientfd.Close();
//                 clients.Remove(clientfd);
//                 Console.WriteLine("客户端　socket 已关闭");
//                 return false;
//             }
//             // 数据处理: 数据的解析
//             string recvStr = Encoding.Default.GetString(state.readBuff, 0, count);
//             Console.WriteLine("Receive :　"+　recvStr ); // 比如客户端来的消息是:Enter|127.0.0.1:12315
//             string[] split = recvStr.Split('|');
//             string msgName = split[0];
//             string msgArgs = split[1]; // bug here TODO
//             string funName = "Msg" + msgName;
//             MethodInfo mi = typeof(MsgHandler).GetMethod(funName);
//             object[] o = { state, msgArgs };
//             mi.Invoke(null, o);
// // 将客户端的发送代码注释掉了 // <<<<<<<<<<<<<<<<<<<<           
// //             // 需要发送给客户端的
// //             // string sendStr = clientfd.RemoteEndPoint.ToString() + ":" + recvStr;
// //             // byte[] sendBytes = Encoding.Default.GetBytes(sendStr);
// //             string sendStr = recvStr;
// //             byte[] sendBytes = Encoding.Default.GetBytes(sendStr);
// //             foreach (ClientState item in clients.Values ) {　// 广播: 遍历服务器所维护的所有连接着的客户端,拿到客户端信道索引,异步发消息
// // // 这里，它前面说，好像是先发到操作系统底层的缓冲区，然后我们程序员就不用管了 ？ 操作系统会帮我们完成管道发送 ？
// //                     item.socket.Send(sendBytes); // 这里应该也是异步的? 还是说对于这个客户端的信道来说,是对于客户端来说的同步收消息呢(对于服务器端来说是异步)?
// //             }
//             return true;
//         }
// 我们将客户端的发送代码注释掉了,所以我们也要单独写一个方法,来广播消息: // <<<<<<<<<<<<<<<<<<<< 
// 发送
        public static void Send(ClientState cs, string sendStr) {
            byte[] sendBytes = System.Text.Encoding.Default.GetBytes(sendStr);
            cs.socket.Send(sendBytes);
        }
    }
}