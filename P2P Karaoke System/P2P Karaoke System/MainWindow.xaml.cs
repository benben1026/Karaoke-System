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
        private Microsoft.Win32.OpenFileDialog openDialog, addFileDialog;
        private WavFormat format;
        private Stream audioStream;
        private WaveOutPlayer thePlayer;
        private DispatcherTimer timer;
        private string audioFormat = null;

        public List<MusicCopy> musicDataList;

        public MainWindow()
        {
            InitializeComponent();

            this.openDialog = new Microsoft.Win32.OpenFileDialog();
            this.openDialog.Filter = "Audio File (*.wav, *.mp3, *.mp4, *.wma, *.m4a)|*.wav;*.mp3;*.mp4;*.wma;*.m4a;";
            this.openDialog.DefaultExt = "wav";

            this.addFileDialog = new Microsoft.Win32.OpenFileDialog();
            this.addFileDialog.Filter = "Audio File (*.wav, *.mp3, *.mp4, *.wma, *.m4a)|*.wav;*.mp3;*.mp4;*.wma;*.m4a;";
            this.addFileDialog.DefaultExt = "wav";

            timer = new DispatcherTimer();
            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = new TimeSpan(0, 0, 1);

            musicDataList = new List<MusicCopy>();

            musicDB = new MusicDataContext(Properties.Settings.Default.MusicConnectString);
            if (musicDB == null)
            {
                MessageBox.Show("Can't connect to the media database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                var musicQuery = from audio in musicDB.Audios orderby audio.Order select audio;
                var audios = musicQuery.ToArray<Audio>();
                for (int i = 0; i < audios.Length; i++)
                {
                    musicList.Items.Add(audios[i]);
                }
            }
            catch
            {
                MessageBox.Show("Can't connect to the media database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                    {
                        audioStream.Position = 0; // loop if the file ends
                        Stop_Click();
                    }
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

        private void Play_Click(object sender = null, RoutedEventArgs e = null)
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
                if (audioStream != null)
                {
                    thePlayer = new WaveOutPlayer(-1, format, 16384, 3, new BufferFillEventHandler(Filler));
                    thePlayer.Volume = volumeSlider.Value;
                    isPlaying = true;
                }
            }
        }

        private void Stop_Click(object sender = null, RoutedEventArgs e = null)
        {
            isPlaying = false;
            if (audioStream != null) audioStream.Position = 0;

            if (thePlayer != null)
            {
                try { thePlayer.Dispose(); }
                finally { thePlayer = null; }
            }  
        }

        void timer_Tick(object sender, EventArgs e)
        {
            progressSlider.Value = currentPosition();
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
            if (audioStream != null)
            {
                audioStream.Dispose();
                audioStream = null;
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
                openFile(openDialog.FileName);
            }
            Audio audio = new Audio();
            audio.MediaPath = "TestPath";
            musicDB.Audios.InsertOnSubmit(audio);
        }

        public void openFile(string fileName)
        {
            CloseFile();
            DisposeWave();
            audioFormat = System.IO.Path.GetExtension(fileName);
            Console.WriteLine(audioFormat);
            progressSlider.Value = 0;

            if (audioFormat.Equals(".wav"))
            {
                try
                {
                    WavStream S = new WavStream(fileName);
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
                NAudio.Wave.WaveStream pcm = new NAudio.Wave.AudioFileReader(fileName);
                format.wFormatTag = 3;
                format.nChannels = (short)pcm.WaveFormat.Channels;
                format.nSamplesPerSec = (int)pcm.WaveFormat.SampleRate;
                format.nAvgBytesPerSec = (int)pcm.WaveFormat.AverageBytesPerSecond;
                format.nBlockAlign = (short)pcm.WaveFormat.BlockAlign;
                format.wBitsPerSample = (short)pcm.WaveFormat.BitsPerSample;
                format.cbSize = (short)pcm.WaveFormat.ExtraSize;
                audioStream = new NAudio.Wave.BlockAlignReductionStream(pcm);
            }

            timer.Start();

            progressSlider.Maximum = currentDuration();
            Console.WriteLine("Duration: " + currentDuration() + "s");
        }

        public int currentDuration()
        {
            if (audioFormat == null) 
                return 0;
            else 
                return (int)(audioStream.Length / format.nAvgBytesPerSec);
        }

        public int currentPosition()
        {
            if (audioFormat == null)
                return 0;
            else
                return (int)(audioStream.Position / format.nAvgBytesPerSec);
        }

        private void progressSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            timer.Stop();
        }

        private void progressSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (audioStream != null)
            {
                Console.WriteLine((int)((Slider)sender).Value);
                audioStream.Position = (int)((Slider)sender).Value * format.nAvgBytesPerSec;
            }
            else
            {
                audioStream.Position = 0;
            }
            timer.Start();
        }
        
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (thePlayer == null) return;

            thePlayer.Volume = (int)((Slider)sender).Value;
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
            //Thread test = new Thread(() => Receiver.StartListening());
            //test.Start();
             
             
            //Get data from other peers
            //Sender.InitialIpList();
            //MusicCopy cp = new MusicCopy("travel1.wma", "travel1", "Jin", "Hello", "264204303863cf9089de5c42d34d64bd", 2009081, 1);
            //CopyIndex t1 = new CopyIndex(0, "travel1.wma");
            //CopyIndex t2 = new CopyIndex(1, "travel1.wma");
            //List<CopyIndex> a = new List<CopyIndex>();
            //a.Add(t1);
            //a.Add(t2);
            //cp.CopyInfo = a;
            //Thread test = new Thread(() => Sender.StartGetMusic(cp));
            //test.Start();
            //Thread.Sleep(1);
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (musicList.SelectedIndex < 0) return;
            Audio audio = (Audio)musicList.SelectedItem;
            EditInfoWindow m = new EditInfoWindow();
            m.title = audio.Title;
            m.singer = audio.Artist;
            m.album = audio.Album;
            m.lrcPath = audio.LyricsPath;
            if (audio.ImagePath != null)
            {
                if (audio.ImagePath.Length > 0) m.coverPath = audio.ImagePath;
                else m.coverPath = null;
            }
            else m.coverPath = null;
            m.Owner = this;
            if (m.ShowDialog() == true)
            {
                audio.Title = audio.Artist = audio.Album = audio.LyricsPath = audio.ImagePath = null;

                if (!string.IsNullOrWhiteSpace(m.title)) audio.Title = m.title;
                if (!string.IsNullOrWhiteSpace(m.singer)) audio.Artist = m.singer;
                if (!string.IsNullOrWhiteSpace(m.album)) audio.Album = m.album;
                if (!string.IsNullOrWhiteSpace(m.lrcPath)) audio.LyricsPath = m.lrcPath;
                if (!string.IsNullOrWhiteSpace(m.coverPath) || m.coverPath == "") audio.ImagePath = m.coverPath;
                
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
            }
        }

        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            if (addFileDialog.ShowDialog() == true)
            {
                
                Audio audio = new Audio();
                TagLib.Tag tag = TagLib.File.Create(addFileDialog.FileName).Tag;
                audio.Album = tag.Album == null ? "Unknown Album" : tag.Album;
                audio.Title = tag.Title == null ? "Unknown Title" : tag.Title;
                audio.MediaPath = addFileDialog.FileName;

                Console.WriteLine(audio.MediaPath);

                if (tag.JoinedPerformers.Length > 0) audio.Artist = tag.JoinedPerformers;
                else audio.Artist = "Unknown Artist";
      
                if(musicList.Items.Count==0) 
                    audio.Order=0;
                if (musicList.Items.Count > 0)
                {
                    audio.Order = ((Audio)(musicList.Items[musicList.Items.Count - 1])).Order+1;
                }

                FileInfo f = new FileInfo(addFileDialog.FileName);
                audio.Size = (int?)f.Length;

                if (musicDB != null)
                {
                    try
                    {
                        musicDB.Audios.InsertOnSubmit(audio);
                        musicDB.SubmitChanges();
                    }
                    catch
                    {
                        MessageBox.Show("Can't connect to the media database.");
                    }
                }
                musicList.Items.Add(audio);
                MusicCopy musicData = new MusicCopy(audio.MediaPath, audio.Title, audio.Artist, audio.Album, audio.HashValue, (int)audio.Size);
                musicDataList.Add(musicData);
            }
        }

        private void musicListItem_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            CloseFile();
            Audio audio = (Audio)((ListBoxItem)e.Source).Content;
            openFile(audio.MediaPath);
            Play_Click();
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
