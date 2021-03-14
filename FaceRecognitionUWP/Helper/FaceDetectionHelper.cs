using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics.Tensors;
using System.Drawing;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.AI.MachineLearning;
using System.Runtime.InteropServices;

namespace FaceRecognitionUWP
{
    /// <summary>Component for accessing pixels in SoftwareBitmap.
    /// Reference: https://docs.microsoft.com/en-us/windows/uwp/audio-video-camera/imaging#create-or-edit-a-softwarebitmap-programmatically
    /// </summary>
    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }

    /// <summary>Class <c>FaceDetectionHelper</c> groups all functions for Face Detection.
    /// </summary>
    public class FaceDetectionHelper
    {
        public const int inputImageWidth = 320;
        public const int inputImageHeight = 240;

        /// <summary>
        /// PreProcessing.
        /// Convert image data in SoftwareBitmap to TensorFloat.
        /// </summary>
        /// <returns>Concerted SoftwareBitmap in TensorFloat.</returns>
        public static TensorFloat SoftwareBitmapToTensorFloat(SoftwareBitmap image)
        {
            using (BitmapBuffer buffer = image.LockBuffer(BitmapBufferAccessMode.Read))
            {
                using (var reference = buffer.CreateReference())
                {
                    // Implementation Reference:
                    // https://github.com/Microsoft/Windows-Machine-Learning/issues/22
                    unsafe
                    {
                        ((IMemoryBufferByteAccess)reference).GetBuffer(out byte* dataInBytes, out uint capacity);

                        // System.Diagnostics.Debug.WriteLine(capacity);
                        long[] shape = { 1, 3, inputImageHeight, inputImageWidth };
                        float[] pCPUTensor = new float[3 * inputImageWidth * inputImageHeight];
                        for (int i = 0; i < capacity; i += 4)
                        {
                            int pixelInd = i / 4;
                            pCPUTensor[pixelInd] = (float)dataInBytes[i];
                            pCPUTensor[(inputImageHeight * inputImageWidth) + pixelInd] = (float)dataInBytes[i + 1];
                            pCPUTensor[(inputImageHeight * inputImageWidth * 2) + pixelInd] = (float)dataInBytes[i + 2];
                        }
                        

                        /*for (int i = 0; i < 30; i++)
                        {
                            System.Diagnostics.Debug.WriteLine(pCPUTensor[i]);
                        }
                        System.Diagnostics.Debug.WriteLine("Processed");
                        */
                        
                        float[] processedTensor = NormalizeFloatArray(pCPUTensor);

                        for (int i = 0; i < 30; i++)
                        {
                            System.Diagnostics.Debug.WriteLine(processedTensor[i]);
                        }

                        TensorFloat tensorFloats = TensorFloat.CreateFromArray(shape, processedTensor);
                        return tensorFloats;
                    }

                }
            }

        }

        /// <summary>
        /// PreProcessing.
        /// Normalize the image data in float array.
        /// </summary>
        /// <returns>Float array with values between -1 to 1.</returns>
        private static float[] NormalizeFloatArray(float[] src)
        {
            const float _meanVals = 127f;
            const float _normVals = (float)(1.0 / 128);
            var normalized = new float[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                normalized[i] = (src[i] - _meanVals) * _normVals;
            }

            return normalized;
        }
    }
}
