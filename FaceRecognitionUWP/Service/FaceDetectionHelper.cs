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
        private const float _scoreThreshold = 0.7f;
        private const float _iou_threshold = 0.5f;

        private const float CenterVariance = 0.1f;

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

                        /*for (int i = 0; i < 30; i++)
                        {
                            System.Diagnostics.Debug.WriteLine(processedTensor[i]);
                        }*/

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

        /// <summary>
        /// PostProcessing.
        /// Clip x between 0 and y.
        /// </summary>
        public static IEnumerable<FaceDetectionInfo> Predict(TensorFloat scores, TensorFloat boxes, int top_k = -1)
        {
            var boundingBoxCollection = new List<FaceDetectionInfo>();
            GenerateBBox(boundingBoxCollection, scores, boxes, _scoreThreshold);
            var faceList = new List<FaceDetectionInfo>();
            NonMaximumSuppression(boundingBoxCollection, faceList, _iou_threshold);

            return faceList;
        }

        /// <summary>
        /// PostProcessing.
        /// Clip x between 0 and y.
        /// </summary>
        private static float Clip(double x, float y)
        {
            return (float)(x < 0 ? 0 : x > y ? y : x);
        }

        private static void GenerateBBox(ICollection<FaceDetectionInfo> boundingBoxCollection, TensorFloat scores, TensorFloat boxes, float scoreThreshold)
        {
            IReadOnlyList<float> vectorBoxes = boxes.GetAsVectorView();
            IList<float> boxList = vectorBoxes.ToList();
            IReadOnlyList<float> vectorScores = scores.GetAsVectorView();
            IList<float> scoreList = vectorScores.ToList();
            long numAnchors = scores.Shape[1];
            System.Diagnostics.Debug.WriteLine("numAnchors");
            System.Diagnostics.Debug.WriteLine(numAnchors);
            for (var i = 0; i < numAnchors; i++)
                if (scoreList[i * 2 + 1] > scoreThreshold)
                {
                    var rects = new FaceDetectionInfo();
                    rects.X1 = boxList[i * 4] * inputImageWidth;
                    rects.Y1 = boxList[i * 4 + 1] * inputImageHeight;
                    rects.X2 = boxList[i * 4 + 2] * inputImageWidth;
                    rects.Y2 = boxList[i * 4 + 3] * inputImageHeight;
                    rects.Score = Clip(scoreList[i * 2 + 1], 1);

                    boundingBoxCollection.Add(rects);
                }
        }

        private static void NonMaximumSuppression(List<FaceDetectionInfo> input, ICollection<FaceDetectionInfo> output, float iou_threshold)
        {
            input.Sort((f1, f2) => f2.Score.CompareTo(f1.Score));

            var boxNum = input.Count;

            var merged = new int[boxNum];

            for (var i = 0; i < boxNum; i++)
            {
                if (merged[i] > 0)
                    continue;

                var buf = new List<FaceDetectionInfo>
                {
                    input[i]
                };

                merged[i] = 1;

                var h0 = input[i].Y2 - input[i].Y1 + 1;
                var w0 = input[i].X2 - input[i].X1 + 1;

                var area0 = h0 * w0;

                for (var j = i + 1; j < boxNum; j++)
                {
                    if (merged[j] > 0)
                        continue;

                    var innerX0 = input[i].X1 > input[j].X1 ? input[i].X1 : input[j].X1;
                    var innerY0 = input[i].Y1 > input[j].Y1 ? input[i].Y1 : input[j].Y1;

                    var innerX1 = input[i].X2 < input[j].X2 ? input[i].X2 : input[j].X2;
                    var innerY1 = input[i].Y2 < input[j].Y2 ? input[i].Y2 : input[j].Y2;

                    var innerH = innerY1 - innerY0 + 1;
                    var innerW = innerX1 - innerX0 + 1;

                    if (innerH <= 0 || innerW <= 0)
                        continue;

                    var innerArea = innerH * innerW;

                    var h1 = input[j].Y2 - input[j].Y1 + 1;
                    var w1 = input[j].X2 - input[j].X1 + 1;

                    var area1 = h1 * w1;

                    var score = innerArea / (area0 + area1 - innerArea);

                    if (score > iou_threshold)
                    {
                        merged[j] = 1;
                        buf.Add(input[j]);
                    }
                }

                // NonMaximumSuppressionMode Hard
                output.Add(buf[0]);

                // NonMaximumSuppressionMode Blending
                // From Blaze Face
                /*var total = 0d;
                for (var j = 0; j < buf.Count; j++)
                {
                    total += Math.Exp(buf[j].Score);
                }

                var rects = new FaceDetectionInfo();
                for (var j = 0; j < buf.Count; j++)
                {
                    var rate = Math.Exp(buf[j].Score) / total;
                    rects.X1 += (float)(buf[j].X1 * rate);
                    rects.Y1 += (float)(buf[j].Y1 * rate);
                    rects.X2 += (float)(buf[j].X2 * rate);
                    rects.Y2 += (float)(buf[j].Y2 * rate);
                    rects.Score += (float)(buf[j].Score * rate);
                }

                output.Add(rects);*/
            }
        }
    }
}
