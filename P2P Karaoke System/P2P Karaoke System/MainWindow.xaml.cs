using System;
using System.IO;
using System.Security.Cryptography;
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
        private LrcReader lyricsReader;
        private string audioFormat = null;
        private ImageSourceConverter imgSrcConverter;
        private ImageSource defaultImage;
        private bool speed2XOn = false;

        private string keyword;
        public string Keyword { get { return keyword; } set { SearchBox.Text = keyword = value; } }

        public static List<MusicCopy> musicDataList;
        public string[] ipListInput;
        public int InputIPNumber { get; set; }

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

            ipListInput = new string[10];
            this.InputIPNumber = 0;
            imgSrcConverter = new ImageSourceConverter();
            defaultImage = img.Source;

            musicList.Visibility = Visibility.Visible;
            searchList.Visibility = Visibility.Collapsed;

            musicDB = new MusicDataContext(Properties.Settings.Default.MusicConnectString);
            if (musicDB == null)
            {
                MessageBox.Show("Can't connect to the media database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                var musicQuery = from Audio audio in musicDB.Audios orderby audio.Order select audio;
                var audios = musicQuery.ToArray<Audio>();
                foreach (var audio in audios)
                {
                    musicList.Items.Add(audio);
                    MusicCopy musicData = new MusicCopy(audio);
                    musicDataList.Add(musicData);
                    Console.WriteLine(audio.Title);
                }
            }
            catch
            {
                MessageBox.Show("Can't connect to the media database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Filler(IntPtr data, int size)
        {
            int speedFactor = 1;
            if (speed2XOn) speedFactor = 2;

            byte[] b = new byte[speedFactor * size];
            if (audioStream != null)
            {
                int pos = 0;
                while (pos < speedFactor * size)
                {
                    int toget = speedFactor*size - pos;
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

            if (speed2XOn)
            {
                long bytePerSample = format.wBitsPerSample / 8;
                long sampleCount = size / bytePerSample;
                for (int i = 0; i < sampleCount; ++i)
                {
                    for (int j = 0; j < bytePerSample; j++)
                    {
                        b[i * bytePerSample + j] = b[i * bytePerSample * 2 + j];
                    }
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(b, 0, data, size);

        }

        private void Play_Click(object sender = null, RoutedEventArgs e = null)
        {
            if (audioStream == null) return;
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
                    AdjustVolume();
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
            //Lyrics
            for (int i = 1; i <= 7; i++)//7 is the number of label
            {
                Label lyricsLabel = (Label) this.FindName("Lyrics" + i);
                lyricsLabel.Content = lyricsReader.GetLyricsByTimeWithOffset(currentPosition() * 1000, i - 4); //the fourth label will get the current lyrics
            }
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

        protected override void OnClosed(EventArgs e)
        {

            CloseFile();
            base.OnClosed(e);
        }

        private void load_Click(object sender, RoutedEventArgs e)
        {
            addButton_Click();
            openFile((Audio)musicList.Items[musicList.Items.Count - 1]);
        }

        public void openFile(Audio audio)
        {
            CloseFile();
            bool fileExists = File.Exists(audio.MediaPath);
            if (!fileExists)
            {
                System.Windows.Forms.MessageBox.Show("File doesn't exist.");
                return;
            }
            audioFormat = System.IO.Path.GetExtension(audio.MediaPath);
            Console.WriteLine(audioFormat);
            progressSlider.Value = 0;

            if (audioFormat.Equals(".wav"))
            {
                try
                {
                    WavStream S = new WavStream(audio.MediaPath);
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
                
                try
                {
                    img.Source = (ImageSource)imgSrcConverter.ConvertFromString(audio.ImagePath);
                }
                catch
                {
                    img.Source = defaultImage;
                }
            }
            else
            {
                NAudio.Wave.WaveStream pcm = new NAudio.Wave.AudioFileReader(audio.MediaPath);
                format.wFormatTag = 3;
                format.nChannels = (short)pcm.WaveFormat.Channels;
                format.nSamplesPerSec = (int)pcm.WaveFormat.SampleRate;
                format.nAvgBytesPerSec = (int)pcm.WaveFormat.AverageBytesPerSecond;
                format.nBlockAlign = (short)pcm.WaveFormat.BlockAlign;
                format.wBitsPerSample = (short)pcm.WaveFormat.BitsPerSample;
                format.cbSize = (short)pcm.WaveFormat.ExtraSize;
                audioStream = new NAudio.Wave.BlockAlignReductionStream(pcm);

                try
                {
                    img.Source = (ImageSource)imgSrcConverter.ConvertFromString(audio.ImagePath);
                }
                catch
                {
                    TagLib.Tag tag = TagLib.File.Create(audio.MediaPath).Tag;
                    if (tag.Pictures.Length > 0)
                    {
                        using (MemoryStream albumArtworkMemStream = new MemoryStream(tag.Pictures[0].Data.Data))
                        {
                            BitmapImage albumImage = new BitmapImage();
                            albumImage.BeginInit();
                            albumImage.CacheOption = BitmapCacheOption.OnLoad;
                            albumImage.StreamSource = albumArtworkMemStream;
                            albumImage.EndInit();
                            img.Source = albumImage;
                        }
                    }
                    else
                    {
                        img.Source = defaultImage;
                    }
                }
            }
            
            //Lyrics
            try
            {
                String lyricsPath;
                if (audio.LyricsPath != null) {
                    lyricsPath = audio.LyricsPath;
                } else {
                    lyricsPath = System.IO.Path.GetDirectoryName(audio.MediaPath) + "\\" + System.IO.Path.GetFileNameWithoutExtension(audio.MediaPath) + ".lrc"; //consider same filename as music file
                }
                lyricsReader = new LrcReader(lyricsPath);
            }
            catch (Exception err)
            {
                System.Windows.Forms.MessageBox.Show(err.Message);
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
            if (audioStream == null)
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

        private void balanceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (((Slider)sender).Value >= 60 || ((Slider)sender).Value <= 40)
                balanceSlider.IsSnapToTickEnabled = false;
            else
                balanceSlider.IsSnapToTickEnabled = true;

            AdjustVolume();
        }

        private void LyricsEnableBtn_Click(object sender, RoutedEventArgs e)
        {
            if (LyricsPanel.Visibility == Visibility.Visible)
                LyricsPanel.Visibility = Visibility.Collapsed;
            else
                LyricsPanel.Visibility = Visibility.Visible;
        }

        private void speedButton_Click(object sender, RoutedEventArgs e)
        {
            if (speed2XOn) speed2XOn = false;
            else speed2XOn = true;
        }

        private void AdjustVolume(object sender = null, RoutedPropertyChangedEventArgs<double> e = null)
        {
            if (thePlayer == null) return;

            double ultimateVolume = volumeSlider.Value > 100 ? 100 : volumeSlider.Value;

            double righVolumeScale = balanceSlider.Value > 50 ? 50 : balanceSlider.Value;
            righVolumeScale = righVolumeScale / 50.0;
            double leftVolumeScale = balanceSlider.Value < 50 ? 0 : balanceSlider.Value - 50;
            leftVolumeScale = 1 - leftVolumeScale / 50.0;

            thePlayer.changeVolume(ultimateVolume * leftVolumeScale, ultimateVolume * righVolumeScale);
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
            //Peer p = new Peer();
            //Thread test = new Thread(() => p.StartListening());
            //test.Start();
             
             
            ////Get data from other peers
            //string[] ipList = new string[10];
            //ipList[0] = "192.168.215.116";
            //ipList[1] = "192.168.213.114";
            //ipList[2] = "";
            //ipList[3] = "";
            //ipList[4] = "";
            //ipList[5] = "";
            //ipList[6] = "";
            //ipList[7] = "";
            //ipList[8] = "";
            //ipList[9] = "";
            ////Sender.InitialIpList();
            //Audio a = new Audio();
            //a.MediaPath = "travel1.wma";
            //a.Title = "travel1";
            //a.Artist = "Jin";
            //a.Album = "Hello";
            //a.HashValue = "264204303863cf9089de5c42d34d64bd";
            //a.Size = 2009081;

            //MusicCopy cp = new MusicCopy(a);
            //CopyIndex t1 = new CopyIndex(0, "travel1.wma", "192.168.215.116");
            ////CopyIndex t2 = new CopyIndex(1, "aaa.wma", "192.168.213.114");
            //List<CopyIndex> t = new List<CopyIndex>();
            //t.Add(t1);
            ////t.Add(t2);
            //cp.CopyInfo = t;
            //Local l = new Local(ipList, cp);
            //Thread test = new Thread(() => l.StartGetMusic());
            //test.Start();
            //Thread.Sleep(1);
            //test.Join();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (musicList.SelectedIndex < 0) return;
            Audio audio = (Audio)musicList.SelectedItem;
            EditInfoWindow m = new EditInfoWindow();
            m.AudioTitle = audio.Title;
            m.Singer = audio.Artist;
            m.Album = audio.Album;
            m.LrcPath = audio.LyricsPath;
            if (audio.ImagePath != null)
            {
                if (audio.ImagePath.Length > 0) m.ImagePath = audio.ImagePath;
                else m.ImagePath = null;
            }
            else m.ImagePath = null;
            m.Owner = this;
            if (m.ShowDialog() == true)
            {
                audio.Title = audio.Artist = audio.Album = audio.LyricsPath = audio.ImagePath = null;

                if (!string.IsNullOrWhiteSpace(m.AudioTitle)) audio.Title = m.AudioTitle;
                if (!string.IsNullOrWhiteSpace(m.Singer)) audio.Artist = m.Singer;
                if (!string.IsNullOrWhiteSpace(m.Album)) audio.Album = m.Album;
                if (!string.IsNullOrWhiteSpace(m.LrcPath)) audio.LyricsPath = m.LrcPath;
                if (!string.IsNullOrWhiteSpace(m.ImagePath) || m.ImagePath == "") audio.ImagePath = m.ImagePath;
                
                if (musicDB != null)
                {
                    try
                    {
                        musicDB.SubmitChanges();
                    }
                    catch
                    {
                        MessageBox.Show("Can't connect to the media database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                musicList.Items.Refresh();
            }
        }

        private void addButton_Click(object sender = null, RoutedEventArgs e = null)
        {
            if (addFileDialog.ShowDialog() == true)
            {
                var query = from Audio a in musicList.Items
                            where a.MediaPath == addFileDialog.FileName
                            select a;
                if (query.Any()) return;
                
                Audio audio = new Audio();
                TagLib.Tag tag = TagLib.File.Create(addFileDialog.FileName).Tag;
                audio.Album = tag.Album == null ? "Unknown Album" : tag.Album;
                audio.Title = tag.Title == null ? "Unknown Title" : tag.Title;
                //if (tag.Pictures.Length > 0)
                //{
                //    using (MemoryStream albumArtworkMemStream = new MemoryStream(tag.Pictures[0].Data.Data))
                //    {
                //        BitmapImage albumImage = new BitmapImage();
                //        albumImage.BeginInit();
                //        albumImage.CacheOption = BitmapCacheOption.OnLoad;
                //        albumImage.StreamSource = albumArtworkMemStream;
                //        albumImage.EndInit();
                //        audio.ImagePath = albumImage;
                //    }
                //}

                audio.MediaPath = addFileDialog.FileName;

               // Console.WriteLine(audios.MediaPath);

                if (tag.JoinedPerformers.Length > 0) audio.Artist = tag.JoinedPerformers;
                else audio.Artist = "Unknown Artist";

                FileStream fs = new FileStream(audio.MediaPath, FileMode.Open);
                MD5 myMD5 = MD5.Create();
                byte[] hashvalue = myMD5.ComputeHash(fs);
                audio.HashValue = Peer.ConvertHashValue(hashvalue);

                Console.WriteLine("count: " +musicList.Items.Count);

                if(musicList.Items.Count==0) 
                    audio.Order=0;
                if (musicList.Items.Count > 0)
                {
                    audio.Order = ((Audio)(musicList.Items[musicList.Items.Count - 1])).Order+1;
                }
                Console.WriteLine("order: "+audio.Order);

                FileInfo f = new FileInfo(addFileDialog.FileName);
                audio.Size = (int?)f.Length;

                musicDB.Log = Console.Out;
                if (musicDB != null)
                {
                    try
                    {
                        musicDB.Audios.InsertOnSubmit((Audio)audio);
                        musicDB.SubmitChanges();
     
                    }
                    catch
                    {
                        MessageBox.Show("Can't connect to the media database.");
                    }
                }
                musicList.Items.Add(audio);
                Console.WriteLine(musicList.Items.Count);
                MusicCopy musicData = new MusicCopy(audio);
                musicDataList.Add(musicData);

                // DEBUG, outut all data 
                //var musicQuery = from audio in musicDB.Audios orderby audio.Order select audio;
                //var audioarray = musicQuery.ToArray<Audio>();
                //for (int i = 0; i < audioarray.Length; i++)
                //{
                //    musicList.Items.Add(audioarray[i]);
                //    Console.WriteLine("TEST: " + audioarray[i].Title);
                //}

               
            }
        }

        private void musicListItem_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            CloseFile();
            Audio audio = (Audio)((ListBoxItem)e.Source).Content;
            openFile(audio);
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
                    Console.WriteLine("close");
                    musicDB.Audios.DeleteOnSubmit((Audio)musicList.SelectedItem);
                    musicDB.SubmitChanges();
                }
                catch
                {
                    MessageBox.Show("Datebase Connection Failure","Error",MessageBoxButton.OK, MessageBoxImage.Error);
                }
                musicList.Items.RemoveAt(musicList.SelectedIndex);
            }
        }

        private void P2P_Setting_Click(object sender, RoutedEventArgs e)
        {
            P2P_Setting m = new P2P_Setting();

            m.IP1 = ipListInput[0];
            m.IP2 = ipListInput[1];
            m.IP3 = ipListInput[2];
            m.IP4 = ipListInput[3];
            m.IP5 = ipListInput[4];
            m.IP6 = ipListInput[5];
            m.IP7 = ipListInput[6];
            m.IP8 = ipListInput[7];
            m.IP9 = ipListInput[8];
            m.IP10 = ipListInput[9];

            m.Owner = this;
            if (m.ShowDialog() == true)
            {
                ipListInput[0] = m.IP1;
                ipListInput[1] = m.IP2;
                ipListInput[2] = m.IP3;
                ipListInput[3] = m.IP4;
                ipListInput[4] = m.IP5;
                ipListInput[5] = m.IP6;
                ipListInput[6] = m.IP7;
                ipListInput[7] = m.IP8;
                ipListInput[8] = m.IP9;
                ipListInput[9] = m.IP10;               
            }
            this.InputIPNumber = 0;
            for (int i = 0; i < 10; i++)
            {
                if (ipListInput[i] != "")
                {
                    this.InputIPNumber++;
                }
            }
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            searchList.Items.Clear();
            this.Keyword = SearchBox.Text;
            //Console.WriteLine("keyword is: " + this.Keyword);
            for (int i = 0; i < musicDataList.Count(); i++)
            {
                musicDataList[i].CopyNumber = 0;
                musicDataList[i].CopyInfo.Clear();
            }
            List<MusicCopy> searchResult = MusicSearchUtil.SearchedMusicList(this.Keyword, musicDataList);

            for (int j = 0; j < searchResult.Count; j++)
            {
                searchResult[j].CopyNumber = -1;
            }

            if (this.InputIPNumber > 0) 
            {
                //Console.WriteLine("InputIPNumber is: {0} \n", this.InputIPNumber);
                Local local = new Local(ipListInput, this.Keyword, this.InputIPNumber);
                List<MusicCopy> peerSearchResult = null;

                Thread test = new Thread(() => { peerSearchResult = local.StartSearch(); } );
                test.Start();
                test.Join();
                if (peerSearchResult != null)
                {
                    List<MusicCopy>[] searchResultArray = { searchResult, peerSearchResult };
                    searchResult = local.MergeMusicListTwo(searchResultArray);
                } 
            }

            musicList.Visibility = Visibility.Collapsed;
            searchList.Visibility = Visibility.Visible;

            if (searchResult.Count == 0)
            {

            }
            else
            {
                List<MusicCopy> sortedList = searchResult.OrderByDescending(m => m.Relevancy).ToList();

                int items = sortedList.Count();
                for (int i = 0; i < items; i++)
                {
                    searchList.Items.Add(sortedList[i]);
                }


                // test search
                Console.WriteLine(items);
                for (int i = 0; i < items; i++)
                {
                    Console.WriteLine("title is :" + sortedList[i].AudioData.Title);
                    Console.WriteLine("copyenum is :" + sortedList[i].CopyNumber);
                    for (int j = 0; j < sortedList[i].CopyNumber; j++)
                    {
                        Console.WriteLine("From " + sortedList[i].CopyInfo[j].FileName + "  where  ip = " + sortedList[i].CopyInfo[j].IPAddress);
                    }
                }
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            musicList.Visibility = Visibility.Visible;
            searchList.Visibility = Visibility.Collapsed;
            searchList.Items.Clear();
            SearchBox.Text = "";
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Search_Click(sender, e);
            }
        }

        private void searchListItem_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            CloseFile();
            MusicCopy musicCopy = (MusicCopy)((ListBoxItem)e.Source).Content;
            Local local = new Local(ipListInput, musicCopy);
            Thread test = new Thread(() => local.StartGetMusic());
            test.Start();
            Thread.Sleep(1);
        }
    }
}
