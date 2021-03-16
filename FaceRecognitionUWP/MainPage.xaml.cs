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
using Windows.Storage.Streams;
using System.Threading.Tasks;

using Windows.UI.Xaml.Shapes;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using System.Threading;
using Windows.UI.Core;
using Windows.Media.MediaProperties;
using System.Numerics;

namespace FaceRecognitionUWP
{
   
    public sealed partial class MainPage : Page
    {
        #region Properties
        // Onnx Model for face detection
        private RfbModel rfbModelGen;
        private RfbInput rfbInput;
        private RfbOutput rfbOutput;

        // Onnx Model for facial landmarks
        private LandmarkModel landmarkModelGen;
        private LandmarkInput landmarkInput;
        private LandmarkOutput landmarkOutput;

        // UI Display
        private const int imageDisplayMaxWidth = 440;
        private const int imageDisplayMaxHeight = 330;
        private int imageOriginalWidth;
        private int imageOriginalHeight;
        List<FaceLandmarks> faceLandmarksList;

        // Image Capturing & Processing
        SoftwareBitmap imageInputData; // Image data for model input
        OpenCVHelper openCVHelper; // Wrapper of OpenCV functions in C++
        DrawingFace drawingFace; // Class to handle drawing of rectangles and landmarks

        // Camera Capturing
        MediaCapture mediaCapture;
        MediaFrameReader mediaFrameReader;
        private SoftwareBitmap backBuffer;
        private bool taskRunning;

        // Mode Control
        bool CameraMode; // Or image mode: load local images
        bool ShowDetail; // Display facial landmarks and distance
        Vector2 cameraFocalLength;
        float closestDistance;
        #endregion

        #region Initialization
        public MainPage()
        {
            this.InitializeComponent();
            Setup();
        }

