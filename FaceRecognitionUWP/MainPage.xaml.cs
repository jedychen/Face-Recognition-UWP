﻿using OpenCVBridge;
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

        SoftwareBitmap outputBitmap;
        private List<Path> facePathes = new List<Path>();

        private const int imageDisplayMaxWidth = 440;
        private const int imageDisplayMaxHeight = 330;
        private int imageOriginalWidth = 1;
        private int imageOriginalHeight = 1;

        OpenCVHelper openCVHelper = new OpenCVBridge.OpenCVHelper();

        public MainPage()
        {
            this.InitializeComponent();
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

        /// <summary>
        /// Pick an image from the local storage.
        /// </summary>
        private async void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            var file = await ImageHelper.PickerImageAsync();
            if (file != null)
            {
                using (var fs = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
                {

                    using (var tempStream = fs.CloneStream())
                    {
                        SoftwareBitmap softImg;
                        softImg = await ImageHelper.GetImageAsync(tempStream);

                        SoftwareBitmap inputBitmap = SoftwareBitmap.Convert(softImg, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore);
                        imageOriginalWidth = inputBitmap.PixelWidth;
                        imageOriginalHeight = inputBitmap.PixelHeight;
                        outputBitmap = new SoftwareBitmap(softImg.BitmapPixelFormat, FaceDetectionHelper.inputImageWidth, FaceDetectionHelper.inputImageHeight, BitmapAlphaMode.Ignore);

                        openCVHelper.Resize(inputBitmap, outputBitmap);
                        var img = new SoftwareBitmapSource();
                        await img.SetBitmapAsync(inputBitmap);
                        inputImage.Source = img;

                        ClearPreviousFaceRectangles();
                    }
                }
            }
        }

        /// <summary>
        /// Load the picked image and preprocessed it as the model input.
        /// The function should excute after the image is loaded.
        /// </summary>
        private async void RecognizeButton_Click(object sender, RoutedEventArgs e)
        {

            ClearPreviousFaceRectangles();

            // Detect face using CNN models
            rfbInput.input = FaceDetectionHelper.SoftwareBitmapToTensorFloat(outputBitmap);
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

            var faceLandmarkPath = new Path();
            faceLandmarkPath.Fill = new SolidColorBrush(Windows.UI.Colors.Green);
            var faceLandmarkGeometryGroup = new GeometryGroup();
            foreach (FaceDetectionInfo faceRect in faceRects)
            {
                int x = (int)faceRect.X1;
                int y = (int)faceRect.Y1;
                int width = (int)(faceRect.X2 - faceRect.X1) + 1;
                int height = (int)(faceRect.Y2 - faceRect.Y1) + 1;
                SoftwareBitmap croppedBitmap = new SoftwareBitmap(outputBitmap.BitmapPixelFormat, landmarkInputSize, landmarkInputSize, BitmapAlphaMode.Ignore);
                openCVHelper.CropResize(outputBitmap, croppedBitmap, x, y, width, height, landmarkInputSize, landmarkInputSize);
                landmarkInput.input = FaceDetectionHelper.SoftwareBitmapToTensorFloat(croppedBitmap);
                landmarkOutput = await landmarkModelGen.EvaluateAsync(landmarkInput);
                List<FaceLandmark> faceLandmarks = (List<FaceLandmark>)FaceLandmarkHelper.Predict(landmarkOutput.output, x, y, width, height);
                foreach (FaceLandmark mark in faceLandmarks)
                {
                    var ellipse = new EllipseGeometry();

                    ellipse.Center = new Point(
                        (int)(mark.X * scaleRatioWidth + marginHorizontal),
                        (int)(mark.Y * scaleRatioHeight + marginVertical));
                    ellipse.RadiusX = 1;
                    ellipse.RadiusY = 1;
                    faceLandmarkGeometryGroup.Children.Add(ellipse);
                }
            }
            facePathes.Add(faceLandmarkPath);
            faceLandmarkPath.Data = faceLandmarkGeometryGroup;
            // Draw rectangles of detected faces on top of image

            var facePath = new Path();
            facePath.Stroke = new SolidColorBrush(Windows.UI.Colors.Red);
            facePath.StrokeThickness = 1;
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

            facePathes.Add(facePath);
            facePath.Data = faceGeometryGroup;
            imageGrid.Children.Add(facePath);
            imageGrid.Children.Add(faceLandmarkPath);
        }

        private void ToggleModeButton_Click(object sender, RoutedEventArgs e)
        {
            // inkCanvas.InkPresenter.StrokeContainer.Clear();
            // numberLabel.Text = "";
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
                foreach (var rect in facePathes)
                {
                    imageGrid.Children.Remove(rect);
                }
            }
        }
    }
}
