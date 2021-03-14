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

namespace FaceRecognitionUWP
{
    public sealed partial class MainPage : Page
    {
        private mnistModel modelGen;
        private mnistInput mnistInput = new mnistInput();
        private mnistOutput mnistOutput;
        //private LearningModelSession    session;
        private Helper helper = new Helper();
        RenderTargetBitmap renderBitmap = new RenderTargetBitmap();


        private RfbModel rfbModelGen;
        private RfbInput rfbInput = new RfbInput();
        private RfbOutput rfbOutput;
        SoftwareBitmap outputBitmap;

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
            rfbInput.input = FaceDetectionHelper.SoftwareBitmapToTensorFloat(outputBitmap);
            rfbOutput = await rfbModelGen.EvaluateAsync(rfbInput);

            System.Diagnostics.Debug.WriteLine(rfbOutput.scores);
            System.Diagnostics.Debug.WriteLine(rfbOutput.boxes);
            
            //confidences, boxes = ort_session.run(None, { input_name: image})
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
