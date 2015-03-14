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
                WaveOutBuffer Prev = buffers;
                try
                {
                    for (int i = 1; i < bufferCount; i++)
                    {
                        WaveOutBuffer Buf = new WaveOutBuffer(waveOut, bufferSize);
                        Prev.NextBuffer = Buf;
                        Prev = Buf;
                    }
                }
                finally
                {
                    Prev.NextBuffer = buffers;
                }
            }
        }
        private void FreeBuffers()
        {
            currentBuffer = null;
            if (buffers != null)
            {
                WaveOutBuffer First = buffers;
                buffers = null;

                WaveOutBuffer Current = First;
                do
                {
                    WaveOutBuffer Next = Current.NextBuffer;
                    Current.Dispose();
                    Current = Next;
                } while (Current != First);
            }
        }
        private void Advance()
        {
            currentBuffer = currentBuffer == null ? buffers : currentBuffer.NextBuffer;
            currentBuffer.WaitFor();
        }
        private void WaitForAllBuffers()
        {
            WaveOutBuffer Buf = buffers;
            while (Buf.NextBuffer != buffers)
            {
                Buf.WaitFor();
                Buf = Buf.NextBuffer;
            }
        }
    }
}
