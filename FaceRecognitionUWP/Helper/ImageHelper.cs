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


        

        /*        public static async Task<SoftwareBitmap> ResizeImage(SoftwareBitmapSource imageSource, int maxWidth, int maxHeight)
                {
                    BitmapImage sourceImage = new BitmapImage();
                    sourceImage.(imageSource);
                    var origHeight = sourceImage.PixelHeight;
                    var origWidth = sourceImage.PixelWidth;
                    var ratioX = maxWidth / (float)origWidth;
                    var ratioY = maxHeight / (float)origHeight;
                    var ratio = Math.Min(ratioX, ratioY);
                    var newHeight = (int)(origHeight * ratio);
                    var newWidth = (int)(origWidth * ratio);

                    sourceImage.DecodePixelWidth = newWidth;
                    sourceImage.DecodePixelHeight = newHeight;

                    return sourceImage;
                }*/

    }

    public class Matrix<T>
    {
        public static T[,] TransposeMatrix(T[,] matrix)
        {
            var rows = matrix.GetLength(0);
            var columns = matrix.GetLength(1);

            var result = new T[columns, rows];

            for (var c = 0; c < columns; c++)
            {
                for (var r = 0; r < rows; r++)
                {
                    result[c, r] = matrix[r, c];
                }
            }

            return result;
        }
    }
}
