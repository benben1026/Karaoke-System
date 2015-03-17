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
        public WavFormat Format { get; set; }

        public AudioInfo(Audio audioData)
        {
            this.Title = audioData.Title;
            this.Artist = audioData.Artist;
            this.Album = audioData.Album;
            this.MediaPath = audioData.MediaPath;
            this.HashValue = audioData.HashValue;

            NAudio.Wave.AudioFileReader audioStream = new NAudio.Wave.AudioFileReader(audioData.MediaPath);
            this.Size = (int)audioStream.Length;
            WavFormat format;
            format.wFormatTag = 3;
            format.nChannels = (short)audioStream.WaveFormat.Channels;
            format.nSamplesPerSec = (int)audioStream.WaveFormat.SampleRate;
            format.nAvgBytesPerSec = (int)audioStream.WaveFormat.AverageBytesPerSecond;
            format.nBlockAlign = (short)audioStream.WaveFormat.BlockAlign;
            format.wBitsPerSample = (short)audioStream.WaveFormat.BitsPerSample;
            format.cbSize = (short)audioStream.WaveFormat.ExtraSize;
            this.Format = format;
            audioStream.Close();
        }

        public AudioInfo(Audio audioData, int status)  
        {
            this.Title = audioData.Title;
            this.Artist = audioData.Artist;
            this.Album = audioData.Album;
            this.MediaPath = audioData.MediaPath;
            this.HashValue = audioData.HashValue;
        }

        public Audio ToAudio()
        {
            Audio audio = new Audio();
            audio.Title = this.Title;
            audio.Artist = this.Artist;
            audio.Album = this.Album;
            audio.MediaPath = this.MediaPath;
            audio.Size = this.Size;
            audio.HashValue = this.HashValue;
            return audio;
        }
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
            this.AudioData = new AudioInfo(audioData);
            this.Relevancy = 0;
            this.CopyNumber = 0;
            this.CopyInfo = new List<CopyIndex>();
        }

        public MusicCopy(Audio audioData, int relevancy)
        {
            if (relevancy == -2)
            {
                this.AudioData = new AudioInfo(audioData, 1);
                return;
            }
            this.AudioData = new AudioInfo(audioData);
            this.Relevancy = relevancy;
            this.CopyNumber = 0;
            this.CopyInfo = new List<CopyIndex>();
        }

        public override string ToString()
        {
            return this.AudioData.Title + "\n" + this.AudioData.Album + "\n" + this.AudioData.Artist + "\n# of peers: [ " + (this.CopyNumber+1) + " ]";
        }
    }
}
