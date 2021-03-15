using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaceRecognitionUWP
{
    public sealed class FaceLandmark
    {
        #region Properties

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

        #endregion
    }
}
