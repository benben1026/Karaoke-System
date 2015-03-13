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

        public delegate void BufferFillEventHandler(IntPtr data, int size);

        internal class WaveOutBuffer : IDisposable
        {
            public WaveOutBuffer NextBuffer;

            private AutoResetEvent playEvent = new AutoResetEvent(false);
            private IntPtr waveOut;

            private WaveHdr m_Header;
            private byte[] m_HeaderData;
            private GCHandle m_HeaderHandle;
            private GCHandle m_HeaderDataHandle;

            private bool m_Playing;
        }
    }
}
