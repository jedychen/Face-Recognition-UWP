using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.AI.MachineLearning;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Media.Capture.Frames;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using System.Threading;
using Windows.UI.Core;
using Windows.Media.Core;
using System.Diagnostics;
using Windows.Media.Devices;
using Windows.Media.Audio;
using System.Numerics;

namespace FaceRecognitionUWP
{
    public class ImageHelper
    {
        public static async Task<SoftwareBitmap> GetImageAsync(IRandomAccessStream stream)
        {
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
            return await decoder.GetSoftwareBitmapAsync();
        }
        public static async Task<StorageFile> PickerImageAsync()
        {
            var imagePicker = new FileOpenPicker()
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };

            imagePicker.ViewMode = PickerViewMode.List;
            imagePicker.FileTypeFilter.Add(".jpg");
            imagePicker.FileTypeFilter.Add(".png");
            imagePicker.FileTypeFilter.Add(".bmp");
            imagePicker.FileTypeFilter.Add(".jpeg");
            var file = await imagePicker.PickSingleFileAsync();
            return file;
        }

        public static void ImageStrechedValues(int maxWidth, int maxHeight, int originalWidth, int originalHeight, ref int outputWidth, ref int outputHeight, ref int marginHorizontal, ref int marginVertical)
        {
            if (maxWidth < 0 || maxHeight < 0 || originalWidth <= 0 || originalHeight <= 0)
            {
                System.Diagnostics.Debug.WriteLine("ImageHelper::StretchRatio - Wrong Input");
                return;
            }
            
            float originalRatio = (float)originalWidth / (float)originalHeight;
            float maxRatio = (float)maxWidth / (float)maxHeight;
            
            if (originalRatio > maxRatio)
            {
                outputWidth = maxWidth;
                outputHeight = (int)((float)maxWidth / originalRatio);
                marginVertical = (int)Math.Ceiling((maxHeight - outputHeight) * 0.5);
                marginHorizontal = 0;
            }
            else
            {
                outputHeight = maxHeight;
                outputWidth = (int)(originalRatio * (float)maxHeight);
                marginHorizontal = (int)Math.Ceiling((maxWidth - outputWidth) * 0.5);
                marginVertical = 0;
            }
        }

        public static float CalculateCameraDistance(Vector2 focalLength, FaceLandmark leftEyePos, FaceLandmark rightEyePos)
        {
            float distance = 0.0f;
            float AVERAGE_EYE_DISTANCE = 6.0f; //cm
            float deltaX = Math.Abs(leftEyePos.X - rightEyePos.X);
            float deltaY = Math.Abs(leftEyePos.Y - rightEyePos.Y);
            float eyeDistance = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
            distance = (float)330.0f * (AVERAGE_EYE_DISTANCE / eyeDistance);
            return distance;
        }
    }
}