        /// <summary>
        /// Set up basic properties.
        /// </summary>
        private void Setup()
        {
            rfbInput = new RfbInput();
            landmarkInput = new LandmarkInput();

            cameraFocalLength = new Vector2(330.0f, 330.0f);
            closestDistance = 10000.0f;
            CameraMode = false;
            ShowDetail = true;
            taskRunning = false;

            openCVHelper = new OpenCVBridge.OpenCVHelper();
            drawingFace = new DrawingFace(imageDisplayMaxWidth,imageDisplayMaxHeight);

            imageOriginalWidth = 1;
            imageOriginalHeight = 1;

            imageInputData = new SoftwareBitmap(BitmapPixelFormat.Bgra8, FaceDetectionHelper.inputImageDataWidth, FaceDetectionHelper.inputImageDataHeight, BitmapAlphaMode.Premultiplied);
            
            recognizeButton.Visibility = Visibility.Collapsed;

            faceLandmarksList = new List<FaceLandmarks>();

            LoadFaceDetectionModelAsync();
            LoadFaceLandmarkModelAsync();
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
        #endregion 

        #region Button Control
        /// <summary>
        /// Button Control: Pick an image from the local storage.
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
                        recognizeButton.Visibility = Visibility.Visible;
                        ClearPreviousFaceRectangles();
                    }
                }
            }
        }

        /// <summary>
        /// Button Control: Load the picked image and preprocessed it as the model input.
        /// </summary>
        private void RecognizeButton_Click(object sender, RoutedEventArgs e)
        {
            DetectFaces();
        }

        /// <summary>
        /// Button Control: Toggle between Image Mode and Camera Mode.
        /// </summary>
        private async void ToggleModeButton_Click(object sender, RoutedEventArgs e)
        {
            if (CameraMode)
            {
                await mediaFrameReader.StopAsync();
                mediaFrameReader.FrameArrived -= ColorFrameReader_FrameArrived;
                mediaCapture.Dispose();
                mediaCapture = null;
                detectionModeText.Text = "Mode: Image";
                selectButton.Visibility = Visibility.Visible;
                recognizeButton.Visibility = Visibility.Collapsed;
                CameraMode = false;
            }
            else
            {
                InitializeCamera();
                detectionModeText.Text = "Mode: Camera";
                selectButton.Visibility = Visibility.Collapsed;
                recognizeButton.Visibility = Visibility.Collapsed;
                CameraMode = true;
            }
        }

        private void ToggleDistanceButton_Click(object sender, RoutedEventArgs e)
        {
            ShowDetail = !ShowDetail;
            DetectFaces();
        }
        #endregion 

        /// <summary>
        /// Load the picked image and preprocessed it as the model input.
        /// The function should excute after the image is loaded.
        /// </summary>
        private async void DetectFaces()
        {
            // Detect face using Onnx models
            rfbInput.input = FaceDetectionHelper.SoftwareBitmapToTensorFloat(imageInputData);
            rfbOutput = await rfbModelGen.EvaluateAsync(rfbInput);
            List<FaceDetectionRectangle> faceRects = (List<FaceDetectionRectangle>)FaceDetectionHelper.Predict(rfbOutput.scores, rfbOutput.boxes);

            // Detect facial landmarks using Onnx models
            if (ShowDetail)
            {
                closestDistance = 10000.0f;
                faceLandmarksList.Clear();

                foreach (FaceDetectionRectangle faceRect in faceRects)
                {
                    int rectX = (int)faceRect.X1;
                    int rectY = (int)faceRect.Y1;
                    int rectWidth = (int)(faceRect.X2 - faceRect.X1) + 1;
                    int rectHeight = (int)(faceRect.Y2 - faceRect.Y1) + 1;

                    // Crop only the image region with faces
                    SoftwareBitmap croppedBitmap = new SoftwareBitmap(
                        imageInputData.BitmapPixelFormat,
                        FaceLandmarkHelper.inputImageDataSize,
                        FaceLandmarkHelper.inputImageDataSize,
                        BitmapAlphaMode.Ignore);
                    bool cropped = openCVHelper.CropResize(imageInputData, croppedBitmap, rectX, rectY, rectWidth, rectHeight);
                    if (!cropped)
                        continue;

                    // Model Processing
                    landmarkInput.input = FaceDetectionHelper.SoftwareBitmapToTensorFloat(croppedBitmap);
                    landmarkOutput = await landmarkModelGen.EvaluateAsync(landmarkInput);
                    FaceLandmarks faceLandmarks =
                        (FaceLandmarks)FaceLandmarkHelper.Predict(landmarkOutput.output, rectX, rectY, rectWidth, rectHeight);

                    // Calculate camera distance
                    if (faceLandmarks.IsValid)
                    {
                        float distance = ImageHelper.CalculateCameraDistance(cameraFocalLength, faceLandmarks.EyeDistance);
                        closestDistance = distance < closestDistance ? distance : closestDistance;
                        faceLandmarksList.Add(faceLandmarks);
                    }
                }
                closestDistance = closestDistance == 10000.0f ? 0.0f : closestDistance;
                detailText.Text = $"Distance: {(int)closestDistance} cm";
            }
            else
            {
                detailText.Text = "";
            }
            
            // Draw rectangles or facial landmarks of detected faces on top of image
            ClearPreviousFaceRectangles();

            if (ShowDetail)
                drawingFace.DrawFaceAll(faceRects, faceLandmarksList);
            else
                drawingFace.DrawFaceRetangles(faceRects);

            foreach (Path path in drawingFace.pathes)
                imageGrid.Children.Add(path);
        }

        #region Utils
        /// <summary>
        /// Clear previous rectangles of detected faces
        /// </summary>
        private void ClearPreviousFaceRectangles()
        {
            if (drawingFace.HasPath)
            {
                foreach (var path in drawingFace.pathes)
                {
                    imageGrid.Children.Remove(path);
                }
                drawingFace.Clear();
            }
        }

        /// <summary>
        /// Update the image data used for face detection with the lastest image input.
        /// Input image comes from local storage or video frame.
        /// </summary>
        private SoftwareBitmap UpdateImageInputData(SoftwareBitmap rawImage)
        {
            SoftwareBitmap inputBitmap = SoftwareBitmap.Convert(rawImage, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            imageOriginalWidth = inputBitmap.PixelWidth;
            imageOriginalHeight = inputBitmap.PixelHeight;

            // Calculate Scaling Ratio
            drawingFace.UpdateDimensions(
                imageOriginalWidth,
                imageOriginalHeight,
                (float)FaceDetectionHelper.inputImageDataWidth,
                (float)FaceDetectionHelper.inputImageDataHeight);

            imageInputData = new SoftwareBitmap(BitmapPixelFormat.Bgra8, FaceDetectionHelper.inputImageDataWidth, FaceDetectionHelper.inputImageDataHeight, BitmapAlphaMode.Premultiplied);
            openCVHelper.Resize(inputBitmap, imageInputData);
            return inputBitmap;
        }
        #endregion

        #region Video Capture
        /// <summary>
        /// Video Capture: Initialize Camera Capture.
        /// Implementation is from the UWP official tutorial.
        /// https://docs.microsoft.com/en-us/windows/uwp/audio-video-camera/process-media-frames-with-mediaframereader
        /// </summary>
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

        /// <summary>
        /// Video Capture: Get camera frame and feed as model input.
        /// Implementation is from the UWP official tutorial.
        /// https://docs.microsoft.com/en-us/windows/uwp/audio-video-camera/process-media-frames-with-mediaframereader
        /// </summary>
        private void ColorFrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            var mediaFrameReference = sender.TryAcquireLatestFrame();
            var videoMediaFrame = mediaFrameReference?.VideoMediaFrame;
            if(videoMediaFrame != null)
            {
                if(videoMediaFrame.CameraIntrinsics != null)
                {
                    cameraFocalLength = videoMediaFrame.CameraIntrinsics.FocalLength;
                    System.Diagnostics.Debug.WriteLine("FocalLength: " + cameraFocalLength.X + " " + cameraFocalLength.Y);
                }
                    
            }
            var softwareBitmap = videoMediaFrame?.SoftwareBitmap;

            if (softwareBitmap != null)
            {
                if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 ||
                    softwareBitmap.BitmapAlphaMode != BitmapAlphaMode.Premultiplied)
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
                            var img = new SoftwareBitmapSource();
                            await img.SetBitmapAsync(latestBitmap);
                            inputImage.Source = img;
                            // Detect face and facial landmarks
                            UpdateImageInputData(latestBitmap);
                            DetectFaces();
                            latestBitmap.Dispose();
                        }

                        taskRunning = false;
                    });
            }

            mediaFrameReference?.Dispose();
        }
        #endregion
    }
}
