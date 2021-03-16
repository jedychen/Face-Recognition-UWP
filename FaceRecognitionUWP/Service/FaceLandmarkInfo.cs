using System;
using System.Collections.Generic;

namespace FaceRecognitionUWP
{
    /// <summary>Class to store a single landmark position</summary>
    public sealed class FaceLandmark
    {
        /// <summary>
        /// Gets the x position of face landmark.
        /// </summary>
        public float X
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the y position of face landmark.
        /// </summary>
        public float Y
        {
            get;
            internal set;
        }
    }

    /// <summary>Class to store a collection of all facial landmarks</summary>
    public sealed class FaceLandmarks
    {

        public List<FaceLandmark> landmarkList;

        private readonly FaceLandmark _emptylandmark;
        private readonly int _totalNum;
        private float _eyeDistance;

        public FaceLandmarks()
        {
            _totalNum = 68;
            _emptylandmark = new FaceLandmark { X = 0, Y = 0 };
            _eyeDistance = 0;
            landmarkList = new List<FaceLandmark>();
        }

        public bool IsValid => landmarkList.Count == _totalNum;
        
        /// <summary>
        /// Gets the position of left eye.
        /// </summary>
        public FaceLandmark LeftEye => IsValid ? landmarkList[40] : _emptylandmark;

        /// <summary>
        /// Gets the position of right eye.
        /// </summary>
        public FaceLandmark RightEye => IsValid ? landmarkList[46] : _emptylandmark;

        /// <summary>
        /// Gets the distance of eyes.
        /// </summary>
        public float EyeDistance
        {
            get
            {
                if (_eyeDistance != 0)
                    return _eyeDistance;
                float deltaX = Math.Abs(LeftEye.X - RightEye.X);
                float deltaY = Math.Abs(LeftEye.Y - RightEye.Y);
                _eyeDistance = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
                return _eyeDistance;
            }
        }
    }
}
