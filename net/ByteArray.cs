using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyServer.net {

	public class ByteArray {

        // 默认大小
        const int DEFAULT_SIZE = 1024;
        // 初始大小
        int initSize = 0;
        // 设置缓冲区
        public byte[] bytes;
        // 读写位置
        public int readIdx = 0;
        public int writeIdx = 0; // 可写数据的下标
        // 容量
        private int capacity = 100; // 不知道这个说的是什么意思
        // 剩余空间
        public int remain {
            get { return capacity - writeIdx; }
        }
        // 此条消息的有效长度
        public int length {
            get { return writeIdx - readIdx; }
        }

        // 构造函数 recv
        public ByteArray(int size = DEFAULT_SIZE) {
            bytes = new byte[size]; 
            capacity = size;
            initSize = size;
            readIdx = 0;
            writeIdx = 0;
        }
        // 构造函数  
        public ByteArray(byte[] defaultBytes) {
            bytes = defaultBytes;
            capacity = defaultBytes.Length;
            initSize = defaultBytes.Length;
            readIdx = 0;
            writeIdx = defaultBytes.Length;
        }

        // 重设尺寸 size代表所需要的数据空间大小
        public void ReSize(int size) {
            // send空间
            if (size < length)
                return;
            // receive空间
            if (size < initSize)
                return;
            int n = 1;
            while (n < size)
                n *= 2;
            // n的长度为1,2,4,8,16,32,64,128,256增长
            capacity = n;
            // 将旧的byte数组复制到新的里面去
            byte[] newByte = new byte[capacity];
            // 将有效数据复制到新的数组里面
            Array.Copy(bytes, readIdx, newByte, 0, writeIdx - readIdx);
            bytes = newByte; 
            writeIdx = length;
            readIdx = 0;
        }

        // 检查并移动数据
        public void CheckAndMoveBytes() {
            if (length < 8) { // length < 
                MoveBytes();
            }
        }
        // 移动数据
        public void MoveBytes() {
            // 从readIdx开始复制到开头:
            // 原数组，源数组的起始位置，目标数组，目标数组的起始位置，要复制的消息长度
            Array.Copy(bytes, readIdx, bytes, 0, length);
            writeIdx = length;
            readIdx = 0;
        }

        // 数据的写入
        // <param name="bs">待写入的数据</param>
        // <param name="offset">可以写入数据的下标</param>
        // <param name="count">待写入的数据长度</param>
        public int Write(byte[] bs, int offset, int count) {
            if (remain < count)
                ReSize(length + count);
            // 将bs加入到bytes(缓冲区)中
            Array.Copy(bs, offset, bytes, writeIdx, count);
            writeIdx += count;
            return count;
        }
        // 读取数据
        // <param name="bs">读取的数据存放数组</param>
        // <param name="offset">从哪个下标开始读?这是下标</param>
        // <param name="count">读取多长,这是长度</param>
        public int Read(byte[] bs, int offset, int count) {
            count = Math.Min(count, length);
            // 将bytes中的数据从0开始复制到bs中,bs里面从offset开始,复制count长度
            Array.Copy(bytes, readIdx, bs, offset, count);
            // Debug.Log(BitConverter.ToString(bytes));
            readIdx += count;
            CheckAndMoveBytes();
            return count;
        }
        public Int16 ReadInt16() {
            if (length < 2)
                return 0;
            Int16 ret = (Int16)((bytes[1] << 8) | bytes[0]);
            readIdx += 2;
            CheckAndMoveBytes();
            return ret;
        }
        public Int32 ReadInt32() {
            if (length < 4)
                return 0;
            Int32 ret = (Int32)((bytes[3] << 24) |
                                (bytes[2] << 16) |
                                (bytes[1] << 8) |
                                bytes[0]);
            
            readIdx += 4;
            CheckAndMoveBytes();
            return ret;
        }
	}
}
