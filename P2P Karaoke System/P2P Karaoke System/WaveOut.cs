using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace P2P_Karaoke_System
{
    class WaveOutHelper
    {
        public static void Try(int err)
        {
            if (err != 0)
                throw new Exception(err.ToString());
        }
    }

    public delegate void BufferFillEventHandler(IntPtr data, int size);

    internal class WaveOutBuffer : IDisposable
    {
        public WaveOutBuffer NextBuffer;

        private AutoResetEvent playEvent = new AutoResetEvent(false);
        private IntPtr waveOut;

        private WaveHdr header;
        private byte[] headerData;
        private GCHandle headerHandle;
        private GCHandle headerDataHandle;

        private bool playing;
        internal static void WaveOutProc(IntPtr hdrvr, Messages uMsg, int dwUser, ref WaveHdr wavhdr, int dwParam2)
        {
            if (uMsg == Messages.MM_WOM_DONE)
            {
                try
                {
                    GCHandle h = (GCHandle)wavhdr.dwUser;
                    WaveOutBuffer buf = (WaveOutBuffer)h.Target;
                    buf.OnCompleted();
                }
                catch
                {
                }
            }
        }

        public WaveOutBuffer(IntPtr waveOutHandle, int size)
        {
            waveOut = waveOutHandle;

            headerHandle = GCHandle.Alloc(header, GCHandleType.Pinned);
            header.dwUser = (IntPtr)GCHandle.Alloc(this);
            headerData = new byte[size];
            headerDataHandle = GCHandle.Alloc(headerData, GCHandleType.Pinned);
            header.lpData = headerDataHandle.AddrOfPinnedObject();
            header.dwBufferLength = size;
            WaveOutHelper.Try(Native.waveOutPrepareHeader(waveOut, ref header, Marshal.SizeOf(header)));
        }
        ~WaveOutBuffer()
        {
            Dispose();
        }
        public void Dispose()
        {
            if (header.lpData != IntPtr.Zero)
            {
                Native.waveOutUnprepareHeader(waveOut, ref header, Marshal.SizeOf(header));
                headerHandle.Free();
                header.lpData = IntPtr.Zero;
            }
            playEvent.Close();
            if (headerDataHandle.IsAllocated)
                headerDataHandle.Free();
            GC.SuppressFinalize(this);
        }

        public int Size
        {
            get { return header.dwBufferLength; }
        }

        public IntPtr Data
        {
            get { return header.lpData; }
        }

        public bool Play()
        {
            lock (this)
            {
                playEvent.Reset();
                playing = Native.waveOutWrite(waveOut, ref header, Marshal.SizeOf(header)) == Native.MMSYSERR_NOERROR;
                return playing;
            }
        }
        public void WaitFor()
        {
            if (playing)
            {
                playing = playEvent.WaitOne();
            }
            else
            {
                Thread.Sleep(0);
            }
        }
        public void OnCompleted()
        {
            playEvent.Set();
            playing = false;
        }
    }

    public class WaveOutPlayer : IDisposable
    {
        private IntPtr waveOut;
        private WaveOutBuffer buffers; // linked list
        private WaveOutBuffer currentBuffer;
        private Thread thread;
        private BufferFillEventHandler fillProc;
        private bool finished;
        private byte zero;

        private Native.WaveDelegate bufferProc = new Native.WaveDelegate(WaveOutBuffer.WaveOutProc);

        public static int DeviceCount
        {
            get { return Native.waveOutGetNumDevs(); }
        }

        public WaveOutPlayer(int device, WavFormat format, int bufferSize, int bufferCount, BufferFillEventHandler fillProc)
        {
            zero = format.wBitsPerSample == 8 ? (byte)128 : (byte)0;
            this.fillProc = fillProc;
            WaveOutHelper.Try(Native.waveOutOpen(out waveOut, device, ref format, bufferProc, 0, Native.CALLBACK_FUNCTION));
            AllocateBuffers(bufferSize, bufferCount);
            thread = new Thread(new ThreadStart(ThreadProc));
            thread.Start();

            //Native.waveOutSetPlaybackRate(waveOut, 0x00020000);
            //Native.waveOutSetPitch(waveOut, 0x00002000);
        }
        ~WaveOutPlayer()
        {
            Dispose();
        }
        public void Dispose()
        {
            if (thread != null)
                try
                {
                    finished = true;
                    if (waveOut != IntPtr.Zero)
                        Native.waveOutReset(waveOut);
                    thread.Join();
                    fillProc = null;
                    FreeBuffers();
                    if (waveOut != IntPtr.Zero)
                        Native.waveOutClose(waveOut);
                }
                finally
                {
                    thread = null;
                    waveOut = IntPtr.Zero;
                }
            GC.SuppressFinalize(this);
        }
        private void ThreadProc()
        {
            while (!finished)
            {
                Advance();
                if (fillProc != null && !finished)
                    fillProc(currentBuffer.Data, currentBuffer.Size);
                else
                {
                    // zero out buffer
                    byte v = zero;
                    byte[] b = new byte[currentBuffer.Size];
                    for (int i = 0; i < b.Length; i++)
                        b[i] = v;
                    Marshal.Copy(b, 0, currentBuffer.Data, b.Length);

                }
                currentBuffer.Play();
            }
            WaitForAllBuffers();
        }
        private void AllocateBuffers(int bufferSize, int bufferCount)
        {
            FreeBuffers();
            if (bufferCount > 0)
            {
                buffers = new WaveOutBuffer(waveOut, bufferSize);
                WaveOutBuffer previousBuffer = buffers;
                try
                {
                    for (int i = 1; i < bufferCount; i++)
                    {
                        WaveOutBuffer Buf = new WaveOutBuffer(waveOut, bufferSize);
                        previousBuffer.NextBuffer = Buf;
                        previousBuffer = Buf;
                    }
                }
                finally
                {
                    previousBuffer.NextBuffer = buffers;
                }
            }
        }
        private void FreeBuffers()
        {
            currentBuffer = null;
            if (buffers != null)
            {
                WaveOutBuffer firstBuffer = buffers;
                buffers = null;

                WaveOutBuffer currentOne = firstBuffer;
                do
                {
                    WaveOutBuffer nextBuffer = currentOne.NextBuffer;
                    currentOne.Dispose();
                    currentOne = nextBuffer;
                } while (currentOne != firstBuffer);
            }
        }
        private void WaitForAllBuffers()
        {
            WaveOutBuffer buffer = buffers;
            while (buffer.NextBuffer != buffers)
            {
                buffer.WaitFor();
                buffer = buffer.NextBuffer;
            }
        }
        private void Advance()
        {
            currentBuffer = currentBuffer == null ? buffers : currentBuffer.NextBuffer;
            currentBuffer.WaitFor();
        }

        public void changeVolume(double leftVolume, double rightVolume)
        {
            int leftValue;
            if (leftVolume > 100) leftValue = 100;
            else if (leftVolume < 0) leftValue = 0;
            else leftValue = (int)(leftVolume / 100 * 0xFFFF);

            int rightValue;
            if (rightVolume > 100) rightValue = 100;
            else if (rightVolume < 0) rightValue = 0;
            else rightValue = (int)(rightVolume / 100 * 0xFFFF);

            //low-order word is left-channel volume, high-order word is right-channel volume
            int volumeValue = leftValue + (rightValue << 16);
            Native.waveOutSetVolume(waveOut, volumeValue);
        }
    }
}
