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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Diagnostics;
using System.Net;

namespace Nexus_6P_Toolkit_Reborn
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public const string MagiskUpdateStable = "https://raw.githubusercontent.com/topjohnwu/MagiskManager/update/stable.json";
        public const string MagiskUpdateBeta = "https://raw.githubusercontent.com/topjohnwu/MagiskManager/update/beta.json";

        public MainWindow()
        {
            InitializeComponent();
        }

        [JsonObject]
        public class MagiskManager
        {
            public string Version { get; set; }
            public int VersionCode { get; set; }
            public string Link { get; set; }
        }

        [JsonObject]
        public class Magisk
        {
            public string Version { get; set; }
            public int VersionCode { get; set; }
            public string Link { get; set; }
            public string Note { get; set; }
        }

        public String UpdateChannel()
        {
            if (Properties.Settings.Default.UpdateIsBeta == false)         //check for update channel. TODO: Implement custom update channel.
                return MagiskUpdateStable;

            return MagiskUpdateBeta;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            String jsonString;

            using (WebClient webClient = new WebClient())
            {
                jsonString = webClient.DownloadString(UpdateChannel());
            }

            JToken root = JObject.Parse(jsonString);
            JToken magisk = root["magisk"];
            JToken magiskManager = root["app"];
            Magisk _magisk = JsonConvert.DeserializeObject<Magisk>(magisk.ToString());
            MagiskManager _magiskManager = JsonConvert.DeserializeObject<MagiskManager>(magiskManager.ToString());

            Process.Start(_magisk.Note);
            MessageBox.Show(_magiskManager.Version);
        }
    }
}
