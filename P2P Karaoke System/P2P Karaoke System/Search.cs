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
        private MusicCopy[] result { get; set; }

        public SearchResponse(MusicCopy[] data)
        {
            this.result = data;
        }
    }
}
