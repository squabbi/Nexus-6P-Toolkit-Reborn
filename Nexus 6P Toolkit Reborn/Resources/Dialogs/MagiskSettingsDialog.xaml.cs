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

namespace Nexus_6P_Toolkit_Reborn.Resources.Dialogs
{
    /// <summary>
    /// Interaction logic for MagiskSettingsDialog.xaml
    /// </summary>
    public partial class MagiskSettingsDialog
    {
        public MagiskSettingsDialog()
        {
            InitializeComponent();
        }

        private bool savePress;

        private void Cmbox_magiskUpdateChannel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Cmbox_magiskUpdateChannel.SelectedIndex == 2) { Txtbox_customUpdateUrl.Visibility = Visibility.Visible; }
            else { Txtbox_customUpdateUrl.Visibility = Visibility.Hidden; }
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Cmbox_magiskUpdateChannel.SelectionChanged += Cmbox_magiskUpdateChannel_SelectionChanged;
            switch (Properties.Settings.Default.MagiskChannel)
            {
                case 0:     //Stable
                    Cmbox_magiskUpdateChannel.SelectedIndex = 0;
                    break;
                case 1:     //Beta
                    Cmbox_magiskUpdateChannel.SelectedIndex = 1;
                    break;
                case 2:     //Custom
                    Cmbox_magiskUpdateChannel.SelectedIndex = 2;
                    break;
                default:    //Stable
                    Cmbox_magiskUpdateChannel.SelectedIndex = 0;
                    break;
            }
        }

        private void Btn_saveSettings_Click(object sender, RoutedEventArgs e)
        {
            switch (Cmbox_magiskUpdateChannel.SelectedIndex)
            {
                case 0:     //Stable
                    Properties.Settings.Default.MagiskChannel = 0;
                    break;
                case 1:     //Beta
                    Properties.Settings.Default.MagiskChannel = 1;
                    break;
                case 2:     //Custom
                    Properties.Settings.Default.MagiskChannel = 2;
                    break;
                default:    //Stable
                    Properties.Settings.Default.MagiskChannel = 0;
                    break;
            }
            Properties.Settings.Default.Save();
            savePress = true;
            this.Close();
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!savePress)
            {
                MessageBoxResult result = MessageBox.Show("Really close?", "Warning", MessageBoxButton.YesNo);
                if (result != MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                }
            }
        }
    }
}
