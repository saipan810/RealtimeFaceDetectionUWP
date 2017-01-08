using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace RealtimeFaceDetection.FaceDrawer
{
    public class FaceLineDrawer : IFaceDrawer
    {
        private readonly Brush _brush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));

        private readonly List<List<int>> LineIndexes = new List<List<int>>()
        {
            new List<int>() { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16 },
            new List<int>() { 17,18,19,20,21 },
            new List<int>() { 22,23,24,25,26 },
            new List<int>() { 27,28,29,30,33 },
            new List<int>() { 31,32,33,34,35 },
            new List<int>() { 36,37,38,39,40,41,36 },
            new List<int>() { 42,43,44,45,46,47,42 },
            new List<int>() { 48,49,50,51,52,53,54,55,56,57,58,59,60,48 },
            new List<int>() { 60,61,62,63,64,65,66,67,60 }
        };

        public string Name { get; } = "FaceLine";

        public void DrawToCanvas(Canvas target, List<FaceDetectData> faces)
        {
            target.Children.Clear();

            foreach (var face in faces)
            {
                foreach (var lineIndex in LineIndexes)
                {
                    for (int i = 0; i < lineIndex.Count - 1; i++)
                    {
                        int start = lineIndex[i];
                        int end = lineIndex[i + 1];

                        var line = CreateLine(face.FaceLandmarks[start], face.FaceLandmarks[end]);

                        target.Children.Add(line);
                    }
                }
            }
        }

        private Line CreateLine(Point start, Point end)
        {
            var line = new Line();
            line.StrokeThickness = 2;
            line.Stroke = _brush;
            line.X1 = start.X;
            line.Y1 = start.Y;
            line.X2 = end.X;
            line.Y2 = end.Y;

            return line;
        }
    }
}