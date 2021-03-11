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

        public MainPage()
        {
            this.InitializeComponent();

            /*// Set supported inking device types.
            inkCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse | Windows.UI.Core.CoreInputDeviceTypes.Pen | Windows.UI.Core.CoreInputDeviceTypes.Touch;
            inkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(
                new Windows.UI.Input.Inking.InkDrawingAttributes()
                {
                    Color = Windows.UI.Colors.White,
                    Size = new Size(22, 22),
                    IgnorePressure = true,
                    IgnoreTilt = true,
                }
            );*/
            LoadFaceModelAsync();
            }

            private async Task LoadModelAsync()
            {
                //Load a machine learning model
                StorageFile modelFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assets/mnist.onnx"));
                modelGen = await mnistModel.CreateFromStreamAsync(modelFile as IRandomAccessStreamReference);
            }

            private async Task LoadFaceModelAsync()
            {
                //Load a machine learning model
                StorageFile modelFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assets/version-RFB-320.onnx"));
                modelGen = await mnistModel.CreateFromStreamAsync(modelFile as IRandomAccessStreamReference);
            }


            private async void selectButton_Click(object sender, RoutedEventArgs e)
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
                        SoftwareBitmap outputBitmap = new SoftwareBitmap(softImg.BitmapPixelFormat, softImg.PixelWidth, softImg.PixelHeight, BitmapAlphaMode.Premultiplied);

                        var helper = new OpenCVBridge.OpenCVHelper();
                        helper.Blur(inputBitmap, outputBitmap);
                        var img = new SoftwareBitmapSource();
                            await img.SetBitmapAsync(outputBitmap);
                            inputImage.Source = img;
                        }
                    }
                }
            }

            private async void recognizeButton_Click(object sender, RoutedEventArgs e)
            {
                
                /*rfbInput.input = ImageFeatureValue.CreateFromVideoFrame(vf);

                //Evaluate the model
                mnistOutput = await modelGen.EvaluateAsync(mnistInput);

                //Convert output to datatype
                IReadOnlyList<float> vectorImage = mnistOutput.Plus214_Output_0.GetAsVectorView();
                IList<float> imageList = vectorImage.ToList();

                //LINQ query to check for highest probability digit
                var maxIndex = imageList.IndexOf(imageList.Max());

                //Display the results
                numberLabel.Text = maxIndex.ToString();*/
            }

            async void recognizeButton2_Click()
            {
                //Bind model input with contents from InkCanvas
                VideoFrame vf = await helper.GetHandWrittenImage(inkGrid);
                mnistInput.Input3 = ImageFeatureValue.CreateFromVideoFrame(vf);

                //Evaluate the model
                mnistOutput = await modelGen.EvaluateAsync(mnistInput);

                //Convert output to datatype
                IReadOnlyList<float> vectorImage = mnistOutput.Plus214_Output_0.GetAsVectorView();
                IList<float> imageList = vectorImage.ToList();

                //LINQ query to check for highest probability digit
                var maxIndex = imageList.IndexOf(imageList.Max());

                //Display the results
                numberLabel.Text = maxIndex.ToString();
            }

            

            private void clearButton_Click(object sender, RoutedEventArgs e)
            {
                // inkCanvas.InkPresenter.StrokeContainer.Clear();
                // numberLabel.Text = "";
            }
    }
}
