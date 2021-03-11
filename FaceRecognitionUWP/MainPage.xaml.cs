﻿using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;
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

        public MainPage()
        {
            this.InitializeComponent();

            // Set supported inking device types.
            inkCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse | Windows.UI.Core.CoreInputDeviceTypes.Pen | Windows.UI.Core.CoreInputDeviceTypes.Touch;
            inkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(
                new Windows.UI.Input.Inking.InkDrawingAttributes()
                {
                    Color = Windows.UI.Colors.White,
                    Size = new Size(22, 22),
                    IgnorePressure = true,
                    IgnoreTilt = true,
                }
            );
            LoadModelAsync();
            }

            private async Task LoadModelAsync()
            {
                //Load a machine learning model
                StorageFile modelFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assets/mnist.onnx"));
                modelGen = await mnistModel.CreateFromStreamAsync(modelFile as IRandomAccessStreamReference);
            }

            private async void recognizeButton_Click(object sender, RoutedEventArgs e)
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
                inkCanvas.InkPresenter.StrokeContainer.Clear();
                numberLabel.Text = "";
            }
    }
}
