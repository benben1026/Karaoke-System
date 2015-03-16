using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2P_Karaoke_System
{
    public  struct CopyIndex
    {
        public int UserIndex;
        public string FileName;
        //public string IPAddress;

        public CopyIndex(int userIndex, string fileName, string ipAddress)
        {
            UserIndex = userIndex;
            FileName = fileName;
            //IPAddress = ipAddress;
        }
    }

    public class MusicCopy
    {
        public Audio AudioData { get; set; }
        public int Relevancy { get; set; }
        public int CopyNumber { get; set; }
        public List<CopyIndex> CopyInfo { get; set; } // {userIndex, fileName}

        public MusicCopy(Audio audioData)
        {
            this.AudioData = audioData;
            this.Relevancy = 0;
            this.CopyNumber = 0;
            this.CopyInfo = new List<CopyIndex>();
        }

        public MusicCopy(Audio audioData, int relevancy)
        {
            this.AudioData = audioData;
            this.Relevancy = relevancy;
            this.CopyNumber = 0;
            this.CopyInfo = new List<CopyIndex>();
        }
    }
}
