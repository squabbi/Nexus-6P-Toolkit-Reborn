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
using System.Windows.Navigation;

namespace Nexus_6P_Toolkit_Reborn
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public class JsonResults
        {
            public Magisk _Magisk { get; set; }
            public MagiskManager _MagiskManager { get; set; }
        }

        public class MagiskManager
        {
            public string version;
            public int versionCode;
            public string link;
        }

        public class Magisk
        {
            public string version;
            public int versionCode;
            public string link;
            public string note;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
