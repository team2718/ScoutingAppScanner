using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using AForge.Video;
using AForge.Video.DirectShow;
using Newtonsoft.Json.Linq;
using ZXing;

namespace CameraFeedApp
{
    public partial class MainForm : Form
    {
        private FilterInfoCollection videoDevices; // List of available video devices
        private VideoCaptureDevice videoSource;    // Current video source
        private CancellationTokenSource cancellationTokenSource;

        public bool Scanned = false;
        string qrCodeData;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.Text = "Scouting App Scanner";
            // Get the list of available video devices
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (videoDevices.Count == 0)
            {
                MessageBox.Show("No video devices found.");
                return;
            }

            // (Optional) List devices in a dropdown or just pick the first device
            foreach (FilterInfo device in videoDevices)
            {
                Console.WriteLine(device.Name); // Output device names for debugging
            }

            Console.WriteLine($"video devices");
            if (videoDevices.Count > 0)
            {
                Console.WriteLine($"{videoDevices[0]} video devices");
                // Initialize the video source with the first camera
                videoSource = new VideoCaptureDevice(videoDevices[1].MonikerString);
                videoSource.NewFrame += VideoSource_NewFrame; // Attach frame event handler
                videoSource.Start(); // Start capturing
            }
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if (!Scanned)
            {
                Bitmap frame = null;

                try
                {
                    // Clone the frame to avoid accessing the original object directly
                    frame = (Bitmap)eventArgs.Frame.Clone();

                    // Display the cloned frame in the PictureBox (UI thread)
                    pictureBox1.Invoke(new Action(() =>
                    {
                        pictureBox1.Image?.Dispose(); // Dispose of the previous image
                        pictureBox1.Image = (Bitmap)frame.Clone(); // Clone again for display
                    }));

                    // Decode QR code in the frame
                    qrCodeData = DecodeQRCodeFromBitmap(frame);

                    // If QR code data is found, handle it
                    if (!string.IsNullOrEmpty(qrCodeData))
                    {
                        Invoke(new Action(() =>
                        {
                            Scanned = true;

                            if (pictureBox1 != null)
                            {
                                this.Controls.Remove(pictureBox1); // Remove from the form's controls
                                pictureBox1.Dispose(); // Dispose to free resources
                                pictureBox1 = null; // Set to null to avoid accessing it after removal
                            }
                            String cutUpData = qrCodeData.Replace(',', '\n');
                            String[] cutUpDataArray = SplitWithNewlines(cutUpData);
                            TextScanned.Text = CombineStrings(cutUpDataArray, 0, 21);
                            TextScanned2.Text = CombineStrings(cutUpDataArray, 20, cutUpDataArray.Length - 1);
                            TextScanned2.Visible = true;
                            TextScanned.Visible = true;
                            TextScannedText.Visible = true;
                            SaveToText.Visible = true;
                            SaveToCSV.Visible = true;
                        }));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing video frame: {ex.Message}");
                }
                finally
                {
                    // Dispose of the cloned frame to free resources
                    frame?.Dispose();
                }
            }
        }


        private void btnStop_Click(object sender, EventArgs e)
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                videoSource.WaitForStop();
                videoSource = null;
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Ensure the camera feed stops when the form closes
            if (videoSource != null && videoSource.IsRunning)
            {
                if (pictureBox1 != null && !pictureBox1.IsDisposed)
                {
                    this.Controls.Remove(pictureBox1); // Remove from the form's controls
                    pictureBox1.Dispose(); // Dispose to free resources
                    pictureBox1 = null; // Set to null to avoid accessing it after removal
                }

                videoSource.SignalToStop();
                videoSource.WaitForStop();
                videoSource = null;
            }

            cancellationTokenSource?.Cancel();

            Application.Exit();
        }

        public string DecodeQRCodeFromBitmap(Bitmap bitmap)
        {
            try
            {
                // Create a clone of the bitmap to avoid locked region issues
                using (var clonedBitmap = new Bitmap(bitmap))
                {
                    // Initialize the QR code reader
                    BarcodeReader reader = new BarcodeReader();

                    // Decode the QR code from the cloned bitmap
                    var result = reader.Decode(clonedBitmap);

                    // If a QR code is detected, return its data
                    return result?.Text; // Returns the decoded text or null if none found
                }
            }
            catch (Exception ex)
            {
                // Log or handle exceptions (optional)
                Console.WriteLine($"Error decoding QR code: {ex.Message}");
                return null;
            }
        }

