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

namespace FaceRecognitionUWP
{
    public sealed partial class MainPage : Page
    {
        private RfbModel rfbModelGen;
        private RfbInput rfbInput = new RfbInput();
        private RfbOutput rfbOutput;
        SoftwareBitmap outputBitmap;

        private List<Path> faceRectangles = new List<Path>();

        public MainPage()
        {
            this.InitializeComponent();
            LoadFaceModelAsync();
        }

        private async Task LoadFaceModelAsync()
        {
            //Load a machine learning model
            StorageFile modelFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assets/version-RFB-320.onnx"));
            rfbModelGen = await RfbModel.CreateFromStreamAsync(modelFile as IRandomAccessStreamReference);
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
                        outputBitmap = new SoftwareBitmap(softImg.BitmapPixelFormat, FaceDetectionHelper.inputImageWidth, FaceDetectionHelper.inputImageHeight, BitmapAlphaMode.Ignore);

                        var helper = new OpenCVBridge.OpenCVHelper();
                        helper.Resize(inputBitmap, outputBitmap);
                        var img = new SoftwareBitmapSource();
                        await img.SetBitmapAsync(outputBitmap);
                        inputImage.Source = img;
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
            if (faceRectangles.Count > 0)
            {
                foreach (var rect in faceRectangles)
                {
                    imageGrid.Children.Remove(rect);
                }
            }
            
            rfbInput.input = FaceDetectionHelper.SoftwareBitmapToTensorFloat(outputBitmap);
            rfbOutput = await rfbModelGen.EvaluateAsync(rfbInput);

            List<FaceDetectionInfo> faceRects = (List<FaceDetectionInfo>)FaceDetectionHelper.Predict(rfbOutput.scores, rfbOutput.boxes);

            var path1 = new Path();
            path1.Stroke = new SolidColorBrush(Windows.UI.Colors.Red);
            path1.StrokeThickness = 3;
            var geometryGroup1 = new GeometryGroup();
            foreach (FaceDetectionInfo face in faceRects)
            { 
                var rectangle = new RectangleGeometry();
                float resizeRatio = 1.0f;
                rectangle.Rect = new Rect(
                    (int)(face.X1 * resizeRatio),
                    (int)(face.Y1 * resizeRatio),
                    (int)(face.X2 - face.X1) * resizeRatio,
                    (int)(face.Y2 - face.Y1) * resizeRatio);
                geometryGroup1.Children.Add(rectangle);
            }
            faceRectangles.Add(path1);
            path1.Data = geometryGroup1;
            imageGrid.Children.Add(path1);
            /*
        //Convert output to datatype
        IReadOnlyList<float> vectorImage = mnistOutput.Plus214_Output_0.GetAsVectorView();
        IList<float> imageList = vectorImage.ToList();

        //LINQ query to check for highest probability digit
        var maxIndex = imageList.IndexOf(imageList.Max());

        //Display the results
        numberLabel.Text = maxIndex.ToString();*/
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            // inkCanvas.InkPresenter.StrokeContainer.Clear();
            // numberLabel.Text = "";
        }
    }
}
