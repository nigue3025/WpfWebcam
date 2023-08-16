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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Expression.Encoder.Devices;
using Microsoft.Expression.Encoder.Live;
using Microsoft.Expression.Encoder;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Drawing;
using Newtonsoft.Json;
using System.Net;
using System.IO;
using System.Collections.Concurrent;
namespace GetWebcam
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        ConcurrentQueue<FrameData> frames = new ConcurrentQueue<FrameData>();
        LiveJob job;
        LiveDeviceSource liveDeviceSource;
        public Collection<EncoderDevice> VideoDevices { get; set; }
        public Collection<EncoderDevice> AudioDevices { get; set; }
        public MainWindow()
        {
            InitializeComponent();




        }
        bool IsCaptureContiously=true;
        async void captureImageContinuously()
        {
            string url = txtbx_url.Text;
            while(IsCaptureContiously)
            {
                {
                    Bitmap bitmp = new Bitmap(WebcamPanel.Width, WebcamPanel.Height);
                    {
                        using (Graphics graphics = Graphics.FromImage(bitmp))
                        {
                            System.Drawing.Point pnt = WebcamPanel.PointToScreen(new System.Drawing.Point(WebcamPanel.ClientRectangle.X, WebcamPanel.ClientRectangle.Y));
                            graphics.CopyFromScreen(pnt, System.Drawing.Point.Empty, new System.Drawing.Size(WebcamPanel.Width, WebcamPanel.Height));
                        }
                        rslt = await sendAsync(txtbx_url.Text, bitmp);
                        txtblck_msg.Text = rslt;
                        plotBoundingBox(rslt, bitmp);
                    }
                    bitmp.Dispose();
                    System.Threading.SpinWait.SpinUntil(() => false, 1);
                }
            }
            Image_Out.Source = null;

        }

        async Task<string> sendAsync(string url, System.Drawing.Image img, string backEnd="retinaface")
        {
            return await Task<string>.Run(() =>
            {
                return sendToJudge(url, img, backEnd);
            });

        }
        Bitmap curBitmap;
        void plotBoundingBox(string jsonStr, System.Drawing.Bitmap bitmap, int fontsize = 12)
        {
            curBitmap = (Bitmap)bitmap.Clone();
            var jsonObj = JsonConvert.DeserializeObject<Dictionary<string, List<DetectedLogo>>>(jsonStr);
            var results = jsonObj["results"];
            int count = 0;
            foreach (var result in results)
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    var scale = result.scale;
                    System.Drawing.Pen pen = new System.Drawing.Pen(System.Drawing.Brushes.Navy, 6);
                    Font drawFont = new Font("Arial", fontsize, System.Drawing.FontStyle.Bold);
                    SolidBrush drawBrush = new SolidBrush(pen.Color);
                    var box = result.box.Select(a => ((int)(scale * (double)a))).ToArray();
                    g.DrawRectangle(pen, new System.Drawing.Rectangle(box[0], box[1], box[2], box[3]));
                    g.DrawString($"no.{++count}\r\n{result.class_name}", drawFont, drawBrush, box[0], box[1]);
                }
            }
            Image_Out.Source = BitmapConverter.Bitmap2BitmapImage(bitmap);

        }

        string sendToJudge(string url, System.Drawing.Image img, string backEnd)
        {

            string result = "";
            Dictionary<string, string> data_send = new Dictionary<string, string>();

            var webAddr = url;
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
            httpWebRequest.ContentType = "application/json; charset=utf-8";
            httpWebRequest.Accept = "application/json";
            httpWebRequest.Method = "POST";


            byte[] arr;
            using (System.IO.Stream ms = new System.IO.MemoryStream())
            {
                img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                arr = (ms as System.IO.MemoryStream).ToArray();
            }
            var b64str = "data:image/jpg;base64," + Convert.ToBase64String(arr);
            data_send["img_path"] = b64str;
            data_send["detector_backend"] = backEnd;
            data_send["enforce_detection"] = "False";
            using (var streamWriter = new System.IO.StreamWriter(httpWebRequest.GetRequestStream()))
            {
                string json = JsonConvert.SerializeObject(data_send);
                streamWriter.Write(json);
                //streamWriter.Flush();
            }
            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new System.IO.StreamReader(httpResponse.GetResponseStream()))
                result = streamReader.ReadToEnd();
            return result;


        }


       

        async Task<BitmapImage> downloadImageAsync(string url)
        {
            BitmapImage bimage = new BitmapImage();
            using (var client = new WebClient())
            {
                var bytes = await client.DownloadDataTaskAsync(url);
                bimage.BeginInit();
                bimage.CacheOption = BitmapCacheOption.OnLoad;
                bimage.StreamSource = new MemoryStream(bytes);
                bimage.EndInit();
            }
            return bimage;
        }
        string rslt;
        private async void btn_capture_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Bitmap bitmp = new Bitmap(WebcamPanel.Width, WebcamPanel.Height);
                {
                    using (Graphics graphics = Graphics.FromImage(bitmp))
                    {
                        System.Drawing.Point pnt = WebcamPanel.PointToScreen(new System.Drawing.Point(WebcamPanel.ClientRectangle.X, WebcamPanel.ClientRectangle.Y));
                        graphics.CopyFromScreen(pnt, System.Drawing.Point.Empty, new System.Drawing.Size(WebcamPanel.Width, WebcamPanel.Height));
                    }
                    rslt= await sendAsync(txtbx_url.Text, bitmp);

                    bitmp.Save("C:\\WebcamSnapshots_test\\temp.jpg");
                    txtblck_msg.Text = rslt;
                    plotBoundingBox(rslt, bitmp);
                }
                bitmp.Dispose();
            }
            catch (Microsoft.Expression.Encoder.SystemErrorException ex)
            {
                MessageBox.Show("Device is in use by another application");
            }
            catch(Exception ex)
            {
                txtblck_msg.Text = ex.ToString();
            }
        }

        private void btn_preview_Click(object sender, RoutedEventArgs e)
        {   
            try
            {
                liveDeviceSource.PreviewWindow = new PreviewWindow(new HandleRef(WebcamPanel, WebcamPanel.Handle));
                job.ActivateSource(liveDeviceSource);
            }
            catch(Exception ex)
            {
                txtblck_msg.Text = ex.ToString();
            }
        }

        private void btn_init_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                job = new LiveJob();
                liveDeviceSource = job.AddDeviceSource(VideoDevices[cmbx_devices.SelectedIndex], null);
                // Get the properties of the device video
                SourceProperties sp = liveDeviceSource.SourcePropertiesSnapshot();
                job.OutputFormat.VideoProfile.Size = new System.Drawing.Size(sp.Size.Width, sp.Size.Height);
                txtblck_msg.Text = "init complete";
            }
            catch(Exception ex)
            {
                txtblck_msg.Text = ex.ToString();
            }
        }

        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                VideoDevices = EncoderDevices.FindDevices(EncoderDeviceType.Video);
                //AudioDevices = EncoderDevices.FindDevices(EncoderDeviceType.Audio);

                cmbx_devices.Items.Clear();
                foreach (var dvc in VideoDevices)
                    cmbx_devices.Items.Add(dvc.Name);
                if (cmbx_devices.Items.Count > 0)
                    cmbx_devices.SelectedIndex = 0;

            }
            catch (Exception ex)
            {
                txtblck_msg.Text = ex.ToString();
            }
        }

        private void btn_init_Click_1(object sender, RoutedEventArgs e)
        {
         
          
        }

        private void btn_startRecording_Click(object sender, RoutedEventArgs e)
        {
            FileArchivePublishFormat fileOut = new FileArchivePublishFormat();

            fileOut.OutputFileName = String.Format("C:\\WebCam{0:yyyyMMdd_hhmmss}.avi", DateTime.Now);
            job.PublishFormats.Add(fileOut);

            job.StartEncoding();

            btn_startRecording.IsEnabled = false;
            btn_stopRecording.IsEnabled = true;

        }

        private void btn_stopRecording_Click(object sender, RoutedEventArgs e)
        {
            job.StopEncoding();
            btn_startRecording.IsEnabled = true;
            btn_stopRecording.IsEnabled = false;
        }

        private void btn_inference_Click(object sender, RoutedEventArgs e)
        {
            IsCaptureContiously = true;
            captureImageContinuously();
           
        }

        private void btn_closeCamera_Click(object sender, RoutedEventArgs e)
        {
            if (job != null)
            {
                job.StopEncoding();
                job.RemoveDeviceSource(liveDeviceSource);
                liveDeviceSource.PreviewWindow = null;
                liveDeviceSource = null;
                IsCaptureContiously = false;
                
            }
        }
    }
}
