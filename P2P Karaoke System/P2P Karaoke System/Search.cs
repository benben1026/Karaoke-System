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
    class SearchRequest
    {
        private string keyword { get; set; }

        public string GetKeyword()
        {
             return this.keyword;
        }

        public SearchRequest(string keyword)
        {
            this.keyword = keyword;
        }

        public byte[] ToByte()
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, this);
            return ms.ToArray();
        }

        public static Object ToObject(byte[] arrBytes)
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
    class SearchResponse
    {
        private ushort status;
        private string msg;
        private List<MusicCopy> result { get; set; }

        public ushort GetStatus()
        {
            return this.status;
        }

        public string GetMsg()
        {
            return this.msg;
        }

        public List<MusicCopy> GetResult()
        {
            return this.result;
        }


        public SearchResponse(List<MusicCopy> data)
        {
            if (data.Count() > 0)
            {
                this.status = 1;
                this.msg = "OK";
                this.result = data;
            }
            else
            {
                this.status = 2;
                this.msg = "No Results";
                this.result = data;
            }
        }

        public byte[] ToByte()
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, this);
            return ms.ToArray();
        }

        public static Object ToObject(byte[] arrBytes)
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