        private void SaveToText_Click(object sender, EventArgs e)
        {
            String Documents = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

            JObject jsonObj = JObject.Parse(qrCodeData);
            JToken teamNumberJToken = jsonObj.SelectToken("$.teamNumber");
            String teamNumberString = teamNumberJToken.ToString();
            Console.WriteLine(teamNumberString);

            JToken matchNumberJToken = jsonObj.SelectToken("$.matchNumber");
            String matchNumberString = matchNumberJToken.ToString();
            Console.WriteLine(matchNumberString);

            string fileName = $"Team({teamNumberString})Match({matchNumberString})Text.json";
            Directory.CreateDirectory(Path.Combine(Documents, "ScoutingData"));
            string ScoutingDataDir = Path.Combine(Documents, "ScoutingData");
            Directory.CreateDirectory(Path.Combine(ScoutingDataDir, "Json-Files"));
            string JsonDir = Path.Combine(ScoutingDataDir, "Json-Files");
            string textFileSaved = Path.Combine(Path.Combine(JsonDir, fileName));
            File.WriteAllText(textFileSaved, qrCodeData);

            Console.WriteLine(textFileSaved);

            File.WriteAllText(textFileSaved, qrCodeData);

            NotifyIcon notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Information, // Set the icon
                Visible = true,
                BalloonTipTitle = "Saved to Json Document",
                BalloonTipText = "Data was saved to " + textFileSaved,
            };

            // Show the notification
            notifyIcon.ShowBalloonTip(3000); // Show for 3 seconds
            
        }

        string SafeFileName(string input)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                input = input.Replace(c.ToString(), ""); // Replace invalid characters with '_'
            }
            return input;
        }

        public static string CombineStrings(string[] inputArray, int startIndex, int endIndex)
        {
            // Validate the input array and indices
            if (inputArray == null || inputArray.Length == 0)
                throw new ArgumentException("Input array cannot be null or empty.");
            if (startIndex < 0 || endIndex >= inputArray.Length || startIndex > endIndex)
                throw new ArgumentOutOfRangeException("Invalid indices provided.");

            // Combine the strings within the range
            string result = "";
            for (int i = startIndex; i <= endIndex; i++)
            {
                result += inputArray[i];
            }
            return result;
        }

        public static string[] SplitWithNewlines(string input)
        {
            // Regular expression that splits by newline but retains it
            var regex = new Regex(@"([^\n]*\n?)");
            var matches = regex.Matches(input);

            // Convert matches to an array of strings
            string[] result = new string[matches.Count];
            for (int i = 0; i < matches.Count; i++)
            {
                result[i] = matches[i].Value;
            }

            return result;
        }

        private void SaveToCSV_Click(object sender, EventArgs e)
        {
            JObject jsonObj = JObject.Parse(qrCodeData);
            JToken teamNumberJToken = jsonObj.SelectToken("$.teamNumber");
            String teamNumberString = teamNumberJToken.ToString();

            JToken matchNumberJToken = jsonObj.SelectToken("$.matchNumber");
            String matchNumberString = matchNumberJToken.ToString();

            string fileName = $"Team({teamNumberString}).csv";
            String Documents = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            Directory.CreateDirectory(Path.Combine(Documents, "ScoutingData"));
            string ScoutingDataDir = Path.Combine(Documents, "ScoutingData");
            Directory.CreateDirectory(Path.Combine(ScoutingDataDir, "CSV-Files"));
            string CSVDir = Path.Combine(ScoutingDataDir, "CSV-Files");
            string CSVFileSaved = Path.Combine(Path.Combine(CSVDir, fileName));

            String CSVed = qrCodeData.Replace(",", "\n").Replace("{", "").Replace("}", "").Replace("\"", "").Replace(":", ", ");
            CSVed = "Type, Value\n" + CSVed;
            

            if (File.Exists(CSVFileSaved))
            {
                string[] connectingValue = qrCodeData.Replace("{", "").Replace("}", "").Replace("\"", "").Split(',');
                List<string> connectingValueSheet = new List<string>();
                connectingValueSheet.Add("Value");
                foreach (string line in connectingValue)
                {
                    connectingValueSheet.Add(line.Split(':')[1]);
                }
                string[] lines = File.ReadAllLines(CSVFileSaved);
                List<string> newSheet = new List<string>();
                for (int i = 0; i < connectingValueSheet.Count; i++)
                {
                    newSheet.Add(lines[i] + ", " + connectingValueSheet[i]);
                }
                Console.WriteLine(connectingValueSheet.Count);
                Console.WriteLine(lines.Length);
                
                foreach (string line in newSheet)
                {
                    Console.WriteLine(line);
                }
                File.WriteAllLines(CSVFileSaved, newSheet);
                
            }
            else 
            {
                File.WriteAllText(CSVFileSaved, CSVed);
            }

            NotifyIcon notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Information, // Set the icon
                Visible = true,
                BalloonTipTitle = "Saved to CSV Document",
                BalloonTipText = "Data was saved to " + CSVFileSaved,
            };

            // Show the notification
            notifyIcon.ShowBalloonTip(3000); // Show for 3 seconds
        }
    }
}

