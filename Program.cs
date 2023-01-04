﻿using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DW.Server.App {
    class Program {

        Socket listenfd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        IPAddress ipAdr = IPAddress.Parse("127.0.0.1");
        IPEndPoint iPEP = new IPEndPoint(ipAdr, 5000);
        listenfd.Bind(iPEP);

        listenfd.Listen(0);
        Console.WriteLine("服务器启动完毕");

// 异步模式的服务器
        while (true) {
            //有客户端请求连接
            if (listenfd.Poll(, SelectMode.SelectRead)) {
                ReadListenfd(listenfd);
            }
            //遍历所有客户端,用来分发消息
            foreach (var item in clients .Values) {
                Socket clientfd = item.socket;
                //如果客户端有消息
                if (clientfd.Poll(, SelectMode.SelectRead)) {
                    if (!ReadClientfd(clientfd)) {
                        break;
                    }
                }
                System.Threading.Thread.Sleep();
            }
        }
        while (true) {
// 下面是同步的写法,全部改写成是异步的写法,因为一台服务器可能要同时连10000台客户端    
            // Accpt
            // Socket Client = listenfd.Accept(); // <<<<<<<<<<<<<<<<<<<< 改写成是异步模式: BeginAccept + EndAccept
            listenfd.BeginAccept(AcceptCallback, listenfd);
    
            Console.WriteLine("[服务器]Accpet");
            // Receive
            byte[] readBuff = new byte[1024]; // 这个长度只是从网络看见一般写这么长,我不需要这么长
            int count = Client.Receive(readBuff); // <<<<<<<<<<<<<<<<<<<< 
            Console.WriteLine(" count = " + count);
            string readStr = System.Text.Encoding.Default.GetString(readBuff, 0, count);
            Console.WriteLine("[服务器接收的消息:]" + readStr);
            // 发消息给客户端
            byte[] sendBytes = System.Text.Encoding.Default.GetBytes(readStr);
            Client.Send(sendBytes); // <<<<<<<<<<<<<<<<<<<< 
        }

        
// 选用一种查找比较快速的数据结构:字典 服务器就必须要有个[数据结构]用来存放(维护管理)所有连接着它的客户端
        static Dictionary<Socket, ClientState> clients = new Dictionary<Socket, ClientState>();

        public Socket BeginAccept (IAsyncResult asyncResult) {
    
        }
// 服务器如果接收到了客户端的连接,并且已经连接成功了,这个回调函数里面应当做些什么呢?
        public static void AcceptCallback(IAsyncResult ar) {
            try {
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
            catch (SocketException e )
            {
                Console.WriteLine("接收客户端连接错误: " + e.Message);
            }

        }

// 要完善BeginReceive的回调函数了.当服务器确认和客户端建立起连接之后,开始回调这个方法
        public static void ReceiveCallback(IAsyncResult ar)　{
            try　{
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
                string readStr = System.Text.Encoding.Default.GetString(state .readBuff, , count);
                byte[] sendBytes = System.Text.Encoding.Default.GetBytes(readStr);
                clientfd.Send(sendBytes);
                clientfd.BeginReceive(state.readBuff, , , SocketFlags.None, ReceiveCallback, state);

            }
            catch (SocketException e) {
                Console.WriteLine("接收客户端消息失败: " + e.Message);
            }
        }
        public Socket EndAccept (IAsyncResult asyncResult) {
    
        }
// 服务器向所有客户端广播消息：不完整的代码 片段
// 这个功能: 客户端 还没实现        
        public void broadMsgs() { // 遍历服务器所维护的所有连接着的客户端,拿到客户端信道索引,异步发消息
            foreach (ClientState  item in clients.Values) {
                item.socket.Send(sendBytes); // 这里应该也是异步的? 还是说对于这个客户端的信道来说,是对于客户端来说的同步收消息呢(对于服务器端来说是异步)?
            }
        }
        public static void ReadListenfd(Socket listenfd) {
                Console.WriteLine("Accpet");
                Socket clientfd = listenfd.Accept();
                ClientState state = new ClientState();
                state.socket = clientfd ;
                clients.Add(clientfd, state);

            }
    }   
}