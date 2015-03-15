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
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using Microsoft.Win32;

namespace P2P_Karaoke_System
{
    /// <summary>
    /// MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool isPlaying = false;
        MusicDataContext musicDB;
        private Microsoft.Win32.OpenFileDialog openDialog;
        private WavFormat format;
        private Stream audioStream;
        private WaveOutPlayer thePlayer;
        private string audioFormat = null;
        // For file format other than WAV
        private NAudio.Wave.BlockAlignReductionStream nAudioStream = null;
        private OpenFileDialog openFileDialog, addFileDialog;

        public MainWindow()
        {
            InitializeComponent();

            this.openDialog = new Microsoft.Win32.OpenFileDialog();
            this.openDialog.Filter = "Audio File (*.wav, *.mp3, *.mp4, *.wma, *.m4a)|*.wav;*.mp3;*.mp4;*.wma;*.m4a;";
            this.openDialog.DefaultExt = "wav";

            musicDB = new MusicDataContext(Properties.Settings.Default.MusicConnectString);
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

        private void Filler2(IntPtr data, int size)
        {
            byte[] b = new byte[size];
            if (nAudioStream != null)
            {
                int pos = 0;
                while (pos < size)
                {
                    int toget = size - pos;
                    int got = nAudioStream.Read(b, pos, toget);
                    if (got < toget)
                        nAudioStream.Position = 0; // loop if the file ends
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
            if (audioFormat == null) return;
            if (isPlaying)
            {
                if (thePlayer != null)
                {
                    try { thePlayer.Dispose(); }
                    finally { thePlayer = null; }
                    isPlaying = false;
                }
            }
            else
            {
                if (audioFormat == ".wav")
                {
                    if (audioStream != null)
                    {
                        thePlayer = new WaveOutPlayer(-1, format, 16384, 3, new BufferFillEventHandler(Filler));
                        isPlaying = true;
                    }
                }
                else
                {
                    if (nAudioStream != null)
                    {
                        thePlayer = new WaveOutPlayer(-1, format, 16384, 3, new BufferFillEventHandler(Filler2));
                        isPlaying = true;
                    }
                }
            }

        }

        private void Stop_Click(object sender = null, RoutedEventArgs e = null)
        {
            if (thePlayer != null)
            {
                try { thePlayer.Dispose(); }
                finally { thePlayer = null; }

                isPlaying = false;
                if (audioStream != null) audioStream.Position = 0;
                if (nAudioStream != null) nAudioStream.Position = 0; 

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

        private void DisposeWave()
        {
            if (nAudioStream != null)
            {
                nAudioStream.Dispose();
                nAudioStream = null;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            CloseFile();
            base.OnClosed(e);
        }

        private void load_Click(object sender, RoutedEventArgs e)
        {
            if (openDialog.ShowDialog() == true)
            {
                CloseFile();
                DisposeWave();
                audioFormat = System.IO.Path.GetExtension(openDialog.FileName);
                Console.WriteLine(audioFormat);

                if (audioFormat.Equals(".wav"))
                {
                    try
                    {
                        WavStream S = new WavStream(openDialog.FileName);
                        if (S.Length <= 0)
                            throw new Exception("Invalid WAV file");
                        format = S.Format;
                        Console.WriteLine(format);
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
                else
                {
                    NAudio.Wave.WaveStream pcm = new NAudio.Wave.AudioFileReader(openDialog.FileName);
                    format.wFormatTag = 3;
                    format.nChannels = (short)pcm.WaveFormat.Channels;
                    format.nSamplesPerSec = (int)pcm.WaveFormat.SampleRate;
                    format.nAvgBytesPerSec = (int)pcm.WaveFormat.AverageBytesPerSecond;
                    format.nBlockAlign = (short)pcm.WaveFormat.BlockAlign;
                    format.wBitsPerSample = (short)pcm.WaveFormat.BitsPerSample;
                    format.cbSize = (short)pcm.WaveFormat.ExtraSize;
                    nAudioStream = new NAudio.Wave.BlockAlignReductionStream(pcm);
                }
                DispatcherTimer timer = new DispatcherTimer();
                timer.Tick += new EventHandler(timer_Tick);
                timer.Interval = new TimeSpan(0, 0, 1);
            }
            Audio audio = new Audio();
            audio.MediaPath = "TestPath";
            musicDB.Audios.InsertOnSubmit(audio);
        }

        private void p2p_Click(object sender, RoutedEventArgs e)
        {
            /*
            //Test object serialization
            GetRequest gr = new GetRequest("1.mp3", "87ECA84BBFF77E54D21711A496857159CC5FA033", 0, 1024);
            Console.WriteLine("name = {0}, md5 = {1}, start = {2}, end = {3}", gr.GetFilename(), gr.GetMd5(), gr.GetStartByte(), gr.GetEndByte());
            byte[] output = gr.ToByte();
            GetRequest newGr = (GetRequest)GetRequest.toObject(output);
            Console.WriteLine("name = {0}, md5 = {1}, start = {2}, end = {3}", newGr.GetFilename(), newGr.GetMd5(), newGr.GetStartByte(), newGr.GetEndByte());
            */

            /*
            //Test get response
            FileStream fs = new FileStream("travel1.wma", FileMode.Open);
            GetResponse gres = new GetResponse("travel1.wma", "87ECA84BBFF77E54D21711A496857159CC5FA033");
            gres.GetData(fs, "87ECA84BBFF77E54D21711A496857159CC5FA033", 0, 1024);
            Console.WriteLine("status = {0}", gres.GetStatus());
            byte[] output = gres.ToByte();
            Console.WriteLine("size = {0}", output.Length);
            */

            
            //waiting for conncetion...
            Thread test = new Thread(() => Receiver.StartListening());
            test.Start();
            
            
            //Get data from other peers
            //Sender.InitialIpList();
            //MusicCopy cp = new MusicCopy("travel1.wma", "travel1", "Jin", "Hello", "87ECA84BBFF77E54D21711A496857159CC5FA033", 2009081, 1);
            //CopyIndex t = new CopyIndex(0, "travel1.wma");
            //List<CopyIndex> a = new List<CopyIndex>();
            //a.Add(t);
            //cp.CopyInfo = a;
            //Thread test = new Thread(() => Sender.StartGetMusic(cp));
            //test.Start();
            
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            //EditInfoWindow m = new EditInfoWindow();
            //m.Show();
        }

        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            if (addFileDialog.ShowDialog() == true)
            {
                Audio audio = new Audio();
                TagLib.Tag tag = TagLib.File.Create(addFileDialog.FileName).Tag;
                audio.Album = tag.Album;
                audio.Title = tag.Title;
                if (tag.JoinedPerformers.Length > 0) audio.Artist = tag.JoinedPerformers;
      
                if(musicList.Items.Count==0) 
                    audio.Order=0;
                if (musicList.Items.Count > 0)
                {
                    audio.Order = ((Audio)(musicList.Items[musicList.Items.Count - 1])).Order+1;
                }


                if (musicDB != null)
                {
                    try
                    {
                        musicDB.Audios.InsertOnSubmit(audio);
                        musicDB.SubmitChanges();
                    }
                    catch
                    {
                        MessageBox.Show("Can't connect to the media database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                musicList.Items.Add(audio);

            }


        }

        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (musicList.SelectedIndex < 0)
                return;
            if (musicDB != null)
            {
                try
                {
                    musicDB.Audios.DeleteOnSubmit((Audio)musicList.SelectedItem);
                    musicDB.SubmitChanges();
                }
                catch
                {
                    MessageBox.Show("Datebase Connection Failure","Error",MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
