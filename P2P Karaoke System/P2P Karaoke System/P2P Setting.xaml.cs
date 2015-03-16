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
using System.Net;

namespace P2P_Karaoke_System
{
    /// <summary>
    /// Interaction logic for P2P_Setting.xaml
    /// </summary>
    public partial class P2P_Setting : Window
    {
        private string ip1;
        private string ip2;
        private string ip3;
        private string ip4;
        private string ip5;
        private string ip6;
        private string ip7;
        private string ip8;
        private string ip9;
        private string ip10;
        
        public string IP1 { get { return ip1; } set { IPBOX1.Text = ip1 = value; } }
        public string IP2 { get { return ip2; } set { IPBOX2.Text = ip2 = value; } }
        public string IP3 { get { return ip3; } set { IPBOX3.Text = ip3 = value; } }
        public string IP4 { get { return ip4; } set { IPBOX4.Text = ip4 = value; } }
        public string IP5 { get { return ip5; } set { IPBOX5.Text = ip5 = value; } }
        public string IP6 { get { return ip6; } set { IPBOX6.Text = ip6 = value; } }
        public string IP7 { get { return ip7; } set { IPBOX7.Text = ip7 = value; } }
        public string IP8 { get { return ip8; } set { IPBOX8.Text = ip8 = value; } }
        public string IP9 { get { return ip9; } set { IPBOX9.Text = ip9 = value; } }
        public string IP10 { get { return ip10; } set { IPBOX10.Text = ip10 = value; } }


        public P2P_Setting()
        {
            InitializeComponent();
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            ip1 = IPBOX1.Text;
            ip2 = IPBOX2.Text;
            ip3 = IPBOX3.Text;
            ip4 = IPBOX4.Text;
            ip5 = IPBOX5.Text;
            ip6 = IPBOX6.Text;
            ip7 = IPBOX7.Text;
            ip8 = IPBOX8.Text;
            ip9 = IPBOX9.Text;
            ip10 = IPBOX10.Text;
            IPAddress checkPart;
            if ((ip1 != "" && !IPAddress.TryParse(ip1, out checkPart)) || (ip2 != "" && !IPAddress.TryParse(ip2, out checkPart)) ||
                (ip3 != "" && !IPAddress.TryParse(ip3, out checkPart)) || (ip4 != "" && !IPAddress.TryParse(ip4, out checkPart)) ||
                (ip5 != "" && !IPAddress.TryParse(ip5, out checkPart)) || (ip6 != "" && !IPAddress.TryParse(ip6, out checkPart)) ||
                (ip7 != "" && !IPAddress.TryParse(ip7, out checkPart)) || (ip8 != "" && !IPAddress.TryParse(ip8, out checkPart)) ||
                (ip9 != "" && !IPAddress.TryParse(ip9, out checkPart)) || (ip10 != "" && !IPAddress.TryParse(ip10, out checkPart)))
            {
                MessageBox.Show("Invalid IP Address!");
            }
            else
            {
                this.DialogResult = true;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
