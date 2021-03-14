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
    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }
    public class FaceDetectionHelper
    {
        public const int inputImageWidth = 320;
        public const int inputImageHeight = 240;
        public static TensorFloat ConvertImageToFloatTensor(SoftwareBitmap image)
        {
            using (BitmapBuffer buffer = image.LockBuffer(BitmapBufferAccessMode.Read))
            {
                using (var reference = buffer.CreateReference())
                {
                    unsafe
                    {
                        byte* dataInBytes;
                        uint capacity;
                        ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacity);

                        System.Diagnostics.Debug.WriteLine(capacity);
                        // Fill-in the BGRA plane
                        BitmapPlaneDescription bufferLayout = buffer.GetPlaneDescription(0);
                        long[] shape = { 1, 3, inputImageHeight, inputImageWidth };
                        float[] pCPUTensor = new float[3 * inputImageWidth * inputImageHeight];
                        for (UInt32 i = 0; i < capacity; i += 4)
                        {
                            UInt32 pixelInd = i / 4;
                            pCPUTensor[pixelInd] = (float)dataInBytes[i];
                            pCPUTensor[(bufferLayout.Height * bufferLayout.Width) + pixelInd] = (float)dataInBytes[i + 1];
                            pCPUTensor[(bufferLayout.Height * bufferLayout.Width * 2) + pixelInd] = (float)dataInBytes[i + 2];
                        }
                        /*for (int i = 0; i < bufferLayout.Height; i++)
                        {
                            for (int j = 0; j < bufferLayout.Width; j++)
                            {

                                byte value = (byte)((float)j / bufferLayout.Width * 255);
                                dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 0] = value;
                                dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 1] = value;
                                dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 2] = value;
                                dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 3] = (byte)255;
                            }
                        }*/
                        for (int i = 0; i < 30; i++)
                        {
                            System.Diagnostics.Debug.WriteLine(pCPUTensor[i]);
                        }
                        System.Diagnostics.Debug.WriteLine("Processed");
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

        private static float[] NormalizeFloatArray(float[] src)
        {
            // Normalize
            float _meanVals = 127f;
            float _normVals = (float)(1.0 / 128);
            var normalized = new float[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                normalized[i] = (src[i] - _meanVals) * _normVals;
            }

            return normalized;
        }
    }
}
