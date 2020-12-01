using System;
using System.Collections.Generic;
using System.Management;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Devices.Power;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace BluetoothApp
{
    public sealed partial class MainPage : Page
    {
        bool reportRequested;
        public MainPage()
        {
            InitializeComponent();
            Battery.AggregateBattery.ReportUpdated += AggregateBattery_ReportUpdated;
        }


        private void GetBatteryReport(object sender, RoutedEventArgs e)
        {
            // Clear UI
            BatteryReportPanel.Children.Clear();


            if (AggregateButton.IsChecked == true)
            {
                // Request aggregate battery report
                RequestAggregateBatteryReport();
            }
            else if(IndividualButton.IsChecked == true)
            {
                // Request individual battery report
                RequestIndividualBatteryReports();
            }
            else
            {
                // Request bluetooth baterry report
                RequestIndividualBluetoothBatteryReports();
            }

            // Note request
            reportRequested = true;
        }

        private void RequestAggregateBatteryReport()
        {
            // Create aggregate battery object
            var aggBattery = Battery.AggregateBattery;

            // Get report
            var report = aggBattery.GetReport();

            // Update UI
            AddReportUI(BatteryReportPanel, report, aggBattery.DeviceId);
        }

        async private void RequestIndividualBatteryReports()
        {
            // Find batteries 
            var deviceInfo = await DeviceInformation.FindAllAsync(Battery.GetDeviceSelector());
            
            foreach (DeviceInformation device in deviceInfo)
            {
                try
                {
                    string name = device.Name;
                    var battery = await Battery.FromIdAsync(device.Id);
                   
                    // Get report
                    var report = battery.GetReport();

                    // Update UI
                    AddReportUI(BatteryReportPanel, report, battery.DeviceId);
                }
                catch { /* Add error handling, as applicable */ }
            }
        }


        async private void RequestIndividualBluetoothBatteryReports()
        {
            // Find batteries 
            //var usbDevices = GetUSBDevices();
            //Paired bluetooth devices
            //DeviceInformationCollection pairedBluetoothDevices =
            //       await DeviceInformation.FindAllAsync(BluetoothDevice.GetDeviceSelectorFromPairingState(true));
            //Connected bluetooth devices
            DeviceInformationCollection connectedBluetoothDevices =
                   await DeviceInformation.FindAllAsync(BluetoothDevice.GetDeviceSelectorFromConnectionStatus(BluetoothConnectionStatus.Connected));
            foreach (DeviceInformation device in connectedBluetoothDevices)
            {
                try
                {
                    string name = device.Name;
                    // Create battery object
                    //Guid batteryLevel = GattCharacteristicUuids.BatteryLevel;
                    var battery = await Battery.FromIdAsync(device.Id);
                    GattDeviceService gattDeviceService = await GattDeviceService.FromIdAsync(device.Id);
                    var batteryLevel = gattDeviceService;

                    // Get report
                    var report = battery.GetReport();

                    // Update UI
                    AddReportUI(BatteryReportPanel, report, battery.DeviceId);
                }
                catch { /* Add error handling, as applicable */ }
            }
        }

        private void AddReportUI(StackPanel sp, BatteryReport report, string DeviceID)
        {
            // Create battery report UI
            TextBlock txt1 = new TextBlock { Text = "Device ID: " + DeviceID };
            txt1.FontSize = 15;
            txt1.Margin = new Thickness(0, 15, 0, 0);
            txt1.TextWrapping = TextWrapping.WrapWholeWords;

            TextBlock txt2 = new TextBlock { Text = "Battery status: " + report.Status };
            txt2.FontStyle = FontStyle.Italic;
            txt2.Margin = new Thickness(0, 0, 0, 15);

            TextBlock txt3 = new TextBlock { Text = "Charge rate (mW): " + report.ChargeRateInMilliwatts };
            TextBlock txt4 = new TextBlock { Text = "Design energy capacity (mWh): " + report.DesignCapacityInMilliwattHours };
            TextBlock txt5 = new TextBlock { Text = "Fully-charged energy capacity (mWh): " + report.FullChargeCapacityInMilliwattHours };
            TextBlock txt6 = new TextBlock { Text = "Remaining energy capacity (mWh): " + report.RemainingCapacityInMilliwattHours };

            // Create energy capacity progress bar & labels
            TextBlock pbLabel = new TextBlock { Text = "Percent remaining energy capacity" };
            pbLabel.Margin = new Thickness(0, 10, 0, 5);
            pbLabel.FontFamily = new FontFamily("Segoe UI");
            pbLabel.FontSize = 11;

            ProgressBar pb = new ProgressBar();
            pb.Margin = new Thickness(0, 5, 0, 0);
            pb.Width = 200;
            pb.Height = 10;
            pb.IsIndeterminate = false;
            pb.HorizontalAlignment = HorizontalAlignment.Left;

            TextBlock pbPercent = new TextBlock();
            pbPercent.Margin = new Thickness(0, 5, 0, 10);
            pbPercent.FontFamily = new FontFamily("Segoe UI");
            pbLabel.FontSize = 11;

            // Disable progress bar if values are null
            if ((report.FullChargeCapacityInMilliwattHours == null) ||
                (report.RemainingCapacityInMilliwattHours == null))
            {
                pb.IsEnabled = false;
                pbPercent.Text = "N/A";
            }
            else
            {
                pb.IsEnabled = true;
                pb.Maximum = Convert.ToDouble(report.FullChargeCapacityInMilliwattHours);
                pb.Value = Convert.ToDouble(report.RemainingCapacityInMilliwattHours);
                pbPercent.Text = ((pb.Value / pb.Maximum) * 100).ToString("F2") + "%";
            }

            // Add controls to stackpanel
            sp.Children.Add(txt1);
            sp.Children.Add(txt2);
            sp.Children.Add(txt3);
            sp.Children.Add(txt4);
            sp.Children.Add(txt5);
            sp.Children.Add(txt6);
            sp.Children.Add(pbLabel);
            sp.Children.Add(pb);
            sp.Children.Add(pbPercent);
        }

        async private void AggregateBattery_ReportUpdated(Battery sender, object args)
        {
            if (reportRequested)
            {

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    // Clear UI
                    BatteryReportPanel.Children.Clear();


                    if (AggregateButton.IsChecked == true)
                    {
                        // Request aggregate battery report
                        RequestAggregateBatteryReport();
                    }
                    else
                    {
                        // Request individual battery report
                        RequestIndividualBatteryReports();
                    }
                });
            }
        }

        static List<USBDeviceInfo> GetUSBDevices()
        {
            List<USBDeviceInfo> devices = new List<USBDeviceInfo>();
            ManagementObjectCollection collection; 
            using (var searcher = new ManagementObjectSearcher(@"Select * From Win32_PnPEntity")) 
                collection = searcher.Get();      

            foreach (var device in collection)
            { 
                devices.Add(new USBDeviceInfo(
                        (string)device.GetPropertyValue("DeviceID"), 
                        (string)device.GetPropertyValue("PNPDeviceID"), 
                        (string)device.GetPropertyValue("Description")));
            }
            
            collection.Dispose(); 
            return devices;
        }

        class USBDeviceInfo
        { 
            public USBDeviceInfo(string deviceID, string pnpDeviceID, string description)
            {
                DeviceID = deviceID;
                PnpDeviceID = pnpDeviceID;
                Description = description;
            }

            private string DeviceID { get; set; }
            private string PnpDeviceID { get; set; }
            private string Description { get; set; }
        }
    }
}