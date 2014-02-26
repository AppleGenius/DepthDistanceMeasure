#region using...
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
#endregion

namespace DepthDistanceMeasure
{
    public partial class MainWindow : Window
    {
        private KinectSensor kinect;
        private WriteableBitmap depthImageBitMap;
        private Int32Rect depthImageBitmapRect;
        private Int32 depthImageStride;
        private DepthImageFrame lastDepthFrame;
        private short[] depthPixelDate;

        public KinectSensor Kinnect
        {
            get { return kinect; }
            set
            {
                if (kinect != null)
                {
                    UninitializeKinectSensor(this.kinect);
                    kinect = null;
                }
                if (value != null && value.Status == KinectStatus.Connected)
                {
                    kinect = value;
                    InitializeKinectSensor(this.kinect);
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += (s, e) => DiscoverKinectSensor();
            this.Unloaded += (s, e) => this.kinect = null;
        }

        private void DiscoverKinectSensor()
        {
            KinectSensor.KinectSensors.StatusChanged += new EventHandler<StatusChangedEventArgs>(KinectSensors_StatusChanged);
            this.Kinnect = KinectSensor.KinectSensors.FirstOrDefault(sensor => sensor.Status == KinectStatus.Connected);
        }

        void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case KinectStatus.Connected:
                    if (this.kinect == null)
                        this.kinect = e.Sensor;
                    break;
                case KinectStatus.Disconnected:
                    if (this.kinect == e.Sensor)
                    {
                        this.kinect = null;
                        this.kinect = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);
                        if (this.kinect == null)
                        {
                        }
                    }
                    break;

            }
        }

        private void InitializeKinectSensor(KinectSensor kinectSensor)
        {
            if (kinectSensor != null)
            {
                DepthImageStream depthStream = kinectSensor.DepthStream;
                depthStream.Enable();

                depthImageBitMap = new WriteableBitmap(depthStream.FrameWidth, depthStream.FrameHeight, 96, 96, PixelFormats.Gray16, null);
                depthImageBitmapRect = new Int32Rect(0, 0, depthStream.FrameWidth, depthStream.FrameHeight);
                depthImageStride = depthStream.FrameWidth * depthStream.FrameBytesPerPixel;

                DepthImage.Source = depthImageBitMap;
                kinectSensor.DepthFrameReady += new EventHandler<DepthImageFrameReadyEventArgs>(kinectSensor_DepthFrameReady);
                kinectSensor.Start();
            }
        }

        private void UninitializeKinectSensor(KinectSensor kinect)
        {
            if (kinect != null)
            {
                kinect.Stop();
                kinect.DepthFrameReady -= new EventHandler<DepthImageFrameReadyEventArgs>(kinectSensor_DepthFrameReady);
            }
        }

        void kinectSensor_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            if (lastDepthFrame != null)
            {
                lastDepthFrame.Dispose();
                lastDepthFrame = null;
            }
            lastDepthFrame = e.OpenDepthImageFrame();
            if (lastDepthFrame != null)
            {
                depthPixelDate = new short[lastDepthFrame.PixelDataLength];
                lastDepthFrame.CopyPixelDataTo(depthPixelDate);
                depthImageBitMap.WritePixels(depthImageBitmapRect, depthPixelDate, depthImageStride, 0);
            }
        }

        private void DepthImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(DepthImage);
            if (depthPixelDate != null && depthPixelDate.Length > 0)
            {
                Int32 pixelIndex = (Int32)(p.X + ((Int32)p.Y * this.lastDepthFrame.Width));
                Int32 depth = this.depthPixelDate[pixelIndex] >> DepthImageFrame.PlayerIndexBitmaskWidth;
                PixelDepth.Text = String.Format("{0}mm", depth);
            }
        }
    }
}
