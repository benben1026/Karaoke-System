using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2P_Karaoke_System
{
    class MusicInfo
    {
        private string title, artist, album;
        private int order,year;
        private string imagePath,mediaPath,lyricsPath;

        public string Title
        {
            get { return title; }
            set 
            {
                title = value;
            }
        }
        public string Artist
        {
            get{return artist;}
            set
            {
                artist=value;
            }
        }
        public string Album
        {
            get{return album;}
            set
            {
                album=value;
            }
        }
        public int Year
        {
            get { return year; }   
            set
            {
                year = value;
            }
        }
        public int Order
        {
            get { return order; }
            set
            {
                order = value;
            }
        }
        public string ImagePath
        {
            get { return imagePath; }
            set
            {
                imagePath = value;
            }
        }
        public string LyricsPath
        {
            get { return lyricsPath; }
            set
            {
                lyricsPath = value;
            }
        }
        public string MediaPath
        {
            get { return mediaPath; }
            set
            {
                mediaPath = value;
            }
        }
        public override string ToString()
        {
            return Title;
        }
    }
}
