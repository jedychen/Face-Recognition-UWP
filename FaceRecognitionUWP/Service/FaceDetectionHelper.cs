using System;
using System.Collections.Generic;
using System.Linq;
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
        public const int inputImageDataWidth = 320;
        public const int inputImageDataHeight = 240;

        private const float _scoreThreshold = 0.7f;
        private const float _iouThreshold = 0.5f;

        /// <summary>
        /// PreProcessing.
        /// Converts image data in SoftwareBitmap to TensorFloat.
        /// </summary>
        public static TensorFloat SoftwareBitmapToTensorFloat(SoftwareBitmap image)
        {
            int width = image.PixelWidth;
            int height = image.PixelHeight;
            using (BitmapBuffer buffer = image.LockBuffer(BitmapBufferAccessMode.Read))
            {
                using (var reference = buffer.CreateReference())
                {
                    // Implementation Reference:
                    // https://github.com/Microsoft/Windows-Machine-Learning/issues/22
                    unsafe
                    {
                        ((IMemoryBufferByteAccess)reference).GetBuffer(out byte* dataInBytes, out uint capacity);

                        long[] shape = { 1, 3, height, width };
                        float[] pCPUTensor = new float[3 * width * height];
                        for (int i = 0; i < capacity; i += 4)
                        {
                            int pixelInd = i / 4;
                            pCPUTensor[pixelInd] = (float)dataInBytes[i];
                            pCPUTensor[(height * width) + pixelInd] = (float)dataInBytes[i + 1];
                            pCPUTensor[(height * width * 2) + pixelInd] = (float)dataInBytes[i + 2];
                        }

                        float[] processedTensor = NormalizeFloatArray(pCPUTensor);

                        TensorFloat tensorFloats = TensorFloat.CreateFromArray(shape, processedTensor);
                        return tensorFloats;
                    }
                }
            }
        }

        /// <summary>
        /// PreProcessing.
        /// Normalizes the image data in float array.
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
        /// Processes scors and boxes and generate a list of face rectangles.
        /// </summary>
        /// <param name="scores">score output of Onnx model.</param>
        /// <param name="boxes">box output of Onnx model.</param>
        public static IEnumerable<FaceDetectionRectangle> Predict(TensorFloat scores, TensorFloat boxes)
        {
            var boundingBoxCollection = new List<FaceDetectionRectangle>();
            GenerateBBox(boundingBoxCollection, scores, boxes, _scoreThreshold);
            var faceList = new List<FaceDetectionRectangle>();
            NonMaximumSuppression(boundingBoxCollection, faceList, _iouThreshold);

            return faceList;
        }

        /// <summary>
        /// PostProcessing.
        /// Generates a list of BBox containing the detected face info.
        /// </summary>
        /// <param name="boundingBoxCollection">empty list of FaceDetectionRec to store results.</param>
        /// <param name="scores">score output of Onnx model.</param>
        /// <param name="boxes">box output of Onnx model.</param>
        /// <param name="scoreThreshold">threshold of score between 0 and 1 for filtering boxes.</param>
        private static void GenerateBBox(
            ICollection<FaceDetectionRectangle> boundingBoxCollection,
            TensorFloat scores, TensorFloat boxes,
            float scoreThreshold)
        {
            IReadOnlyList<float> vectorBoxes = boxes.GetAsVectorView();
            IList<float> boxList = vectorBoxes.ToList();
            IReadOnlyList<float> vectorScores = scores.GetAsVectorView();
            IList<float> scoreList = vectorScores.ToList();

            long numAnchors = scores.Shape[1];
            if (numAnchors <= 0)
                return;
            
            for (var i = 0; i < numAnchors; i++)
                if (scoreList[i * 2 + 1] > scoreThreshold)
                {
                    var rect = new FaceDetectionRectangle
                    {
                        X1 = boxList[i * 4] * inputImageDataWidth,
                        Y1 = boxList[i * 4 + 1] * inputImageDataHeight,
                        X2 = boxList[i * 4 + 2] * inputImageDataWidth,
                        Y2 = boxList[i * 4 + 3] * inputImageDataHeight,
                        Score = Clip(scoreList[i * 2 + 1], 0, 1)
                    };

                    boundingBoxCollection.Add(rect);
                }
        }

        /// <summary>
        /// Utils: Filter non-overlapped detected BBox with hard NMS.
        /// </summary>
        private static void NonMaximumSuppression(List<FaceDetectionRectangle> input, ICollection<FaceDetectionRectangle> output, float iouThreshold)
        {
            input.Sort((f1, f2) => f2.Score.CompareTo(f1.Score));
            var boxNum = input.Count;
            var merged = new int[boxNum];

            for (var i = 0; i < boxNum; i++)
            {
                if (merged[i] > 0)
                    continue;

                var buf = new List<FaceDetectionRectangle>
                {
                    input[i]
                };

                merged[i] = 1;

                for (var j = i + 1; j < boxNum; j++)
                {
                    if (merged[j] > 0)
                        continue;

                    var score = IOUOf(input[i], input[j]);

                    if (score < 0)
                        continue;

                    if (score > iouThreshold)
                    {
                        merged[j] = 1;
                        buf.Add(input[j]);
                    }
                }

                output.Add(buf[0]);
            }
        }

        /// <summary>
        /// Utils: Clip x between 0 and y.
        /// </summary>
        private static float Clip(float x, float min, float max)
        {
            return (float)(x < min ? min : x > max ? max : x);
        }

        /// <summary>
        /// Utils: Clip x between 0 and y.
        /// </summary>
        private static float Clip(float x, float min)
        {
            return (float)(x < min ? min : x);
        }

        /// <summary>
        /// Utils: Compute the areas of rectangles given two corners.
        /// </summary>
        private static float AreaOf(FaceDetectionRectangle rect)
        {
            var h = Clip(rect.Y2 - rect.Y1 + 1, 0);
            var w = Clip(rect.X2 - rect.X1 + 1, 0);
            return h * w;
        }

        /// <summary>
        /// Utils: Return intersection-over-union (Jaccard index) of 2 boxes.
        /// </summary>
        /// <returns>Float value > 0 if overlapped area exists. -1 if overlapped area doesn't exist.</returns>
        private static float IOUOf(FaceDetectionRectangle rect1, FaceDetectionRectangle rect2)
        {
            var rect = new FaceDetectionRectangle
            {
                X1 = Math.Max(rect1.X1, rect2.X1),
                Y1 = Math.Max(rect1.Y1, rect2.Y1),
                X2 = Math.Min(rect1.X2, rect2.X2),
                Y2 = Math.Min(rect1.Y2, rect2.Y2),
                Score = 0
            };

            var innerArea = AreaOf(rect);
            var area1 = AreaOf(rect1);
            var area2 = AreaOf(rect2);

            if (innerArea == 0 || area1 == 0 || area2 == 0)
                return -1;

            return innerArea / (area1 + area2 - innerArea);
        }
    }
}
