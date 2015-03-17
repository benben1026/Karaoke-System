using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2P_Karaoke_System
{
    [Serializable]
    public class CopyIndex
    {
        public int UserIndex { get; set; }
        public string FileName { get; set; }
        public string IPAddress { get; set; }

        public CopyIndex(int userIndex, string fileName, string ipAddress)
        {
            UserIndex = userIndex;
            FileName = fileName;
            IPAddress = ipAddress;
        }
    }

    [Serializable]
    public class AudioInfo
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string MediaPath { get; set; }
        public int Size { get; set; }
        public string HashValue { get; set; }
    }
    [Serializable]
    public class MusicCopy
    {
        public AudioInfo AudioData { get; set; }
        public int Relevancy { get; set; }
        public int CopyNumber { get; set; }
        public List<CopyIndex> CopyInfo { get; set; } // {userIndex, fileName}

        public MusicCopy(Audio audioData)
        {
            this.AudioData = new AudioInfo();
            this.AudioData.Title = audioData.Title;
            this.AudioData.Artist = audioData.Artist;
            this.AudioData.Album = audioData.Album;
            this.AudioData.MediaPath = audioData.MediaPath;
            this.AudioData.Size = (int)audioData.Size;
            this.AudioData.HashValue = audioData.HashValue;
            this.Relevancy = 0;
            this.CopyNumber = 0;
            this.CopyInfo = new List<CopyIndex>();
        }

        public MusicCopy(Audio audioData, int relevancy)
        {
            this.AudioData = new AudioInfo();
            this.AudioData.Title = audioData.Title;
            this.AudioData.Artist = audioData.Artist;
            this.AudioData.Album = audioData.Album;
            this.AudioData.MediaPath = audioData.MediaPath;
            this.AudioData.Size = (int)audioData.Size;
            this.AudioData.HashValue = audioData.HashValue;
            this.Relevancy = relevancy;
            this.CopyNumber = 0;
            this.CopyInfo = new List<CopyIndex>();
        }

        public override string ToString()
        {
            return this.AudioData.Title + "\n" + this.AudioData.Album + "\n" + this.AudioData.Artist + " [ " + this.CopyNumber + " ]";
        }
    }
}
