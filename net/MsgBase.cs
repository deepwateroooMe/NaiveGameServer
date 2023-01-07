using Microsoft.AspNet.SignalR.Json;
using Nancy.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyServer.net {

	public class MsgBase {
        // 协议名
        public string protoName = "";

        // //编码
        // public static byte[] Encode(MsgBase msgBase){
        //     string s = JsonUtility.ToJson(msgBase); 
        //     return System.Text.Encoding.UTF8.GetBytes(s);
        // }

        // //解码
        // public static MsgBase Decode(string protoName, byte[] bytes, int offset, int count){
        //     string s = System.Text.Encoding.UTF8.GetString(bytes, offset, count);
        //     MsgBase msgBase = (MsgBase)JsonUtility.FromJson(s, Type.GetType(protoName));
        //     return msgBase;
        // }

        // //编码协议名（2字节长度+字符串）
        // public static byte[] EncodeName(MsgBase msgBase){
        //     //名字bytes和长度
        //     byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(msgBase.protoName);
        //     Int16 len = (Int16)nameBytes.Length;
        //     //申请bytes数值
        //     byte[] bytes = new byte[2+len];
        //     //组装2字节的长度信息
        //     bytes[0] = (byte)(len%256);
        //     bytes[1] = (byte)(len/256);
        //     //组装名字bytes
        //     Array.Copy(nameBytes, 0, bytes, 2, len);

        //     return bytes;
        // }

        // //解码协议名（2字节长度+字符串）
        // public static string DecodeName(byte[] bytes, int offset, out int count){
        //     count = 0;
        //     //必须大于2字节
        //     if(offset + 2 > bytes.Length){
        //         return "";
        //     }
        //     //读取长度
        //     Int16 len = (Int16)((bytes[offset+1] << 8 )| bytes[offset] );
        //     //长度必须足够
        //     if(offset + 2 + len > bytes.Length){
        //         return "";
        //     }
        //     //解析
        //     count = 2+len;
        //     string name = System.Text.Encoding.UTF8.GetString(bytes, offset+2, len);
        //     return name;
        // }

        // 实例json解析类
        static JavaScriptSerializer js = new JavaScriptSerializer();

        // 编码协议名
        // <param name="msgBase">继承MsgBase的子类</param>
        // <returns>byte[]类型协议名 </returns>
        public static byte[] EncodeName(MsgBase msgBase) {
            byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(msgBase.protoName);
            // 协议名长度
            Int16 len = (Int16)nameBytes.Length;
            byte[] bytes = new byte[2 + len];
            // 组装协议长度byte数组
            bytes[0] = (byte)(len % 256);
            bytes[1] = (byte)(len / 256); 
            Array.Copy(nameBytes, 0, bytes, 2, len);
            return bytes;
        }

        // 解码协议名
        // <param name="bytes">协议字节数组</param>
        // <param name="offset">开始解码的字节下标</param>
        // <param name="count"></param>
        public static string DecodeName(byte[] bytes, int offset, out int count) {
            count = 0;
            // 
            if (offset + 2 > bytes.Length)
                return "";
            // 读取长度
            Int16 len = (Int16)(bytes[offset + 1] << 8 | bytes[offset]);
            if (offset + 2 + len > bytes.Length)
                return "";
            // 解析协议名
            count = len + 2;// 在字节数组占用的总长度
            string name = System.Text.Encoding.UTF8.GetString(bytes, offset + 2, len);
            return name;
        }

        // json->byte[]
        public static byte[] Encode(MsgBase msgBase) {
            string s = js.Serialize(msgBase);
            return System.Text.Encoding.UTF8.GetBytes(s);
        }

        // byte[] ->json
        // <param name="protoName">协议名</param>
        // <param name="bytes">json字节</param>
        // <param name="offset">便宜量</param>
        // <param name="count">要转换的长度</param>
        public static MsgBase Decode(string protoName, byte[] bytes, int offset, int count) {
            Console.WriteLine("解析出来的协议名:" + protoName);
            string s = System.Text.Encoding.UTF8.GetString(bytes, offset, count);
            MsgBase msgBase = js.Deserialize<MsgBase>(s); // , Type.GetType(protoName)
			return msgBase;
        }
	}

    public class MsgMove:MsgBase {
        public MsgMove() {protoName = "MsgMove";}

        public int x = 0;
        public int y = 0;
        public int z = 0;
    }

    public class MsgAttack:MsgBase {
        public MsgAttack() {protoName = "MsgAttack";}

        public string desc = "127.0.0.1:6543";
    }
    
}
