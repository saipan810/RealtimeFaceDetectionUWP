using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace RealtimeFaceDetection.FaceDrawer
{
    public class BlackEyeLineDrawer : IFaceDrawer
    {
        private readonly Brush _brush = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));

        public string Name { get; } = "BlackEyeLine";

        public void DrawToCanvas(Canvas target, List<FaceDetectData> faces)
        {
            target.Children.Clear();

            foreach (var face in faces)
            {
                var landmarks = face.FaceLandmarks;

                double height = Math.Max(
                    Math.Max(landmarks[40].Y - landmarks[38].Y, landmarks[41].Y - landmarks[37].Y),
                    Math.Max(landmarks[46].Y - landmarks[44].Y, landmarks[47].Y - landmarks[43].Y)
                    ) * 3;

                var eyeEnd1 = landmarks[36];
                var eyeEnd2 = landmarks[45];

                double width = Math.Sqrt(Math.Pow(eyeEnd2.X - eyeEnd1.X, 2) + Math.Pow(eyeEnd2.Y - eyeEnd1.Y, 2));

                var line = new Line();
                line.StrokeThickness = height;
                line.Stroke = _brush;
                line.X1 = eyeEnd1.X - width * 0.2;
                line.Y1 = Lerp(eyeEnd1, eyeEnd2, line.X1);
                line.X2 = eyeEnd2.X + width * 0.2;
                line.Y2 = Lerp(eyeEnd1, eyeEnd2, line.X2);

                target.Children.Add(line);
            }
        }

        private double Lerp(Point p1, Point p2, double x)
        {
            return p1.Y + (p2.Y - p1.Y) * (x - p1.X) / (p2.X - p1.X);
        }
    }
}