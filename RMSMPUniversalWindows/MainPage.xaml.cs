using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;
using Windows.Devices.Enumeration;
using Microsoft.Azure.Devices.Client;
using System.Text;
using Newtonsoft.Json;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RMSMPUniversalWindows
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // local variables
        private SerialDevice serialPort = null;
        DataWriter dataWriteObject = null;
        DataReader dataReaderObject = null;
        //private string arduinoSerialName = "Arduino";
        private string arduinoSerialName = "USB Serial";
        private CancellationTokenSource ReadCancellationTokenSource;
        List<DataPoints> dataPointsList = new List<DataPoints>();

        public MainPage()
        {
            this.InitializeComponent();        
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Start.IsEnabled = false;
                await ConnectToArduino();
                Listen();
            }
            catch (Exception ex)
            {
                string s = ex.Message;
            }


        }


        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            Start.IsEnabled = false;
            Stop.IsEnabled = true;
            await ConnectToArduino();
            Listen();
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            Start.IsEnabled = true;
            Stop.IsEnabled = false;
            CancelReadTask();
            connection.Text = "Connection Cancelled";

        }

        /// <summary>
        /// Connect application to arduino
        /// </summary>
        /// <returns></returns>
        private async Task ConnectToArduino()
        {
            List<DeviceInformation> listOfDevices = new List<DeviceInformation>();

            try
            {
                string deviceSelector = SerialDevice.GetDeviceSelector();
                var devices = await DeviceInformation.FindAllAsync(deviceSelector);
                for (int i = 0; i < devices.Count; i++)
                {
                    listOfDevices.Add(devices[i]);
                }
                // update device id
                string deviceID = listOfDevices.Where(x => x.Name.Contains(arduinoSerialName)).Select(x => x.Id).FirstOrDefault();

                // get serial port and confirm on screen
                serialPort = await SerialDevice.FromIdAsync(deviceID);


                if (serialPort == null)
                {
                    connection.Text = "Problem connecting to serial.";
                    return;
                }
                else
                {
                    connection.Text = "Successfully connected to serial.";
                }
 
                // Configure serial settings
                serialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                serialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                serialPort.BaudRate = 9600;
                serialPort.Parity = SerialParity.None;
                serialPort.StopBits = SerialStopBitCount.One;
                serialPort.DataBits = 8;
                serialPort.Handshake = SerialHandshake.None;

                // Create cancellation token object to close I/O operations when closing the device
                ReadCancellationTokenSource = new CancellationTokenSource();
                return;
            }

            catch (Exception ex)
            {
                string error = ex.Message;
                return;
            }



        }

        private async void Listen()
        {
            try
            {
                if (serialPort != null)
                {
                    dataReaderObject = new DataReader(serialPort.InputStream);

                    // keep reading the serial input
                    while (true)
                    {
                        await ReadAsync(ReadCancellationTokenSource.Token);
                    }
                }
            }
            catch (TaskCanceledException tce)
            {
                CloseDevice();
                connection.Text = "Reading task was cancelled, closing device and cleaning up. Error: " + tce.Message;
            }
            catch (Exception ex)
            {
                connection.Text = ex.Message;
            }
            finally
            {
                // Cleanup once complete
                if (dataReaderObject != null)
                {
                    dataReaderObject.DetachStream();
                    dataReaderObject = null;
                }
            }
        }

        private async Task ReadAsync(CancellationToken cancellationToken)
        {
            Task<UInt32> loadAsyncTask;

            uint ReadBufferLength = 4096;

            // If task cancellation was requested, comply
            cancellationToken.ThrowIfCancellationRequested();

            // Set InputStreamOptions to complete the asynchronous read operation when one or more bytes is available
            dataReaderObject.InputStreamOptions = InputStreamOptions.Partial;

            using (var childCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                // Create a task object to wait for data on the serialPort.InputStream
                loadAsyncTask = dataReaderObject.LoadAsync(ReadBufferLength).AsTask(childCancellationTokenSource.Token);

                // Launch the task and wait
                UInt32 bytesRead = await loadAsyncTask;
                if (bytesRead > 0)
                {
                    string message = dataReaderObject.ReadString(bytesRead);
                     
                    ProcessData(message);


                    //p.updateDataLog(message);
                    //dynamic d = JsonConvert.DeserializeObject(message);
                    //d.timeStamp = DateTime.Now;
                    //message = JsonConvert.SerializeObject(d);
                    ////SystemData data = JsonConvert.DeserializeObject<SystemData>(message);
                    ////data.timeStamp = DateTime.Now;
                    ////message = JsonConvert.SerializeObject(data);
                    //p.updateDataLog(message);
                    //CloudHelper.SendDeviceToCloudMessagesAsync(message);
                }
            }

        }

        private void CloseDevice()
        {
            if (serialPort != null)
            {
                serialPort.Dispose();
            }
            serialPort = null;


        }

        private void CancelReadTask()
        {
            if (ReadCancellationTokenSource != null)
            {
                if (!ReadCancellationTokenSource.IsCancellationRequested)
                {
                    ReadCancellationTokenSource.Cancel();
                }
            }
            if (serialPort != null)
            {
                serialPort.Dispose();
            }
            serialPort = null;

            
        }

        private void ProcessData(string message)
        {
            DataPoints dataPoints;
            try
            {
                dataPoints = JsonConvert.DeserializeObject<DataPoints>(message);
                data.Text = message;
            }
            catch(Exception e)
            {
                data.Text = e.Message;
                return;
            }

            //  deserialize data into object
            // compare time stamps - we want to update every x seconds
            // if the data list is empty, then we want to add to it
            if (dataPointsList.Count() > 0)
            {
                // 
                if (dataPointsList[dataPointsList.Count() - 1].groupingStamp > dataPoints.timeStamp.AddMilliseconds(-Settings.uploadInterval))
                {
                    dataPoints.groupingStamp = dataPointsList[dataPointsList.Count() - 1].groupingStamp;
                    dataPointsList.Add(dataPoints);
                }
                else
                {
                    // submit old data and start again
                    DataPoints p = new DataPoints(dataPointsList);
                    string payload = JsonConvert.SerializeObject(p);
                    n.Text = p.sourceCount.ToString();
                    x.Text = p.returnAirHumidity.ToString();
                    data.Text = payload;
                    SendDeviceToCloudMessagesAsync(payload);
                    //
                    dataPointsList.Clear();
                    dataPoints.groupingStamp = dataPoints.timeStamp;
                    dataPointsList.Add(dataPoints);
                }
            }
            else
            {
                dataPoints.groupingStamp = dataPoints.timeStamp;
                dataPointsList.Add(dataPoints);
            }

        }

        async void SendDeviceToCloudMessagesAsync(string msg)
        {

            var deviceClient = DeviceClient.Create(Settings.iotHubUri,
            AuthenticationMethodFactory.
            CreateAuthenticationWithRegistrySymmetricKey(Settings.deviceId, Settings.deviceKey),
            TransportType.Http1);
            var message = new Message(Encoding.ASCII.GetBytes(msg));
            await deviceClient.SendEventAsync(message);
        }
    }
}
