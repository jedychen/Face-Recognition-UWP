using System;
using System.Collections.Generic;
using System.Linq;
using Windows.AI.MachineLearning;

namespace FaceRecognitionUWP
{
    /// <summary>Class <c>FaceLandmarkHelper</c> groups all functions for Detecting Facial Landmarks.
    /// </summary>
    public class FaceLandmarkHelper
    {
        /// <summary>
        /// Processes scors and boxes and generate a list of face rectangles.
        /// </summary>
        /// <param name="landmarkTensors">landmark output of Onnx model.</param>
        /// <param name="imageX">X start position of the image.</param>
        /// <param name="imageY">Y start position of the image.</param>
        /// <param name="imageWidth">width of the image.</param>
        /// <param name="imageHeight">height of the image.</param>
        public static FaceLandmarks Predict(TensorFloat landmarkTensors, int imageX, int imageY, int imageWidth, int imageHeight)
        {
            var faceLandmarks = new FaceLandmarks();
            
            IReadOnlyList<float> vectorLandmarks = landmarkTensors.GetAsVectorView();
            IList<float> landmarkFloatList = vectorLandmarks.ToList();
            long numAnchors = (long)Math.Ceiling(landmarkTensors.Shape[1] * 0.5);
            for (var i = 0; i < numAnchors; i++)
            {
                var mark = new FaceLandmark
                {
                    X = landmarkFloatList[i * 2] * imageWidth + imageX,
                    Y = landmarkFloatList[i * 2 + 1] * imageHeight + imageY
                };

                faceLandmarks.landmarkList.Add(mark);
            }

            return faceLandmarks;
        }
    }
}
