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

        public MainWindow()
        {
            InitializeComponent();

            this.openDialog = new Microsoft.Win32.OpenFileDialog();
            this.openDialog.DefaultExt = "wav";
            this.openDialog.Filter = "WAV files|*.wav";
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
        }

        void timer_Tick(object sender, EventArgs e)
        {
        }

        public void CloseFile()
        {

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
