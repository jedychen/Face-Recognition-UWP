using OpenCVBridge;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
#if USE_WINML_NUGET
using Microsoft.AI.MachineLearning;
#else
using Windows.AI.MachineLearning;
#endif
using Windows.Media;
using Windows.Storage.Streams;
using System.Threading.Tasks;

using System.Runtime.InteropServices;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Media;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Devices.Enumeration;
using System.Threading;
using Windows.UI.Core;
using Windows.Media.Core;
using System.Diagnostics;
using Windows.Media.Devices;
using Windows.Media.Audio;
using Windows.Media.MediaProperties;

namespace FaceRecognitionUWP
{
   
    public sealed partial class MainPage : Page
    {
        // Onnx Model for face detection
        private RfbModel rfbModelGen;
        private RfbInput rfbInput = new RfbInput();
        private RfbOutput rfbOutput;

        // Onnx Model for face landmarks
        private LandmarkModel landmarkModelGen;
        private LandmarkInput landmarkInput = new LandmarkInput();
        private LandmarkOutput landmarkOutput;

        // UI Display
        private const int imageDisplayMaxWidth = 440;
        private const int imageDisplayMaxHeight = 330;
        private int imageOriginalWidth = 1;
        private int imageOriginalHeight = 1;
        private List<Path> facePathes = new List<Path>();

        private Path faceLandmarkPath = new Path();
        private Path faceRectanglePath = new Path();

        // Image Capturing & Processing
        SoftwareBitmap imageInputData;
        OpenCVHelper openCVHelper = new OpenCVBridge.OpenCVHelper();

        // Camera Capturing
        MediaCapture mediaCapture;
        MediaFrameReader mediaFrameReader;
        private SoftwareBitmap backBuffer;
        private bool taskRunning = false;

        // Mode Control
        bool CameraMode = false;

        public MainPage()
        {
            this.InitializeComponent();
            InitImage();
            LoadFaceDetectionModelAsync();
            LoadFaceLandmarkModelAsync();
        }

        private async void InitImage()
        {
            imageInputData = new SoftwareBitmap(BitmapPixelFormat.Bgra8, FaceDetectionHelper.inputImageWidth, FaceDetectionHelper.inputImageHeight, BitmapAlphaMode.Premultiplied);
            var img = new SoftwareBitmapSource();
            await img.SetBitmapAsync(imageInputData);
            inputImage.Source = img;

            faceLandmarkPath.Fill = new SolidColorBrush(Windows.UI.Colors.Green);
            faceRectanglePath.Stroke = new SolidColorBrush(Windows.UI.Colors.Red);
            faceRectanglePath.StrokeThickness = 1;
        }

        private async Task LoadFaceDetectionModelAsync()
        {
            //Load a machine learning model
            StorageFile modelFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assets/Models/version-RFB-320.onnx"));
            rfbModelGen = await RfbModel.CreateFromStreamAsync(modelFile as IRandomAccessStreamReference);
        }

        private async Task LoadFaceLandmarkModelAsync()
        {
            //Load a machine learning model
            StorageFile modelFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assets/Models/landmark_detection_56_se_external.onnx"));
            landmarkModelGen = await LandmarkModel.CreateFromStreamAsync(modelFile as IRandomAccessStreamReference);
        }


        /// <summary>
        /// Pick an image from the local storage.
        /// </summary>
        private async void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            var file = await ImageHelper.PickerImageAsync();
            if (file != null)
            {
                using (var fs = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                {

                    using (var tempStream = fs.CloneStream())
                    {
                        SoftwareBitmap softImg;
                        softImg = await ImageHelper.GetImageAsync(tempStream);
                        var img = new SoftwareBitmapSource();
                        await img.SetBitmapAsync(UpdateImageInputData(softImg));
                        inputImage.Source = img;

                        ClearPreviousFaceRectangles();
                    }
                }
            }
        }

