using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace RealtimeFaceDetection.FaceDrawer
{
    public class BasicFaceDrawer : IFaceDrawer
    {
        private readonly Brush _brush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));

        public string Name { get; } = "Basic";

        public void DrawToCanvas(Canvas target, List<FaceDetectData> faces)
        {
            target.Children.Clear();

            foreach (var face in faces)
            {
                var rectangle = CreateRectangle(face.FaceRect);
                target.Children.Add(rectangle);

                foreach (var point in face.FaceLandmarks)
                {
                    var ellipse = CreateEllipse(point);
                    target.Children.Add(ellipse);
                }
            }
        }

        private Rectangle CreateRectangle(Rect rect)
        {
            var rectangle = new Rectangle();
            rectangle.StrokeThickness = 2;
            rectangle.Stroke = _brush;
            rectangle.Width = rect.Width;
            rectangle.Height = rect.Height;
            rectangle.SetValue(Canvas.LeftProperty, rect.X);
            rectangle.SetValue(Canvas.TopProperty, rect.Y);

            return rectangle;
        }

        private Ellipse CreateEllipse(Point point)
        {
            var ellipse = new Ellipse();
            ellipse.Fill = _brush;
            ellipse.Width = 4;
            ellipse.Height = 4;
            ellipse.SetValue(Canvas.LeftProperty, point.X - (ellipse.Width / 2));
            ellipse.SetValue(Canvas.TopProperty, point.Y - (ellipse.Height / 2));

            return ellipse;
        }
    }
}