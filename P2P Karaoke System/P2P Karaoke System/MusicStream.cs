using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace P2P_Karaoke_System
{
    class MusicStream : Stream
    {
        private byte[] fileData;
        private bool[] flag;
        private long pos;
        private AutoResetEvent are;
        private object streamLock;
        private Mutex mutex;

        public MusicStream(int size)
        {
            pos = 0;
            fileData = new byte[size];
            flag = new bool[size];
            are = new AutoResetEvent(false);
            streamLock = new object();
            mutex = new Mutex();
        }

        public void WriteSegment(byte[] buffer, int offset, int count, int startPos)
        {
            Console.WriteLine("Write to stream from {0} to {1}", startPos, startPos + count - 1);
            mutex.WaitOne();
            Array.Copy(buffer, offset, fileData, startPos, count);
            for (int i = startPos; i < startPos + count; i++)
            {
                this.flag[i] = true;
            }
            mutex.ReleaseMutex();
            are.Set();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = 0;
            mutex.WaitOne();
            lock (streamLock)
            {
                for (int i = 0; i < count; i++, pos++, read++)
                {
                    if (pos >= fileData.Length) break;
                    while (!flag[pos])
                    {
                        are.Reset();
                        mutex.ReleaseMutex();
                        are.WaitOne(200);
                        mutex.WaitOne();
                        if (read > 0) break;
                    }
                    if (!flag[pos])
                    {
                        mutex.ReleaseMutex();
                        break;
                    }
                    buffer[offset + i] = fileData[pos];
                }
            }
            mutex.ReleaseMutex();
            return read;
        }

        ~MusicStream()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && are != null)
            {
                are.Dispose();
                are = null;
            }
            fileData = null;
            flag = null;
            base.Dispose(disposing);
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return fileData.LongLength; }
        }

        public override long Position
        {
            get { return pos; }
            set { Seek(value, SeekOrigin.Begin); }
        }

        public override void Flush() { }

        public override void SetLength(long value)
        {
            throw new InvalidOperationException();
        }

        public override void Close()
        {
            mutex.WaitOne();
            for (int i = 0; i < Length; i++)
            {
                this.flag[i] = true;
            }
            mutex.ReleaseMutex();
            are.Set();
            base.Close();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            lock (streamLock)
            {
                switch (origin)
                {
                    case SeekOrigin.Begin:
                        pos = offset;
                        break;
                    case SeekOrigin.Current:
                        pos += offset;
                        break;
                    case SeekOrigin.End:
                        pos = fileData.LongLength - offset;
                        break;
                }
            }
            return pos;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException();
        }
    }
}
