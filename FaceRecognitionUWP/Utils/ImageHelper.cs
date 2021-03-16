using System;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using System.Numerics;

namespace FaceRecognitionUWP
{
    /// <summary>Class <c>ImageHelper</c> handles image related tasks.
    /// </summary>
    public class ImageHelper
    {
        /// <summary>
        /// Gets image streams.
        /// </summary>
        public static async Task<SoftwareBitmap> GetImageAsync(IRandomAccessStream stream)
        {
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
            return await decoder.GetSoftwareBitmapAsync();
        }

        /// <summary>
        /// Opens image picker and allows uses to choose image file.
        /// </summary>
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

        /// <summary>
        /// Caculates eye distance to the camera based on camera intrinsics.
        /// </summary>
        public static float CalculateCameraDistance(Vector2 focalLength, float eyeDistance)
        {
            float distance = 0.0f;
            float AVERAGE_EYE_DISTANCE = 6.2f; //cm
            distance = (float)focalLength.X * (AVERAGE_EYE_DISTANCE / eyeDistance);
            return distance;
        }
    }
}
