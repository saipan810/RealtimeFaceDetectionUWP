using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace RealtimeFaceDetection.FaceDrawer
{
    public interface IFaceDrawer
    {
        string Name { get; }

        void DrawToCanvas(Canvas target, List<FaceDetectData> faces);
    }

    public class FaceDetectData
    {
        /// <summary>
        /// Face rect.
        /// </summary>
        public Rect FaceRect { get; set; } = Rect.Empty;

        /// <summary>
        /// Face landmarks.
        /// Num of landmark( = facePoints.Count) is always 68.
        /// The following url indicates the meaning of landmarks.
        /// http://openface-api.readthedocs.io/en/latest/_images/dlib-landmark-mean.png
        /// </summary>
        public List<Point> FaceLandmarks { get; set; } = new List<Point>();
    }
}