using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
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
        public const string MagiskUpdateStable = "https://raw.githubusercontent.com/topjohnwu/MagiskManager/update/stable.json0";
        public const string MagiskUpdateBeta = "https://raw.githubusercontent.com/topjohnwu/MagiskManager/update/beta.json";
        public Magisk magisk;
        public MagiskManager magiskManager;

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

        public async void RefreshMagiskInfo()
        {
            //disable action button upon refresh
            this.btn_installMagisk.IsEnabled = false;
            this.btn_installMagisk.Content = "Checking...";

            String jsonString;

            using (WebClient webClient = new WebClient())
            {
                jsonString = webClient.DownloadString(UpdateChannel());     //grab json from topjohnwu's repo, depending on stable/beta/custom
            }

            JToken root = JObject.Parse(jsonString);
            JToken _magisk = root["magisk"];                                 //get the objects for the 'magisk' token
            JToken _magiskManager = root["app"];                             //get the objects for the 'app' token
            magisk = JsonConvert.DeserializeObject<Magisk>(_magisk.ToString());
            magiskManager = JsonConvert.DeserializeObject<MagiskManager>(_magiskManager.ToString());

            //update visual items
            if (Properties.Settings.Default.UpdateIsBeta == false)
                tblk_magiskChannel.Text = "Stable";
            else tblk_magiskChannel.Text = "Beta";

            tblk_magiskVersion.Text = magisk.Version;
            btn_installMagisk.IsEnabled = true;
            btn_installMagisk.Content = "Install";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshMagiskInfo();        //update Magisk info on startup
        }
    }
}
