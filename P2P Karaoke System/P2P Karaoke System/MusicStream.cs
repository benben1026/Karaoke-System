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

        public MusicStream(int size)
        {
            pos = 0;
            fileData = new byte[size];
            flag = new bool[size];
            are = new AutoResetEvent(false);
            streamLock = new object();
        }

        public void WriteSegment(byte[] buffer, int offset, int count, int startPos)
        {
            Array.Copy(buffer, offset, fileData, startPos, count);
            for (int i = offset; i < offset + count; i++)
            {
                this.flag[i] = true;
            }
            are.Set();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = 0;
            lock (streamLock)
            {
                if (pos >= fileData.Length) return -1;
                for (int i = 0; i < count; i++, pos++, read++)
                {
                    if (pos >= fileData.Length) break;
                    if (!flag[pos])
                    {
                        are.Reset();
                        are.WaitOne(200);
                        if (!flag[pos]) break;
                    }
                    buffer[offset + i] = fileData[pos];
                }
            }
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
