using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
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
        private Thread connectThread;
        private Thread statusThread;
        //private Thread ComPortListUpdaterThread;
        private String[] availableCOMPorts;

        public MainWindow()
        {
            InitializeComponent();

            // Start the thread that is checking if the machine has 
            // set DSR and CTS.
            statusThread = new Thread(new ThreadStart(CheckDTRRTS));
            statusThread.IsBackground = true;
            statusThread.Start();

            // Start job that checks the availability of COM Ports 
            // and updates the dropdown menu
            //ComPortListUpdaterThread = new Thread(new ThreadStart(ComPortListUpdater));
            //ComPortListUpdaterThread.IsBackground = true;
            //ComPortListUpdaterThread.Start();

            // Add the available COM Ports to the dropdown menu.
            availableCOMPorts = SerialPort.GetPortNames();
            comboBoxCOMPorts.ItemsSource = availableCOMPorts;

            // If at least one COM Port is available set it as selected item.
            if (availableCOMPorts.Length > 0)
                comboBoxCOMPorts.SelectedItem = availableCOMPorts[0];

            createTestUser();
        }

        private void comboBoxCOMPorts_DropDownOpened(object sender, EventArgs e)
        {
            string tempSelection = comboBoxCOMPorts.Text;
            availableCOMPorts = SerialPort.GetPortNames();
            comboBoxCOMPorts.ItemsSource = availableCOMPorts;
            if (availableCOMPorts.Contains(tempSelection))
                comboBoxCOMPorts.SelectedItem = tempSelection;

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

                serialPort1.Open();
                // Add a data Receive listener to the connection.
                serialPort1.DataReceived += new SerialDataReceivedEventHandler(SerialPort1_DataReceived);
                
                // DTR must be set by the computer to make the machine receive commands
                serialPort1.DtrEnable = true;
                
                // Try to connect to machine
                connectThread = new Thread(new ThreadStart(changeToRemoteControl));
                connectThread.Start();

                ProgressBarConnection.IsIndeterminate = true;

                buttonConnect.IsEnabled = false;
                buttonDisconnect.IsEnabled = true;
                comboBoxCOMPorts.IsEnabled = false;


            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void changeToRemoteControl()
        {
            try
            {
                while (true)
                {
                    if (serialPort1.IsOpen)
                    {
                        if (serialPort1.CtsHolding || serialPort1.DsrHolding)
                        {
                            serialPort1.Write("V\r");
                            Dispatcher.Invoke(new ChangeConnectionStatusDelegate(ChangeConnectionStatus), "Maschine auf Fernsteuerung umschalten");
                        }
                        else
                        {
                            // Maschine nicht bereit
                            // Warten auf CTS DSR
                            Dispatcher.Invoke(new ChangeConnectionStatusDelegate(ChangeConnectionStatus), "Warten auf DSR/CTS");
                        }
                    }
                    else
                    {
                        // Serial Port not open
                        Dispatcher.Invoke(new ChangeConnectionStatusDelegate(ChangeConnectionStatus), "Serial Port not open");
                    }
                    Thread.Sleep(500);
                }
            }
            catch (ThreadInterruptedException)
            {
                return;
            }
        }

        private delegate void ChangeConnectionStatusDelegate(string status);
        private void ChangeConnectionStatus(string status)
        {
            if (!labelStatus.Content.Equals(status))
            {
                // Change status label.
                labelStatus.Content = status;
            }
        }


        private void CheckDTRRTS()
        {
            try
            {
                bool ctsHolding = false;
                bool dsrHolding = false;
                while (true)
                {
                    if (serialPort1.IsOpen)
                    {
                        if (ctsHolding != serialPort1.CtsHolding)
                        {
                            Dispatcher.Invoke(new UpdateCTSLabelDelegete(UpdateCTSLabel), serialPort1.CtsHolding);
                            ctsHolding = serialPort1.CtsHolding;
                            // Dispatcher.Invoke(new UpdateUiTextDelegate(WriteData), "CheckDTRRTS() - CTS" + ctsHolding);
                        }
                        if (dsrHolding != serialPort1.DsrHolding)
                        {
                            Dispatcher.Invoke(new UpdateDSRLabelDelegate(UpdateDSRLabel), serialPort1.DsrHolding);
                            dsrHolding = serialPort1.DsrHolding;
                            // Dispatcher.Invoke(new UpdateUiTextDelegate(WriteData), "CheckDTRRTS() - DSR" + dsrHolding);
                        }
                    }
                    // This is the case if the machine is shut of.
                    if (ctsHolding != false)
                    {
                        Dispatcher.Invoke(new UpdateCTSLabelDelegete(UpdateCTSLabel), false);
                        ctsHolding = false;
                    }
                    if (dsrHolding != false)
                    {
                        Dispatcher.Invoke(new UpdateDSRLabelDelegate(UpdateDSRLabel), false);
                        dsrHolding = false;
                    }
                    // Dispatcher.Invoke(new UpdateUiTextDelegate(WriteData), "CheckDTRRTS() - No Connection opened");
                    Thread.Sleep(200);
                }
            }
            catch (ThreadInterruptedException)
            {
                return;
            }
        }

        private delegate void UpdateCTSLabelDelegete(bool status);
        private delegate void UpdateDSRLabelDelegate(bool status);

        private void UpdateCTSLabel(bool status)
        {
            if (status)
                labelCTS.Content = "CTS on";
            else
                labelCTS.Content = "CTS off";
        }
        private void UpdateDSRLabel(bool status)
        {
            if (status)
                labelDSR.Content = "DSR on";
            else
                labelDSR.Content = "DSR off";
        }



        private void buttonDisconnect_Click(object sender, RoutedEventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                try
                {
                    // End the connect thread.
                    connectThread.Interrupt();
                    if (connectThread.IsAlive) 
                    {
                        connectThread.Join();
                    }

                    serialPort1.Close();

                    labelStatus.Content = "getrennt";
                    buttonDisconnect.IsEnabled = false;
                    buttonConnect.IsEnabled = true;
                    buttonSnedCommand.IsEnabled = false;
                    ProgressBarConnection.IsIndeterminate = false;
                    ProgressBarConnection.Value = 0;
                    comboBoxCOMPorts.IsEnabled = true;
                }
                catch (Exception err)
                {
                    MessageBox.Show(err.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }



        private void SerialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {

            //Read ASCI Code
            serialPort1.NewLine = "\r";
            // If not working change to 
            string dataIn = serialPort1.ReadLine();
            // string dataIn = serialPort1.ReadExisting();
            
            // Maschine sende "HKeineBerechtigung" nach dem V\r erfolgreich 
            // empfangen wurde.
            if (dataIn.Contains("Keine Berechtigung"))
                Dispatcher.Invoke(new ChangeConnectionStatusDelegate(ChangeConnectionStatus), "Auf Fernsteuerungsbestätigung warten");
            
            // Maschine sendet "F794" wenn man V\r sendet, die Maschine aber
            // bereits erfolgreich auf FErn umgestellt wurde.
            // Wenn "F974" empfangen wurde ist die Maschine erfolgreich umgestellt.
            if (dataIn.Contains("F794"))
                Dispatcher.Invoke(new MachineConnectedDelegate(MachineConnected));

            Dispatcher.Invoke(new UpdateUiTextDelegate(WriteData), dataIn);

            // Parse the input:
            // Dispatcher.Invoke(new ParseShotDelegate(ParseShot), dataIn);
        }

        private delegate void UpdateUiTextDelegate(string text);
        private delegate void ParseShotDelegate(string text);
        private delegate void SendACKDelegate();

        private delegate void MachineConnectedDelegate();


        private void MachineConnected()
        {
            if (connectThread.IsAlive)
            {
                connectThread.Interrupt();
                connectThread.Join();
            }
            ProgressBarConnection.IsIndeterminate = false;
            ProgressBarConnection.Value = 100;
            labelStatus.Content = "Machine verbunden";

            // Hier alle Buttons enablen die enabled sein soll, wenn die Machine verbunden ist.
            buttonSnedCommand.IsEnabled = true;
        }


        private void WriteData(string text)
        {
            textBoxReceivedData.Text += text + "\n";
            textBoxReceivedData.ScrollToEnd();
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
            SendData(textBoxBefehl.Text);
        }

        private void textBoxBefehl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendData(textBoxBefehl.Text);
            }
        }

        private void SendData(string text)
        {
            //Preprocess Data

            string command = text.Trim() + "\r";
            // Print command as dezimal values 
            byte[] buf = Encoding.ASCII.GetBytes(command);
            textBoxOutput.AppendText(BitConverter.ToString(buf) + "\n");
            //serialPort1.Write(buf, 0, buf.Length);

            if (!textBoxBefehl.Text.Equals(""))
            {
                if (serialPort1.IsOpen)
                {
                    DateTime startTime = DateTime.Now;
                    DateTime currentTime;
                    bool commandSend = false;
                    bool timeout = false;
                    while (!commandSend && !timeout)
                    {
                        if (serialPort1.DtrEnable)
                        {
                            //Befehl senden als string
                            serialPort1.Write(command);
                            // Befehl senden als Byte[]
                            //serialPort1.Write(buf, 0, buf.Length);
                            //Gesendeten Kommand in der Textbox anzeigen.
                            textBoxOutput.AppendText("Erfolgreich gesendet: " + command + "\n");
                            textBoxOutput.ScrollToEnd();
                            commandSend = true;
                        }
                        currentTime = DateTime.Now;
                        if (currentTime.Ticks - startTime.Ticks > 5000000)
                        {
                            textBoxOutput.AppendText("Timeout while sending: " + command + "\n");
                            textBoxOutput.ScrollToEnd();
                            timeout = true;
                        }
                    } 
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


        private void createTestUser()
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
