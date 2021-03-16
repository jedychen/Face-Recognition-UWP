using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Media;
using Windows.Foundation;

namespace FaceRecognitionUWP
{
    /// <summary>Class <c>DrawingFace</c> groups all drawing functions for Face Detection.
    /// </summary>
    class DrawingFace
    {
        public List<Path> pathes; // All output path

        private Path faceLandmarkPath;
        private Path faceRectanglePath;

        private readonly int _displayWidth; // Display image size in UI
        private readonly int _displayHeight;
        private int outputImageWidth; // Output image size in UI
        private int outputImageHeight;
        private int marginHorizontal; // Margin for positioning the landmarks
        private int marginVertical;
        private float scaleRatioWidth; // how much to scale
        private float scaleRatioHeight;

        /// <summary>
        /// Initial setup.
        /// </summary>
        /// <param name="width">width of the final displayed image in UI.</param>
        /// <param name="height">height of the final displayed image in UI.</param>
        public DrawingFace(int width, int height)
        {
            _displayWidth = width;
            _displayHeight = height;

            outputImageWidth = 1;
            outputImageHeight = 1;
            marginHorizontal = 1;
            marginVertical = 1;
            scaleRatioWidth = 1.0f;
            scaleRatioHeight = 1.0f;

            pathes = new List<Path>();
            faceLandmarkPath = new Path();
            faceRectanglePath = new Path();

            faceLandmarkPath.Fill = new SolidColorBrush(Windows.UI.Colors.Green);
            faceRectanglePath.Stroke = new SolidColorBrush(Windows.UI.Colors.Red);
            faceRectanglePath.StrokeThickness = 1;
        }

        /// <summary>
        /// Clears all drawing path.
        /// </summary>
        public void Clear()
        {
            pathes.Clear();
        }

        /// <summary>
        /// If there is path to draw.
        /// </summary>
        public bool HasPath => pathes.Count > 0;

        /// <summary>
        /// Calculates positions and dimensions to draw on final canvas in UI.
        /// </summary>
        /// <param name="imageOriginalWidth">Input image's original size.</param>
        /// <param name="imageOriginalHeight">Input image's original size.</param>
        /// <param name="inputImageWidth">Input image's processed size.</param>
        /// <param name="inputImageHeight">Input image's processed size.</param>
        public void UpdateDimensions(int imageOriginalWidth, int imageOriginalHeight, float inputImageWidth, float inputImageHeight)
        {
            ImageStrechedValues(
                imageOriginalWidth,
                imageOriginalHeight,
                ref outputImageWidth,
                ref outputImageHeight,
                ref marginHorizontal,
                ref marginVertical);
            scaleRatioWidth = (float)outputImageWidth / inputImageWidth;
            scaleRatioHeight = (float)outputImageHeight / inputImageHeight;
        }

        /// <summary>
        /// Draws both face rectangles and facial landmarks.
        /// </summary>
        public void DrawFaceAll(List<FaceDetectionRectangle> faceRects, List<FaceLandmarks> faceLandmarksList)
        {
            DrawFaceRetangles(faceRects);
            DrawFaceLandmarks(faceLandmarksList);
        }

        public void DrawFaceRetangles(List<FaceDetectionRectangle> faceRects)
        {
            var faceGeometryGroup = new GeometryGroup();
            foreach (FaceDetectionRectangle face in faceRects)
            {
                var rectangle = new RectangleGeometry
                {
                    Rect = new Rect(
                    (int)(face.X1 * scaleRatioWidth + marginHorizontal),
                    (int)(face.Y1 * scaleRatioHeight + marginVertical),
                    (int)(face.X2 - face.X1 + 1) * scaleRatioWidth,
                    (int)(face.Y2 - face.Y1 + 1) * scaleRatioHeight)
                };
                faceGeometryGroup.Children.Add(rectangle);
            }
            faceRectanglePath.Data = faceGeometryGroup;
            pathes.Add(faceRectanglePath);
        }

        public void DrawFaceLandmarks(List<FaceLandmarks> faceLandmarksList)
        {
            var faceLandmarkGeometryGroup = new GeometryGroup();

            foreach (FaceLandmarks landmarks in faceLandmarksList)
                foreach (FaceLandmark mark in landmarks.landmarkList)
                {
                    var ellipse = new EllipseGeometry
                    {
                        Center = new Point(
                        (int)(mark.X * scaleRatioWidth + marginHorizontal),
                        (int)(mark.Y * scaleRatioHeight + marginVertical)),
                        RadiusX = 2,
                        RadiusY = 2
                    };
                    faceLandmarkGeometryGroup.Children.Add(ellipse);
                }
            faceLandmarkPath.Data = faceLandmarkGeometryGroup;
            pathes.Add(faceLandmarkPath);
        }

        /// <summary>
        /// Resizes image to allow image fits the max width and height.
        /// </summary>
        public void ImageStrechedValues(int originalWidth, int originalHeight, ref int outputWidth, ref int outputHeight, ref int marginHorizontal, ref int marginVertical)
        {
            if (_displayWidth < 0 || _displayHeight < 0 || originalWidth <= 0 || originalHeight <= 0)
            {
                System.Diagnostics.Debug.WriteLine("ImageHelper::StretchRatio - Wrong Input");
                return;
            }

            float originalRatio = (float)originalWidth / (float)originalHeight;
            float maxRatio = (float)_displayWidth / (float)_displayHeight;

            if (originalRatio > maxRatio)
            {
                outputWidth = _displayWidth;
                outputHeight = (int)((float)_displayWidth / originalRatio);
                marginVertical = (int)Math.Ceiling((_displayHeight - outputHeight) * 0.5);
                marginHorizontal = 0;
            }
            else
            {
                outputHeight = _displayHeight;
                outputWidth = (int)(originalRatio * (float)_displayHeight);
                marginHorizontal = (int)Math.Ceiling((_displayWidth - outputWidth) * 0.5);
                marginVertical = 0;
            }
        }
    }
}
