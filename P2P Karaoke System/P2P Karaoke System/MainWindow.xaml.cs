using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace P2P_Karaoke_System
{
    /// <summary>
    /// MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool playing = false;
        private Microsoft.Win32.OpenFileDialog openDialog;
        private WavFormat format;
        private Stream audioStream;
        private WaveOutPlayer thePlayer;

        public MainWindow()
        {
            InitializeComponent();

            this.openDialog = new Microsoft.Win32.OpenFileDialog();
            this.openDialog.DefaultExt = "wav";
            this.openDialog.Filter = "WAV files|*.wav";
        }

        private void Filler(IntPtr data, int size)
        {
            byte[] b = new byte[size];
            if (audioStream != null)
            {
                int pos = 0;
                while (pos < size)
                {
                    int toget = size - pos;
                    int got = audioStream.Read(b, pos, toget);
                    if (got < toget)
                        audioStream.Position = 0; // loop if the file ends
                    pos += got;
                }
            }
            else
            {
                for (int i = 0; i < b.Length; i++)
                    b[i] = 0;
            }
            System.Runtime.InteropServices.Marshal.Copy(b, 0, data, size);

        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            Stop_Click();
            if (audioStream != null)
            {
                audioStream.Position = 0;
                thePlayer = new WaveOutPlayer(-1, format, 16384, 3, new BufferFillEventHandler(Filler));
            }
        }

        private void Stop_Click(object sender = null, RoutedEventArgs e = null)
        {
            if (thePlayer != null)
            {
                try
                {
                    thePlayer.Dispose();
                }
                finally
                {
                    thePlayer = null;
                }
            }
        }

        void timer_Tick(object sender, EventArgs e)
        {
        }

        public void CloseFile()
        {
            Stop_Click();
            if (audioStream != null)
            {
                try
                {
                    audioStream.Close();
                }
                finally
                {
                    audioStream = null;
                }
            }
        }

        private void load_Click(object sender, RoutedEventArgs e)
        {
            if (openDialog.ShowDialog() == true)
            {
                CloseFile();
                try
                {
                    WavStream S = new WavStream(openDialog.FileName);
                    if (S.Length <= 0)
                        throw new Exception("Invalid WAV file");
                    format = S.Format;
                    if (format.wFormatTag != (short)WavFormats.PCM && format.wFormatTag != (short)WavFormats.FLOAT)
                        throw new Exception("Olny PCM files are supported");

                    audioStream = S;
                }
                catch (Exception err)
                {
                    CloseFile();
                    System.Windows.Forms.MessageBox.Show(err.Message);
                }
            }
        }
    }
}
