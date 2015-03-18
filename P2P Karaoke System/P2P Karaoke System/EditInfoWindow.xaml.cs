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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace P2P_Karaoke_System
{
    /// <summary>
    /// Interaction logic for EditInfoWindow.xaml
    /// </summary>
    public partial class EditInfoWindow : Window
    {
        private string mediaPath;
        private string audioTitle;
        private string singer;
        private string album;
        private string lrcPath;
        private string imagePath;
        private ImageSource defaultImage;

        public string MediaPath { get { return mediaPath; } set { mediaPath = value; } }
        public string AudioTitle { get { return audioTitle; } set { titleBox.Text = audioTitle = value; } }
        public string Singer { get { return singer; } set { singerBox.Text = singer = value; } }
        public string Album { get { return album; } set { albumBox.Text = album = value; } }
        public string LrcPath { get { return lrcPath; } set { lrcBox.Text = lrcPath = value; } }

        public string ImagePath
        {
            get { return imagePath; }
            set
            {
                imagePath = value;              
            }
        }

        private string tmpCoverPath;
        private OpenFileDialog openLRCDialog, openImgDialog;
        private ImageSourceConverter imgSrcConverter;

        public EditInfoWindow()
        {
            InitializeComponent();
            openLRCDialog = new OpenFileDialog();
            openImgDialog = new OpenFileDialog();
            openLRCDialog.Filter = "Lyrics File (*.lrc)|*.lrc";
            openImgDialog.Filter = "Image File (*.jpg, *.png, *.bmp)|*.jpg;*.png;*.bmp";
            imgSrcConverter = new ImageSourceConverter();
            defaultImage = CoverIMG.Source;
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            this.AudioTitle = string.Copy(titleBox.Text);
            this.Singer = singerBox.Text;
            this.Album = albumBox.Text;
            this.LrcPath = lrcBox.Text;
            this.ImagePath = tmpCoverPath;
            this.DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OpenLRC_Click(object sender, RoutedEventArgs e)
        {
            openLRCDialog.ShowDialog();
            lrcBox.Text = openLRCDialog.FileName;
        }

        private void OpenIMG_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)openImgDialog.ShowDialog())
            {
                tmpCoverPath = openImgDialog.FileName;
            }
            else
            {
                tmpCoverPath = "";
            }

            try
            {
                CoverIMG.Source = (ImageSource)imgSrcConverter.ConvertFromString(tmpCoverPath);
            }
            catch
            {
                CoverIMG.Source = defaultImage;
            }
           
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            tmpCoverPath = "";
            CoverIMG.Source = defaultImage;
        }
    }

}
