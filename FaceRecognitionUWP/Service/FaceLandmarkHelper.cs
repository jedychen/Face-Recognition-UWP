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
    public class FaceLandmarkHelper
    {
        /// <summary>
        /// PostProcessing.
        /// Clip x between 0 and y.
        /// </summary>
        public static IEnumerable<FaceLandmark> Predict(TensorFloat landmarks, int startX, int startY, int imageWidth, int imageHeight)
        {
            var landmarkList = new List<FaceLandmark>();
            ExtractLandmarks(landmarks, landmarkList, startX, startY, imageWidth, imageHeight);

            return landmarkList;
        }

        /// <summary>
        /// PostProcessing.
        /// Generate a list of BBox containing the detected face info.
        /// </summary>
        private static void ExtractLandmarks(TensorFloat landmarkTensors, ICollection<FaceLandmark> landmarkList, int startX, int startY, int width, int height)
        {
            IReadOnlyList<float> vectorLandmarks = landmarkTensors.GetAsVectorView();
            IList<float> landmarkFloatList = vectorLandmarks.ToList();
            long numAnchors = (long)Math.Ceiling(landmarkTensors.Shape[1] * 0.5);
            for (var i = 0; i < numAnchors; i++)
            {        
                var faceLandmark = new FaceLandmark();
                faceLandmark.X = landmarkFloatList[i * 2] * width + startX;
                faceLandmark.Y = landmarkFloatList[i * 2 + 1] * height + startY;

                landmarkList.Add(faceLandmark);
            }
        }
    }
}
