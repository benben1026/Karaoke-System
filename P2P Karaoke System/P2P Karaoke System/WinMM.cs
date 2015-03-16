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
    enum Messages : uint
    {
        MM_WIM_OPEN = 0x3BE,
        MM_WIM_CLOSE = 0x3BF,
        MM_WIM_DATA = 0x3C0,

        MM_WOM_OPEN = 0x3BB,
        MM_WOM_CLOSE = 0x3BC,
        MM_WOM_DONE = 0x3BD,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WaveHdr
    {
        public IntPtr lpData; // pointer to locked data buffer
        public int dwBufferLength; // length of data buffer
        public int dwBytesRecorded; // used for input only
        public IntPtr dwUser; // for client's use
        public int dwFlags; // assorted flags (see defines)
        public int dwLoops; // loop control counter
        public IntPtr lpNext; // PWaveHdr, reserved for driver
        public int reserved; // reserved for driver
    }

    internal class Native
    {
        public const int MMSYSERR_NOERROR = 0;
        public const int CALLBACK_FUNCTION = 0x00030000;  

        // callbacks
        public delegate void WaveDelegate(IntPtr hdrvr, Messages uMsg, int dwUser, ref WaveHdr wavhdr, int dwParam2);

        private const string mmdll = "winmm.dll";

        [DllImport(mmdll)]
        public static extern int waveOutGetNumDevs();
        [DllImport(mmdll)]
        public static extern int waveOutPrepareHeader(IntPtr hWaveOut, ref WaveHdr lpWaveOutHdr, int uSize);
        [DllImport(mmdll)]
        public static extern int waveOutUnprepareHeader(IntPtr hWaveOut, ref WaveHdr lpWaveOutHdr, int uSize);
        [DllImport(mmdll)]
        public static extern int waveOutWrite(IntPtr hWaveOut, ref WaveHdr lpWaveOutHdr, int uSize);
        [DllImport(mmdll)]
        public static extern int waveOutOpen(out IntPtr hWaveOut, int uDeviceID, ref WavFormat lpFormat, WaveDelegate dwCallback, int dwInstance, int dwFlags);
        [DllImport(mmdll)]
        public static extern int waveOutReset(IntPtr hWaveOut);
        [DllImport(mmdll)]
        public static extern int waveOutClose(IntPtr hWaveOut);
        [DllImport(mmdll)]
        public static extern int waveOutSetVolume(IntPtr hWaveOut, int dwVolume);
        [DllImport(mmdll)]
        public static extern int waveOutGetVolume(IntPtr hWaveOut, out int dwVolume);
        [DllImport(mmdll)]
        public static extern int waveOutSetPlaybackRate(IntPtr hwo, int dwRate);
    }
}
