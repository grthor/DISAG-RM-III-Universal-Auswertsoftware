using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO.Ports;
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
using System.Windows.Shapes;
using System.Windows.Threading;

namespace V1Auslesen
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private SerialPort serialPort1 = new SerialPort();
        private StringBuilder readBuffer = new StringBuilder();
        private ObservableCollection<Shot> shots = new ObservableCollection<Shot>();

        public MainWindow()
        {
            InitializeComponent();

            string[] ports = SerialPort.GetPortNames();
            foreach (String port in ports)
            {
                comboBoxCOMPorts.Items.Add(port);
            }
            if (ports.Length > 0)
            {
                comboBoxCOMPorts.SelectedItem = ports[0];
            }

            // Bind dataGridShots to shots
            dataGridShots.ItemsSource = shots;

        }

        private void buttonConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                serialPort1.PortName = comboBoxCOMPorts.Text;
                serialPort1.BaudRate = 2400;
                serialPort1.DataBits = 8;
                serialPort1.StopBits = StopBits.One;
                serialPort1.Parity = Parity.None;
                // Wenn es nicht läuf hier 
                // serialPort1.Handshake = Handshake.RequestToSend;

                serialPort1.Open();
                serialPort1.DataReceived += new SerialDataReceivedEventHandler(SerialPort1_DataReceived);

                buttonConnect.IsEnabled = false;
                buttonDisconnect.IsEnabled = true;
                labelStatus.Content = "verbunden";
                buttonRefreshDSRCTS.IsEnabled = true;
            } 
            catch (Exception err)
            {
                MessageBox.Show(err.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void buttonDisconnect_Click(object sender, RoutedEventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                try
                {
                    serialPort1.Close();

                    labelStatus.Content = "getrennt";
                    buttonDisconnect.IsEnabled = false;
                    buttonConnect.IsEnabled = true;
                    buttonRefreshDSRCTS.IsEnabled = false;
                }
                catch (Exception err)
                {
                    MessageBox.Show(err.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void checkBoxDTR_Checked(object sender, RoutedEventArgs e)
        {
            serialPort1.DtrEnable = true;
        }

        private void checkBoxDTR_Unchecked(object sender, RoutedEventArgs e)
        {
            serialPort1.DtrEnable = false;
        }

        private void checkBoxRTS_Unchecked(object sender, RoutedEventArgs e)
        {
            serialPort1.RtsEnable = false;
        }

        private void checkBoxRTS_Checked(object sender, RoutedEventArgs e)
        {
            serialPort1.RtsEnable = true;
        }

        private void buttonRefreshDSRCTS_Click(object sender, RoutedEventArgs e)
        {
            if (serialPort1.CtsHolding)
                labelCTS.Content = "on";
            else
                labelCTS.Content = "off";

            if (serialPort1.DsrHolding)
                labelDSR.Content = "on";
            else
                labelDSR.Content = "off";
        }

        private void SerialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            serialPort1.NewLine = "\r";
            string dataIn = serialPort1.ReadLine();
            // If not working change to 
            //string dataIn = serialPort1.ReadExisting();
            Dispatcher.Invoke(new UpdateUiTextDelegate(WriteData), dataIn);
            Dispatcher.Invoke(new ParseShotDelegate(ParseShot), dataIn);
        }

        private delegate void UpdateUiTextDelegate(string text);
        private delegate void ParseShotDelegate(string text);

        private void WriteData(string text)
        {
            textBoxReceivedData.Text += text;
        }

        private void ParseShot(string text)
        {
            string[] shot = new String[6];
            shot = text.Split(";");

            foreach (String str in shot)
            {
                textBoxReceivedData.Text += "\n" + str;
            }

            Shot s = new Shot();
            s.SchussnummerInSerie = Int16.Parse(shot[0]);
            s.Ringe = float.Parse(shot[1], CultureInfo.InvariantCulture);
            s.Teiler = float.Parse(shot[2], CultureInfo.InvariantCulture);
            s.XAbweichung = float.Parse(shot[3], CultureInfo.InvariantCulture);
            s.YAbweichung = float.Parse(shot[4], CultureInfo.InvariantCulture);
            s.Markierung = shot[5].Trim();

            shots.Add(s);
        }


    }


}
