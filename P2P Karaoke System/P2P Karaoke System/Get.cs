using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;
using System.Runtime.Serialization.Formatters.Binary;

namespace P2P_Karaoke_System
{
    [Serializable]
    class GetRequest
    {
        private string filename{ get; set; }
        private string md5 { get; set; }
        private int startByte { get; set; }
        private int endByte { get; set; }

        public string GetFilename()
        {
            return this.filename;
        }

        public string GetMd5()
        {
            return this.md5;
        }

        public int GetStartByte()
        {
            return this.startByte;
        }

        public int GetEndByte()
        {
            return this.endByte;
        }

        public GetRequest(string filename, string md5, int startByte, int endByte)
        {
            this.filename = filename;
            this.md5 = md5;
            this.startByte = startByte;
            this.endByte = endByte;
        }

        public byte[] ToByte()
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, this);
            return ms.ToArray();
        }

        public static Object toObject(byte[] arrBytes)
        {
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            object obj = (object)binForm.Deserialize(memStream);
            return obj;
        }
    }

    [Serializable]
    class GetResponse
    {
        private ushort status;
        private string msg;
        private string filname;
        private string md5;
        private int startByte;
        private int endByte;
        private byte[] data;

        public GetResponse(string filename, string md5)
        {
            this.filname = filename;
            this.md5 = md5;
        }

        public void SetStatus(ushort status)
        {
            this.status = status;
        }

        public void SetMsg(string msg)
        {
            this.msg = msg;
        }

        public ushort GetStatus()
        {
            return this.status;
        }

        public string GetMsg()
        {
            return this.msg;
        }

        public string GetMd5()
        {
            return this.md5;
        }

        public int GetStartByte()
        {
            return this.startByte;
        }

        public int GetEndByte()
        {
            return this.endByte;
        }

        public bool CopyData(byte[] dst)
        {
            if (dst.Length < endByte)
            {
                Console.WriteLine("data length = {0}, start = {1}, end = {2}", dst.Length, this.startByte, this.endByte);
                return false;
            }
            this.data.CopyTo(dst, this.startByte);
            return true;
        }

        public void GetData(FileStream fs, string oldMd5, int startByte, int endByte)
        {
            if (endByte <= startByte)
            {
                return;
            }
            
            //MD5 myMD5 = MD5.Create();
            //byte[] hashvalue = myMD5.ComputeHash(fs);
            //string hash = ConvertHashValue(hashvalue);

            //Console.WriteLine("oldmd5 = {0}", oldMd5);
            //Console.WriteLine("newmd5 = {0}", hash);
            //if (String.Compare(oldMd5, hash, true) == 0)
            //{
            //    this.md5 = hash;
                this.status = 1;
                this.msg = "OK";
                this.startByte = startByte;
                this.endByte = endByte;

                int segSize = endByte - startByte + 1;
                this.data = new byte[segSize];
                fs.Seek(startByte, SeekOrigin.Begin);
                fs.Read(this.data, 0, segSize);
            //}
            //else
            //{
            //    this.status = 2;
            //    this.msg = "File Modified";
            //}
        }

        public static string ConvertHashValue(byte[] hashvalue)
        {
            var sb = new StringBuilder("");
            foreach (var b in hashvalue)
            {
                sb.Append(b.ToString("x2"));
                // we can change this format accordingly
            }
            return sb.ToString();
        }

        public byte[] ToByte()
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, this);
            return ms.ToArray();
        }

        public static Object toObject(byte[] arrBytes)
        {
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            object obj = (object)binForm.Deserialize(memStream);
            return obj;
        }
    }
}
