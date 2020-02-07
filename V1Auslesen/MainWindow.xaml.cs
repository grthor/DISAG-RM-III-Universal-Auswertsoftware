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
        private ObservableCollection<Teilnehmer> teilnehmer = new ObservableCollection<Teilnehmer>();
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

            createtTestUser();
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
                buttonSnedCommand.IsEnabled = true;
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
                    buttonSnedCommand.IsEnabled = false;
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
            //s.SchussnummerInSerie = Int16.Parse(shot[0]);
            s.Ringe = float.Parse(shot[1], CultureInfo.InvariantCulture);
            s.Teiler = float.Parse(shot[2], CultureInfo.InvariantCulture);
            s.XAbweichung = float.Parse(shot[3], CultureInfo.InvariantCulture);
            s.YAbweichung = float.Parse(shot[4], CultureInfo.InvariantCulture);
            s.Markierung = shot[5].Trim();

            shots.Add(s);
        }

        private void buttonSnedCommand_Click(object sender, RoutedEventArgs e)
        {
            sendData();
        }

        private void textBoxBefehl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                sendData();
            }
        }

        private void sendData()
        {
            if (!textBoxBefehl.Text.Equals(""))
            {
                if (serialPort1.IsOpen)
                {
                    //Gesendeten Kommand in der Textbox anzeigen.
                    textBoxOutput.AppendText(textBoxBefehl.Text.Trim() + "\n");
                    textBoxOutput.ScrollToEnd();

                    //Befehl senden
                    serialPort1.Write(textBoxBefehl.Text.Trim());
                } else
                {
                    MessageBox.Show("Keine Verbindung", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            } 
            else
            {
                MessageBox.Show("Kein Befehl eingegeben", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }

        private void dataGridTeilnehmer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            object o1 = dataGridTeilnehmer.CurrentItem;
            // Checken ob es überhaupt ein aktuelles Objekt gibt.
            // Beim Aufbau der GUI gibt es keine Objekte.
            if (o1 != null)
            {
                // Prüfen, ob ein neuer Eintrag gemacht werden soll.
                if (o1 == CollectionView.NewItemPlaceholder)
                {
                    // Wenn ja, neuen Teilnehmer erstellen und anzeigen.
                    Teilnehmer teilnehmerTemp = new Teilnehmer();
                    teilnehmer.Add(teilnehmerTemp);
                    dataGridTeilnehmer.CurrentItem = teilnehmerTemp;
                    dataGridSeries.ItemsSource = teilnehmerTemp.Ringe;
                } 
                else
                {
                    // Wenn nein, vorhandenen Teilnehmer anzeigen.
                    dataGridSeries.ItemsSource = ((Teilnehmer)dataGridTeilnehmer.CurrentItem).Ringe;
                }
                
            }

        }


        private void createtTestUser()
        {
            //Create some default users
            var user1 = new Teilnehmer
            {
                Startnummer = 1,
                Vorname = "Max",
                Nachname = "Mustermann",
                Mannschaft = "SVL",
                Geschlecht = Geschl.m,
                Ringe = new MyObservableCollection<Series>() 
            };

            user1.Ringe.Add(new Series {
                Serie = 1,
                Schuss1 = new Shot { Ringe = 9.7 },
                Schuss2 = new Shot { Ringe = 10.123},
                Schuss3 = new Shot { Ringe = 8.999},
                Schuss4 = new Shot { Ringe = 9.8},
                Schuss5 = new Shot { Ringe = 8.2}
            });

            user1.Ringe.Add(new Series
            {
                Serie = 2,
                Schuss1 = new Shot { Ringe = 10.1 },
                Schuss2 = new Shot { Ringe = 6.9 },
                Schuss3 = new Shot { Ringe = 8.2 },
                Schuss4 = new Shot { Ringe = 10.0 },
                Schuss5 = new Shot { Ringe = 9.5 }
            });

            user1.Ringe.Add(new Series
            {
                Serie = 3,
                Schuss1 = new Shot { Ringe = 10.1 },
                Schuss2 = new Shot { Ringe = 6.9 },
                Schuss3 = new Shot { Ringe = 8.2 },
                Schuss4 = new Shot { Ringe = 10.0 },
                Schuss5 = new Shot { Ringe = 9.5 }
            });

            teilnehmer.Add(user1);

            var user2 = new Teilnehmer();
            user2.Startnummer = 2;
            user2.Vorname = "Hugh";
            user2.Nachname = "Jackman";
            user2.Mannschaft = "Hollywood";

            user2.Ringe.Add(new Series
            {
                Serie = 1,
                Schuss1 = new Shot { Ringe = 6.2 },
                Schuss2 = new Shot { Ringe = 7.1 },
                Schuss3 = new Shot { Ringe = 5.7 },
                Schuss4 = new Shot { Ringe = 6.7 },
                Schuss5 = new Shot { Ringe = 5.1 }
            });

            teilnehmer.Add(user2);

            // Bind dataGridTeilnehmer to Teilnehmer
            dataGridTeilnehmer.ItemsSource = teilnehmer;
        }




    }
}
