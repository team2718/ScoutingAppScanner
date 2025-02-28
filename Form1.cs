using AForge.Video;
using AForge.Video.DirectShow;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using ZXing;

namespace CameraFeedApp
{
    public partial class MainForm : Form
    {
        private FilterInfoCollection videoDevices; // List of available video devices
        private VideoCaptureDevice videoSource;    // Current video source
        private CancellationTokenSource cancellationTokenSource;

        public bool Scanned = false;
        string currentQRCodeString;
        JObject currentQRCodeJson = null;
        string deviceMoniker = "";

        Dictionary<string, JObject> all_the_reports = new Dictionary<string, JObject>();

        String scoutingDataDir;
        String reportsFileDir;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.Text = "Scouting App Scanner";

            scoutingDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ScoutingData");
            reportsFileDir = Path.Combine(scoutingDataDir, "scouting_reports_db.json");

            // Read all_the_reports from a file
            if (File.Exists(reportsFileDir))
            {
                string json_text = File.ReadAllText(reportsFileDir);
                all_the_reports = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, JObject>>(json_text);
            }
            else
            {
                Directory.CreateDirectory(scoutingDataDir);
                File.Create(reportsFileDir);
            }

            numReportMenuItem.Text = "Num Reports: " + all_the_reports.Count.ToString();

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

