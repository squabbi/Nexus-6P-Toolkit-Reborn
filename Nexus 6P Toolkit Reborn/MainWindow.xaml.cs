using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Diagnostics;
using System.Net;
using MahApps.Metro.Controls.Dialogs;
using System.Runtime.InteropServices;
using System.Windows.Documents;
using AndroidCtrl;
using AndroidCtrl.ADB;
using AndroidCtrl.Tools;
using AndroidCtrl.Fastboot;
using System.Windows.Controls;
using Nexus_6P_Toolkit_Reborn.Resources.Dialogs;

namespace Nexus_6P_Toolkit_Reborn
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public const string MagiskUpdateStable = "https://raw.githubusercontent.com/topjohnwu/MagiskManager/update/stable.json";
        public const string MagiskUpdateBeta = "https://raw.githubusercontent.com/topjohnwu/MagiskManager/update/beta.json";
        public Magisk magisk;
        public MagiskManager magiskManager;

        public MainWindow()
        {
            if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1)
            {
                MessageBox.Show(
                    "There seems to be another instance of the toolkit running. Please make sure it is not running in the background.",
                    "Another Instance is running", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }

            InitializeComponent();
        }

        //Driver installation API        
        [DllImport("DIFXApi.dll", CharSet = CharSet.Unicode)]
        public static extern Int32 DriverPackagePreinstall(string DriverPackageInfPath, Int32 Flags);

        //Console appender for commands
        public void Add(List<string> msg)
        {
            foreach (string tmp in msg)
            {
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    console.Document.Blocks.Add(new Paragraph(new Run(tmp.Replace("(bootloader) ", ""))));
                });
            }
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                console.ScrollToEnd();
            });
        }

        //Console appender
        public void ConsoleAppend(string message)
        {
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                console.AppendText(string.Format("\n{0} :: {1}", DateTime.Now, message));
                console.ScrollToEnd();
            });
        }

        #region AndroidCtrl Start

        private void SetDeviceList()
        {
            string active = String.Empty;
            if (deviceselector.Items.Count != 0)
            {
                active = ((DataModelDevicesItem)deviceselector.SelectedItem).Serial;
            }

            App.Current.Dispatcher.Invoke((Action)delegate
            {
                // Here we refresh our combobox
                deviceselector.Items.Clear();
            });

            // This will get the currently connected ADB devices
            IEnumerable<DataModelDevicesItem> adbDevices = ADB.Devices();

            // This will get the currently connected Fastboot devices
            IEnumerable<DataModelDevicesItem> fastbootDevices = Fastboot.Devices();

            foreach (DataModelDevicesItem device in adbDevices)
            {
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    // here goes the add command ;)
                    deviceselector.Items.Add(device);
                    ConsoleAppend(String.Format("Device connected: {0} ({1}), State: {2}.", device.Model, device.Serial.ToString(), device.State.ToString()));
                });
            }
            foreach (DataModelDevicesItem device in fastbootDevices)
            {
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    deviceselector.Items.Add(device);
                    ConsoleAppend(String.Format("Device connected: {0} ({1}), State: {2}.", device.Model, device.Serial.ToString(), device.State.ToString()));
                });
            }
            if (deviceselector.Items.Count != 0)
            {
                int i = 0;
                bool empty = true;
                foreach (DataModelDevicesItem device in deviceselector.Items)
                {
                    if (device.Serial == active)
                    {
                        empty = false;
                        deviceselector.SelectedIndex = i;
                        break;
                    }
                    i++;
                }
                if (empty)
                {

                    // This calls will select the BASE class if we have no connected devices
                    ADB.SelectDevice();
                    Fastboot.SelectDevice();

                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        deviceselector.SelectedIndex = 0;
                    });
                }
            }
        }

        private void DeviceDetectionService()
        {
            ADB.Start();
            // Here we initiate the BASE Fastboot instance
            Fastboot.Instance();

            //This will starte a thread which checks every 10 sec for connected devices and call the given callback
            if (Fastboot.ConnectionMonitor.Start())
            {
                //Here we define our callback function which will be raised if a device connects or disconnects
                Fastboot.ConnectionMonitor.Callback += ConnectionMonitorCallback;

                // Here we check if ADB is running and initiate the BASE ADB instance (IsStarted() will everytime check if the BASE ADB class exists, if not it will create it)
                if (ADB.IsStarted)
                {
                    //Here we check for connected devices
                    SetDeviceList();

                    //This will starte a thread which checks every 10 sec for connected devices and call the given callback
                    if (ADB.ConnectionMonitor.Start()) { ADB.ConnectionMonitor.Callback += ConnectionMonitorCallback; }
                    //Here we define our callback function which will be raised if a device connects or disconnects
                }
            }
        }

        public void ConnectionMonitorCallback(object sender, ConnectionMonitorArgs e)
        {
            App.Current.Dispatcher.Invoke((Action)delegate { SetDeviceList(); });
        }

        private void SelectDeviceInstance(object sender, SelectionChangedEventArgs e)
        {
            if (deviceselector.Items.Count != 0)
            {
                DataModelDevicesItem device = (DataModelDevicesItem)deviceselector.SelectedItem;
                // This will select the given device in the Fastboot and ADB class
                Fastboot.SelectDevice(device.Serial);
                ADB.SelectDevice(device.Serial);
            }
        }

        public void CheckandDeploy()
        {
            if (ADB.IntegrityCheck() == false) { Deploy.ADB(); }
            if (Fastboot.IntegrityCheck() == false) { Deploy.Fastboot(); }
            // Check if ADB is running
            if (ADB.IsStarted)
            {
                ADB.Stop();             // Stop ADB
                ADB.Stop(true);         // Force Stop ADB
            }
            else { ADB.Start(); }       // Start ADB
        }

        #endregion
        #region Magisk JSON

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
            switch (Properties.Settings.Default.MagiskChannel)
            {
                case 0: return MagiskUpdateStable;
                case 1: return MagiskUpdateBeta;
                case 2: return Properties.Settings.Default.MagiskCustomChannelUrl;
                default: return MagiskUpdateStable;
            }
        }

        public void RefreshMagisk()
        {
            //disable action button upon refresh
            this.Btn_installMagisk.IsEnabled = false;
            this.Btn_installMagisk.Content = "Checking...";

            String jsonString;

            using (WebClient webClient = new WebClient()) { jsonString = webClient.DownloadString(UpdateChannel()); }
                                                                            //grab json from topjohnwu's repo, depending on stable/beta/custom

            JToken root = JObject.Parse(jsonString);
            JToken _magisk = root["magisk"];                                 //get the objects for the 'magisk' token
            JToken _magiskManager = root["app"];                             //get the objects for the 'app' token
            magisk = JsonConvert.DeserializeObject<Magisk>(_magisk.ToString());
            magiskManager = JsonConvert.DeserializeObject<MagiskManager>(_magiskManager.ToString());

            //update visual items
            switch (Properties.Settings.Default.MagiskChannel)
            {
                case 0: Tblk_magiskChannel.Text = "Stable"; break;
                case 1: Tblk_magiskChannel.Text = "Beta"; break;
                case 2: Tblk_magiskChannel.Text = "Custom"; break;
                default: Tblk_magiskChannel.Text = "Stable"; break;
            }

            Tblk_magiskManagerVersion.Text = magiskManager.Version;

            Tblk_magiskVersion.Text = magisk.Version;
            Btn_installMagisk.IsEnabled = true;
            Btn_installMagisk.Content = "Install";
        }

        #endregion

        #region Indicators

        private void statusProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ConsoleAppend(string.Format("{0}% completed...", statusProgress.Value.ToString()));
        }

        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshMagisk();            //update Magisk info on startup
            CheckandDeploy();           //check and deploy Platform Tools
            DeviceDetectionService();   //start device detection monitor
        }

        private void Btn_magiskChangelog_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(magisk.Note);
        }

        private void hyLink_unlockBootloaderTip_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ADB.Dispose();
            Fastboot.Dispose();
        }

        private void Btn_magiskSettings_Click(object sender, RoutedEventArgs e)
        {
            MagiskSettingsDialog msd = new MagiskSettingsDialog
            {
                Owner = this
            };
            msd.ShowDialog();
            RefreshMagisk();  //Refresh Magisk
        }
    }
}
