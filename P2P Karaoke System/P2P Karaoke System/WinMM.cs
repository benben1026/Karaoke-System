using System;
using System.Runtime.InteropServices;

namespace P2P_Karaoke_System
{
    [StructLayout(LayoutKind.Sequential)]
    public struct WavFormat
    {
        public short wFormatTag;
        public short nChannels;
        public int nSamplesPerSec;
        public int nAvgBytesPerSec;
        public short nBlockAlign;
        public short wBitsPerSample;
        public short cbSize;

        public WavFormat(int sampleRate, int bits, int channels)
        {
            wFormatTag = (short)WavFormats.PCM;
            nChannels = (short)channels;
            nSamplesPerSec = sampleRate;
            wBitsPerSample = (short)bits;
            cbSize = 0;

            nBlockAlign = (short)(channels * (bits / 8));
            nAvgBytesPerSec = nSamplesPerSec * nBlockAlign;
        }
    }

    public enum WavFormats
    {
        PCM = 1,
        FLOAT = 3,
    }

    class WinMM
    {
    }
}
