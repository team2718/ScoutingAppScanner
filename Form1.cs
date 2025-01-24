using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
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
                            Console.WriteLine($"{qrCodeData}");
                            if (pictureBox1 != null)
                            {
                                this.Controls.Remove(pictureBox1); // Remove from the form's controls
                                pictureBox1.Dispose(); // Dispose to free resources
                                pictureBox1 = null; // Set to null to avoid accessing it after removal
                            }
                            TextScanned.Text = qrCodeData;
                            TextScanned.Visible = true;
                            TextScannedText.Visible = true;
                            SaveToText.Visible = true;
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
            string[] partsTeamNum = qrCodeData.Split('\n');
            string[] parts2TeamNum = partsTeamNum[0].Split(':');
            string teamNumber = parts2TeamNum[1].Replace(" ", "");

            string[] partsMatchNum = qrCodeData.Split('\n');
            string[] parts2MatchNum = partsMatchNum[1].Split(':');
            string matchNumber = parts2MatchNum[1].Replace(" ", "");

            teamNumber = SafeFileName(teamNumber);
            matchNumber = SafeFileName(matchNumber);

            string fileName = $"Team({teamNumber})Match({matchNumber})Text.txt";
            Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ScoutingData"));
            string textFileSaved = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ScoutingData"), fileName);

            Console.WriteLine(textFileSaved);

            File.WriteAllText(textFileSaved, qrCodeData);

            NotifyIcon notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Information, // Set the icon
                Visible = true,
                BalloonTipTitle = "Saved to Text Document",
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
    }
}