            if (videoDevices.Count > 0)
            {
                // Add all devices to combobox
                foreach (FilterInfo device in videoDevices)
                {
                    comboBox1.Items.Add(device.Name);
                }

                comboBox1.SelectedIndex = 0;
            }
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap frame = null;
            try
            {
                // Clone the frame to avoid accessing the original object directly
                frame = (Bitmap)eventArgs.Frame.Clone();

                pictureBox1.Image = new Bitmap(frame, pictureBox1.Width, pictureBox1.Height);

                // Decode QR code in the frame
                var qrCodeData = DecodeQRCodeFromBitmap(frame);

                // If QR code data is found, handle it
                if (!string.IsNullOrEmpty(qrCodeData))
                {
                    Invoke(new Action(() =>
                    {
                        try
                        {
                            currentQRCodeString = qrCodeData;
                            currentQRCodeJson = JObject.Parse(qrCodeData);
                            TextScannedText.Text = "Match " + currentQRCodeJson.SelectToken("$.matchNumber") + " Team " + currentQRCodeJson.SelectToken("$.teamNumber");

                            if (!all_the_reports.ContainsKey(currentQRCodeJson.SelectToken("$.uid").ToString()))
                            {
                                all_the_reports[currentQRCodeJson.SelectToken("$.uid").ToString()] = currentQRCodeJson;
                                numReportMenuItem.Text = "Num Reports: " + all_the_reports.Count.ToString();
                                writeReportsToFile();
                            }

                            String cutUpData = qrCodeData.Replace(',', '\n');
                            TextScanned.Text = cutUpData;
                            TextScanned.Visible = true;
                            TextScannedText.Visible = true;
                            Scanned = true;
                        }
                        catch (Exception ex)
                        {
                            //Console.WriteLine($"Error processing QR code: {ex.Message}");
                        }
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
                if (frame != null)
                    frame?.Dispose();
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

        private void writeReportsToFile()
        {
            // Write all_the_reports to a file
            string json_text = Newtonsoft.Json.JsonConvert.SerializeObject(all_the_reports);
            File.WriteAllText(reportsFileDir, json_text);
        }

        private void closeSafely()
        {
            writeReportsToFile();

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

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            closeSafely();
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

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Set the video source to the selected camera
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                videoSource.WaitForStop();
                videoSource = null;
            }

            deviceMoniker = videoDevices[comboBox1.SelectedIndex].MonikerString;

            videoSource = new VideoCaptureDevice(deviceMoniker);
            videoSource.NewFrame += VideoSource_NewFrame;
            double aspect_ratio = videoSource.VideoCapabilities[0].FrameSize.Height / (double)videoSource.VideoCapabilities[0].FrameSize.Width;
            pictureBox1.Height = (int)(pictureBox1.Width * aspect_ratio);
            videoSource.Start();
        }

        private void saveAsCSVToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!Scanned)
            {
                MessageBox.Show("No QR code scanned yet.");
                return;
            }

            JToken teamNumberJToken = currentQRCodeJson.SelectToken("$.teamNumber");
            String teamNumberString = teamNumberJToken.ToString();

            JToken matchNumberJToken = currentQRCodeJson.SelectToken("$.matchNumber");
            String matchNumberString = matchNumberJToken.ToString();

            string fileName = $"Team({teamNumberString}).csv";
            String Documents = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            Directory.CreateDirectory(Path.Combine(Documents, "ScoutingData"));
            string ScoutingDataDir = Path.Combine(Documents, "ScoutingData");
            Directory.CreateDirectory(Path.Combine(ScoutingDataDir, "CSV-Files"));
            string CSVDir = Path.Combine(ScoutingDataDir, "CSV-Files");
            string CSVFileSaved = Path.Combine(Path.Combine(CSVDir, fileName));

            String CSVed = currentQRCodeString.Replace(",", "\n").Replace("{", "").Replace("}", "").Replace("\"", "").Replace(":", ", ");
            CSVed = "Type, Value\n" + CSVed;


            if (File.Exists(CSVFileSaved))
            {
                string[] connectingValue = currentQRCodeString.Replace("{", "").Replace("}", "").Replace("\"", "").Split(',');
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

        private void saveAsJSONToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!Scanned)
            {
                MessageBox.Show("No QR code scanned yet.");
                return;
            }

            String Documents = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

            JToken teamNumberJToken = currentQRCodeJson.SelectToken("$.teamNumber");
            String teamNumberString = teamNumberJToken.ToString();
            Console.WriteLine(teamNumberString);

            JToken matchNumberJToken = currentQRCodeJson.SelectToken("$.matchNumber");
            String matchNumberString = matchNumberJToken.ToString();
            Console.WriteLine(matchNumberString);

            string fileName = $"Team({teamNumberString})Match({matchNumberString})Text.json";
            Directory.CreateDirectory(Path.Combine(Documents, "ScoutingData"));
            string ScoutingDataDir = Path.Combine(Documents, "ScoutingData");
            Directory.CreateDirectory(Path.Combine(ScoutingDataDir, "Json-Files"));
            string JsonDir = Path.Combine(ScoutingDataDir, "Json-Files");
            string textFileSaved = Path.Combine(Path.Combine(JsonDir, fileName));
            File.WriteAllText(textFileSaved, currentQRCodeString);

            Console.WriteLine(textFileSaved);

            File.WriteAllText(textFileSaved, currentQRCodeString);

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

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            closeSafely();
        }

        private void clearAllReportsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            writeReportsToFile();

            // Backup reports file with timestamp
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string backupFileName = $"scouting_reports_db_{timestamp}.json";
            string backupFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ScoutingData", backupFileName);
            File.Copy(reportsFileDir, backupFilePath);

            all_the_reports.Clear();
            numReportMenuItem.Text = "Num Reports: " + all_the_reports.Count.ToString();
            writeReportsToFile();
        }

        private string ConvertJsonToCsv(Dictionary<string, JObject> reports)
        {
            // Gather all the keys of the first report and use as the header
            JObject firstReport = reports.Values.First();
            List<string> header = new List<string>();
            foreach (var key in firstReport)
            {
                if (key.Key == "stagesComplete" || key.Key == "uid")
                {
                    continue;
                }
                header.Add(key.Key);
            }

            // Write the header row
            string csvText = string.Join(",", header) + "\n";

            // Write each report as a row
            foreach (var report in reports)
            {
                List<string> row = new List<string>();
                foreach (var key in header)
                {
                    var value = report.Value.SelectToken(key);

                    // This is about to get real ugly partner

                    if (key == "alliance")
                    {
                        //< item > Red </ item >
                        //< item > Blue </ item >
                        string new_value = value.ToString();
                        switch ((int)value)
                        {
                            case 0: new_value = "Red"; break;
                            case 1: new_value = "Blue"; break;
                        }
                        row.Add(new_value);
                    }
                    else if (key == "startingPosition")
                    {
                        //< item > Processor Side </ item >
                        //< item > Middle </ item >
                        //< item > Other Side </ item >
                        string new_value = value.ToString();
                        switch ((int)value)
                        {
                            case 0: new_value = "Processor Side"; break;
                            case 1: new_value = "Middle"; break;
                            case 2: new_value = "Other Side"; break;
                        }
                        row.Add(new_value);
                    }
                    else if (key == "hangType")
                    {
                        //< item > None </ item >
                        //< item > Park </ item >
                        //< item > Shallow </ item >
                        //< item > Deep </ item >
                        //< item > Shallow Failed </ item >
                        //< item > Deep Failed </ item >
                        string new_value = value.ToString();
                        switch ((int)value)
                        {
                            case 0: new_value = "None"; break;
                            case 1: new_value = "Park"; break;
                            case 2: new_value = "Shallow"; break;
                            case 3: new_value = "Deep"; break;
                            case 4: new_value = "Shallow Failed"; break;
                            case 5: new_value = "Deep Failed"; break;
                        }
                        row.Add(new_value);
                    }
                    else if (key == "cardReceived")
                    {
                        //< item > None </ item >
                        //< item > Yellow </ item >
                        //< item > Red </ item >
                        string new_value = value.ToString();
                        switch ((int)value)
                        {
                            case 0: new_value = "None"; break;
                            case 1: new_value = "Yellow"; break;
                            case 2: new_value = "Red"; break;
                        }
                        row.Add(new_value);
                    }
                    else if (key != "stagesComplete" && key != "uid")
                    {
                        row.Add(value.ToString());
                    }
                }
                csvText += string.Join(",", row) + "\n";
            }

            return csvText;
        }

        private void convertToCSVToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Ask for a file to convert
            // Start at the folder where the reports are stored
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                InitialDirectory = Path.GetDirectoryName(reportsFileDir),
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string jsonFilePath = openFileDialog.FileName;
                string json = File.ReadAllText(jsonFilePath);
                try
                {
                    Dictionary<string, JObject> loaded_reports = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, JObject>>(json);

                    if (loaded_reports.Count == 0)
                    {
                        MessageBox.Show("No reports found in the JSON file.");
                        return;
                    }

                    // Convert the JSON to CSV
                    string csvText = ConvertJsonToCsv(loaded_reports);

                    // Save the CSV to a file
                    string csvFilePath = Path.ChangeExtension(jsonFilePath, ".csv");
                    File.WriteAllText(csvFilePath, csvText);

                    // Notify the user
                    MessageBox.Show($"Converted JSON to CSV and saved to {csvFilePath}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error converting JSON to CSV: {ex.Message}");
                }
            }
        }

        private void openReportsFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Open reports folder
            System.Diagnostics.Process.Start(scoutingDataDir);
        }
    }
}

