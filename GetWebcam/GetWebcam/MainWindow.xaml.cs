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

namespace GetWebcam
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        LiveJob job;
        LiveDeviceSource liveDeviceSource;
        public Collection<EncoderDevice> VideoDevices { get; set; }
        public Collection<EncoderDevice> AudioDevices { get; set; }
        public MainWindow()
        {
            InitializeComponent();



            VideoDevices = EncoderDevices.FindDevices(EncoderDeviceType.Video);
            AudioDevices = EncoderDevices.FindDevices(EncoderDeviceType.Audio);
            job = new LiveJob();
            liveDeviceSource = job.AddDeviceSource(VideoDevices[0], null);
            // Get the properties of the device video
            SourceProperties sp = liveDeviceSource.SourcePropertiesSnapshot();

            // Resize the preview panel to match the video device resolution set
            WebcamPanel.Size = new System.Drawing.Size(sp.Size.Width, sp.Size.Height);


            // Setup the output video resolution file as the preview
            job.OutputFormat.VideoProfile.Size = new System.Drawing.Size(sp.Size.Width, sp.Size.Height);


        }

        private void btn_capture_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using(Bitmap bitmp=new Bitmap(WebcamPanel.Width, WebcamPanel.Height))
                {

                    using (Graphics graphics = Graphics.FromImage(bitmp))
                    {
                        System.Drawing.Point pnt = WebcamPanel.PointToScreen(new System.Drawing.Point(WebcamPanel.ClientRectangle.X, WebcamPanel.ClientRectangle.Y));
                        graphics.CopyFromScreen(pnt, System.Drawing.Point.Empty, new System.Drawing.Size(WebcamPanel.Width, WebcamPanel.Height));
                    }
                    bitmp.Save("C:\\WebcamSnapshots_test\\temp.jpg");
                }

            }
            catch (Microsoft.Expression.Encoder.SystemErrorException ex)
            {
                MessageBox.Show("Device is in use by another application");
            }
        }

        private void btn_preview_Click(object sender, RoutedEventArgs e)
        {
            liveDeviceSource.PreviewWindow = new PreviewWindow(new HandleRef(WebcamPanel, WebcamPanel.Handle));
            job.ActivateSource(liveDeviceSource);
        }

        private void btn_init_Click(object sender, RoutedEventArgs e)
        {

        }

        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
