using System;
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
        public string title { get { return title; } set { titleBox.Text = title = value; } }
        public string singer { get { return singer; } set { singerBox.Text = singer = value; } }
        public string album { get { return album; } set { albumBox.Text = album = value; } }
        public string lrcPath { get { return lrcPath; } set { lrcBox.Text = lrcPath = value; } }
        public string coverPath { get { return coverPath; } set { CoverIMG.Source = (ImageSource)imgSrcConverter.ConvertFromString(value); } }

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
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            this.title = string.Copy(titleBox.Text);
            this.singer = singerBox.Text;
            this.album = albumBox.Text;
            this.lrcPath = lrcBox.Text;
            this.coverPath = tmpCoverPath;
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
            openImgDialog.ShowDialog();
            tmpCoverPath = openImgDialog.FileName;
            CoverIMG.Source = (ImageSource)imgSrcConverter.ConvertFromString(tmpCoverPath);
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            tmpCoverPath = "";
            CoverIMG.Source = null;
        }
    }

}