        private SoftwareBitmap UpdateImageInputData(SoftwareBitmap rawImage)
        {
            SoftwareBitmap inputBitmap = SoftwareBitmap.Convert(rawImage, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            imageOriginalWidth = inputBitmap.PixelWidth;
            imageOriginalHeight = inputBitmap.PixelHeight;

            imageInputData = new SoftwareBitmap(BitmapPixelFormat.Bgra8, FaceDetectionHelper.inputImageWidth, FaceDetectionHelper.inputImageHeight, BitmapAlphaMode.Premultiplied);
            openCVHelper.Resize(inputBitmap, imageInputData);
            return inputBitmap;
        }

        /// <summary>
        /// Load the picked image and preprocessed it as the model input.
        /// The function should excute after the image is loaded.
        /// </summary>
        private void RecognizeButton_Click(object sender, RoutedEventArgs e)
        {
            DetectFaces();
        }
            /// <summary>
            /// Load the picked image and preprocessed it as the model input.
            /// The function should excute after the image is loaded.
            /// </summary>
        private async void DetectFaces()
        {
            
            // Detect face using CNN models
            rfbInput.input = FaceDetectionHelper.SoftwareBitmapToTensorFloat(imageInputData);
            rfbOutput = await rfbModelGen.EvaluateAsync(rfbInput);
            List<FaceDetectionInfo> faceRects = (List<FaceDetectionInfo>)FaceDetectionHelper.Predict(rfbOutput.scores, rfbOutput.boxes);


            // Calculate Scaling Ratio
            int outputWidth = 1;
            int outputHeight = 1;
            int marginHorizontal = 1;
            int marginVertical = 1;

            ImageHelper.ImageStrechedValues(
                imageDisplayMaxWidth,
                imageDisplayMaxHeight,
                imageOriginalWidth,
                imageOriginalHeight,
                ref outputWidth,
                ref outputHeight,
                ref marginHorizontal,
                ref marginVertical);
            float scaleRatioWidth = (float)outputWidth / (float)FaceDetectionHelper.inputImageWidth;
            float scaleRatioHeight = (float)outputHeight / (float)FaceDetectionHelper.inputImageHeight;


            // Find face landmarks and store it in UI
            int landmarkInputSize = 56;
            
            var faceLandmarkGeometryGroup = new GeometryGroup();
            foreach (FaceDetectionInfo faceRect in faceRects)
            {
                int x = (int)faceRect.X1;
                int y = (int)faceRect.Y1;
                int width = (int)(faceRect.X2 - faceRect.X1) + 1;
                int height = (int)(faceRect.Y2 - faceRect.Y1) + 1;
                SoftwareBitmap croppedBitmap = new SoftwareBitmap(imageInputData.BitmapPixelFormat, landmarkInputSize, landmarkInputSize, BitmapAlphaMode.Ignore);
                openCVHelper.CropResize(imageInputData, croppedBitmap, x, y, width, height, landmarkInputSize, landmarkInputSize);
                landmarkInput.input = FaceDetectionHelper.SoftwareBitmapToTensorFloat(croppedBitmap);
                landmarkOutput = await landmarkModelGen.EvaluateAsync(landmarkInput);
                List<FaceLandmark> faceLandmarks = (List<FaceLandmark>)FaceLandmarkHelper.Predict(landmarkOutput.output, x, y, width, height);
                foreach (FaceLandmark mark in faceLandmarks)
                {
                    var ellipse = new EllipseGeometry();

                    ellipse.Center = new Point(
                        (int)(mark.X * scaleRatioWidth + marginHorizontal),
                        (int)(mark.Y * scaleRatioHeight + marginVertical));
                    ellipse.RadiusX = 2;
                    ellipse.RadiusY = 2;
                    faceLandmarkGeometryGroup.Children.Add(ellipse);
                }
            }
            
            
            // Draw rectangles of detected faces on top of image
            var faceGeometryGroup = new GeometryGroup();
            foreach (FaceDetectionInfo face in faceRects)
            { 
                var rectangle = new RectangleGeometry();
                
                rectangle.Rect = new Rect(
                    (int)(face.X1 * scaleRatioWidth + marginHorizontal),
                    (int)(face.Y1 * scaleRatioHeight + marginVertical),
                    (int)(face.X2 - face.X1 + 1) * scaleRatioWidth,
                    (int)(face.Y2 - face.Y1 + 1) * scaleRatioHeight) ;
                faceGeometryGroup.Children.Add(rectangle);
            }

            faceLandmarkPath.Data = faceLandmarkGeometryGroup;
            faceRectanglePath.Data = faceGeometryGroup;
            ClearPreviousFaceRectangles();
            facePathes.Add(faceLandmarkPath);
            facePathes.Add(faceRectanglePath);
            imageGrid.Children.Add(faceRectanglePath);
            imageGrid.Children.Add(faceLandmarkPath);
        }

        private async void ToggleModeButton_Click(object sender, RoutedEventArgs e)
        {
            if(CameraMode)
            {
                await mediaFrameReader.StopAsync();
                mediaFrameReader.FrameArrived -= ColorFrameReader_FrameArrived;
                mediaCapture.Dispose();
                mediaCapture = null;
                detectionModeText.Text = "Mode: < Image >";
                CameraMode = false;
            } else
            {
                InitializeCamera();
                detectionModeText.Text = "Mode: < Camera >";
                CameraMode = true;
            }
        }

        private void ToggleDistanceButton_Click(object sender, RoutedEventArgs e)
        {
            // inkCanvas.InkPresenter.StrokeContainer.Clear();
            // numberLabel.Text = "";
        }

        /// <summary>
        /// Clear previous rectangles of detected faces
        /// </summary>
        private void ClearPreviousFaceRectangles()
        {
            if (facePathes.Count > 0)
            {
                foreach (var path in facePathes)
                {
                    imageGrid.Children.Remove(path);
                    
                }
                facePathes.Clear();
            }
        }

        public async void InitializeCamera()
        {
            var frameSourceGroups = await MediaFrameSourceGroup.FindAllAsync();
            var selectedGroupObjects = frameSourceGroups.Select(group =>
            new
            {
                sourceGroup = group,
                colorSourceInfo = group.SourceInfos.FirstOrDefault((sourceInfo) =>
                {
                    // On Xbox/Kinect, omit the MediaStreamType and EnclosureLocation tests
                    return sourceInfo.SourceKind == MediaFrameSourceKind.Color;

                })

            }).Where(t => t.colorSourceInfo != null)
            .FirstOrDefault();

            MediaFrameSourceGroup selectedGroup = selectedGroupObjects?.sourceGroup;
            MediaFrameSourceInfo colorSourceInfo = selectedGroupObjects?.colorSourceInfo;

            if (selectedGroup == null)
            {
                return;
            }

            mediaCapture = new MediaCapture();

            var settings = new MediaCaptureInitializationSettings()
            {
                SourceGroup = selectedGroup,
                SharingMode = MediaCaptureSharingMode.ExclusiveControl,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu,
                StreamingCaptureMode = StreamingCaptureMode.Video
            };
            try
            {
                await mediaCapture.InitializeAsync(settings);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("MediaCapture initialization failed: " + ex.Message);
                return;
            }

            var colorFrameSource = mediaCapture.FrameSources[colorSourceInfo.Id];

            mediaFrameReader = await mediaCapture.CreateFrameReaderAsync(colorFrameSource, MediaEncodingSubtypes.Argb32);
            mediaFrameReader.FrameArrived += ColorFrameReader_FrameArrived;
            await mediaFrameReader.StartAsync();
        }

        private void ColorFrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            var mediaFrameReference = sender.TryAcquireLatestFrame();
            var videoMediaFrame = mediaFrameReference?.VideoMediaFrame;
            var softwareBitmap = videoMediaFrame?.SoftwareBitmap;

            if (softwareBitmap != null)
            {
                if (softwareBitmap.BitmapPixelFormat != Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8 ||
                    softwareBitmap.BitmapAlphaMode != Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied)
                {
                    softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                }

                // Swap the processed frame to _backBuffer and dispose of the unused image.
                softwareBitmap = Interlocked.Exchange(ref backBuffer, softwareBitmap);
                softwareBitmap?.Dispose();

                // Changes to XAML ImageElement must happen on UI thread through Dispatcher
                var task = inputImage.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    async () =>
                    {
                        // Don't let two copies of this task run at the same time.
                        if (taskRunning)
                        {
                            return;
                        }
                        taskRunning = true;

                        // Keep draining frames from the backbuffer until the backbuffer is empty.
                        SoftwareBitmap latestBitmap;
                        while ((latestBitmap = Interlocked.Exchange(ref backBuffer, null)) != null)
                        {
                            var imageSource = (SoftwareBitmapSource)inputImage.Source;
                            UpdateImageInputData(latestBitmap);
                            DetectFaces();
                            await imageSource.SetBitmapAsync(latestBitmap);
                            latestBitmap.Dispose();
                        }

                        taskRunning = false;
                    });
            }

            mediaFrameReference.Dispose();
        }
    }
}
