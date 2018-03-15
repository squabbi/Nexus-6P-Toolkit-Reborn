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
                });
            }
            foreach (DataModelDevicesItem device in fastbootDevices)
            {
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    deviceselector.Items.Add(device);
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
                    if (ADB.ConnectionMonitor.Start())
                    {
                        //Here we define our callback function which will be raised if a device connects or disconnects
                        ADB.ConnectionMonitor.Callback += ConnectionMonitorCallback;
                    }
                }
            }
        }

        public void ConnectionMonitorCallback(object sender, ConnectionMonitorArgs e)
        {
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                // Do what u want with the "List<DataModelDevicesItem> e.Devices"
                // The "sender" is a "string" and returns "adb" or "fastboot"
                SetDeviceList();
            });
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
            if (Properties.Settings.Default.UpdateIsBeta == false)         //check for update channel. TODO: Implement custom update channel.
                return MagiskUpdateStable;

            return MagiskUpdateBeta;
        }

        public void RefreshMagiskInfo()
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

        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshMagiskInfo();        //update Magisk info on startup
            DeviceDetectionService();   //start device detection monitor
        }

        private void Btn_magiskChangelog_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(magisk.Note);
        }
    }
}
