﻿using System;
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
            float AVERAGE_EYE_DISTANCE = 6.0f; //cm
            distance = (float)330.0f * (AVERAGE_EYE_DISTANCE / eyeDistance);
            return distance;
        }
    }
}
