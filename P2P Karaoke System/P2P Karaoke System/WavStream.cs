using System;
using System.IO;
using System.Text;

namespace P2P_Karaoke_System
{
    class WavStream : Stream, IDisposable
    {
        private Stream stream;
        private long length;
        private long dataPosition;
        private WavFormat format;
        private object lockObject;

        public WavFormat Format
        {
            get
            {
                return format;
            }
        }

        private string ReadChunk(BinaryReader reader)
        {
            byte[] ch = new byte[4];
            reader.Read(ch, 0, ch.Length);
            return Encoding.ASCII.GetString(ch);
        }

        public void ReadHeader()
        {
            BinaryReader Reader = new BinaryReader(stream);
            if (ReadChunk(Reader) != "RIFF")
                throw new InvalidDataException("Invalid file format");

            Reader.ReadInt32(); // Get rid of the first 8 bytes of "RIFF" info

            if (ReadChunk(Reader) != "WAVE")
                throw new InvalidDataException("Invalid file format");

            if (ReadChunk(Reader) != "fmt ")
                throw new InvalidDataException("Invalid file format");

            int len = Reader.ReadInt32();
            if (len < 16) // bad format chunk length
                throw new InvalidDataException("Invalid file format");

            format = new WavFormat(22050, 16, 2); // initialize to any format
            format.wFormatTag = Reader.ReadInt16();
            format.nChannels = Reader.ReadInt16();
            format.nSamplesPerSec = Reader.ReadInt32();
            format.nAvgBytesPerSec = Reader.ReadInt32();
            format.nBlockAlign = Reader.ReadInt16();
            format.wBitsPerSample = Reader.ReadInt16();

            // advance in the stream to skip the wave format block
            for (len -= 16; len > 0; --len)
            {
                Reader.ReadByte();
            }

            // assume the data chunk is aligned
            while (stream.Position < stream.Length && ReadChunk(Reader) != "data");

            if (stream.Position >= stream.Length)
                throw new InvalidDataException("Invalid file format");

            length = Reader.ReadInt32();
            dataPosition = stream.Position;

            Position = 0;
        }

        public WavStream(string fileName)
        {
            lockObject = new object();
            stream = new FileStream(fileName, FileMode.Open);
            ReadHeader();
        }

        public WavStream(Stream inputStream)
        {
            lockObject = new object();
            stream = inputStream;
            ReadHeader();
        }

        ~WavStream()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && stream != null)
            {
                stream.Close();
                stream = null;
            }
            base.Dispose(disposing);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read;
            lock (lockObject) {
                int lengthToRead = (int)Math.Min(count, length - Position);
                read = stream.Read(buffer, offset, lengthToRead);
            }
            return read;
        }

        public override void SetLength(long value)
        {
            throw new InvalidOperationException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            lock (lockObject)
            {
                switch (origin)
                {
                    case SeekOrigin.Begin:
                        stream.Position = offset + dataPosition;
                        break;
                    case SeekOrigin.Current:
                        stream.Seek(offset, SeekOrigin.Current);
                        break;
                    case SeekOrigin.End:
                        stream.Position = dataPosition + length - offset;
                        break;
                }
                return Position;
            }
        }

        public override void Flush() { }

        public override long Position
        {
            get { return stream.Position - dataPosition; }
            set { Seek(value, SeekOrigin.Begin); }
        }

        public override long Length
        {
            get { return length; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanRead
        {
            get { return true; }
        }
    }
}
