using System;
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
        private System.Windows.Forms.OpenFileDialog openDialog;

        public MainWindow()
        {
            InitializeComponent();

            this.openDialog = new System.Windows.Forms.OpenFileDialog();
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

        private void load_Click(object sender, RoutedEventArgs e)
        {
            openDialog.ShowDialog();
            //CloseFile();
            /*try
            {
                WaveLib.WaveStream S = new WaveLib.WaveStream(OpenDlg.FileName);
                if (S.Length <= 0)
                    throw new Exception("Invalid WAV file");
                m_Format = S.Format;
                if (m_Format.wFormatTag != (short)WaveLib.WaveFormats.Pcm && m_Format.wFormatTag != (short)WaveLib.WaveFormats.Float)
                    throw new Exception("Olny PCM files are supported");

                m_AudioStream = S;
            }
            catch (Exception e)
            {
                CloseFile();
                MessageBox.Show(e.Message);
            }*/
        }
    }
}
