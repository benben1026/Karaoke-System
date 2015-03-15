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

        public CopyIndex(int userIndex, string fileName)
        {
            UserIndex = userIndex;
            FileName = fileName;
        }
    }

    class MusicCopy
    {
        public string Filename { get; set; }
        public string Title { get; set; }
        public string Singer { get; set; }
        public string Album { get; set; }
        public string Hashvalue { get; set; }
        public int Size { get; set; }
        public int Relevancy { get; set; }
        public int CopyNumber { get; set; }
        public List<CopyIndex> CopyInfo { get; set; } // {userIndex, fileName}

        public MusicCopy(string filename, string title, string singer, string album, string hashvalue, int size)
        {
            this.Filename = filename;
            this.Title = title;
            this.Singer = singer;
            this.Album = album;
            this.Hashvalue = hashvalue;
            this.Size = size;
            this.Relevancy = 0;
            this.CopyNumber = 0;
            this.CopyInfo = new List<CopyIndex>();
        }

        public MusicCopy(string filename, string title, string singer, string album, string hashvalue, int size, int relevancy)
        {
            this.Filename = filename;
            this.Title = title;
            this.Singer = singer;
            this.Album = album;
            this.Hashvalue = hashvalue;
            this.Size = size;
            this.Relevancy = relevancy;
            this.CopyNumber = 0;
            this.CopyInfo = new List<CopyIndex>();
        }
    }
}
