using System;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace hashP2P
{
    // modified the MusicData Class a little bit
    // use a string Message to store the hashvalue for every file
    public class MusicData
    {
        public string Filename { get; set; }
        public string Title { get; set; }
        public string Singer { get; set; }
        public string Album { get; set; }
        public string Hashvalue { get; set; }
        public int Size { get; set; }

        public MusicData(string filename, string title, string singer, string album, int size)
        {
            this.Filename = filename;
            this.Title = title;
            this.Singer = singer;
            this.Album = album;
            this.Hashvalue = hashvalue;
            this.Size = size;
        }

    }

    public class sendMessage
    {
        public static void testMain()
        {
            // messageSender side
            FileStream fs = new FileStream("F:\\music.wav", FileMode.Open);
            RIPEMD160 myRIPEMD160 = RIPEMD160Managed.Create();
            byte[] hashvalue = myRIPEMD160.ComputeHash(fs);
            string hash = convertHashValue(hashvalue);

            MusicData music = new MusicData("F:\\music.wav", "music","singer","none", hash);
            byte[] output = System.Text.Encoding.UTF8.GetBytes(music.Message);

            //I assume we only send the byte array output back to requester
            // messageReceiver side
            string recover = convertHashValue(output); 
            Console.WriteLine(recover);

            Console.ReadLine();
            fs.Close();
            return;
        }

        public static string convertHashValue(byte[] hashvalue)
        {
            var sb = new StringBuilder("{ ");
            foreach (var b in hashvalue)
            {
                sb.Append(b + " ");
                // we can change this format accordingly
            }
            sb.Append("}");
            return sb.ToString();
        }

    }

}
}
