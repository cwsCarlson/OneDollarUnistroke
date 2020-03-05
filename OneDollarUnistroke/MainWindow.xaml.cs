using System.Windows;
using System.Windows.Media;
using System.Windows.Ink;
using System.Windows.Input;

namespace OneDollarUnistroke
{
    /// Interaction logic for MainWindow.xaml
    public partial class MainWindow : Window
    {
        const int N = 64;

        public MainWindow()
        {
            InitializeComponent();
        }

        /// SampleStroke - returns a StylusPointCollection of N points
        ///                on the point, which are equal distance apart.
        private StylusPointCollection SampleStroke(Stroke curStroke)
        {
            // Get all the stylus points and convert them into a PathFigureCollection
            // consisting of line segments between each point.
            StylusPointCollection strokePoints = curStroke.StylusPoints;
            PathFigureCollection figureCollection = new PathFigureCollection();
            for (int i = 0; i < strokePoints.Count - 1; i++)
            {
                PathFigure figure = new PathFigure
                {
                    StartPoint = new Point(strokePoints[i].X, strokePoints[i].Y)
                };

                LineSegment lineSegment = new LineSegment
                {
                    Point = new Point(strokePoints[i + 1].X, strokePoints[i + 1].Y)
                };

                PathSegmentCollection segmentCollection = new PathSegmentCollection
                {
                    lineSegment
                };

                figure.Segments = segmentCollection;

                figureCollection.Add(figure);
            }

            // Convert the PathFigureCollection to the PathGeometry.
            PathGeometry pg = new PathGeometry
            {
                Figures = figureCollection
            };

            // For every 1/64th of the path, get the point and add it to sampledPoints.
            // (N - 1 is used so the last point is the end of the stroke.
            StylusPointCollection sampledPoints = new StylusPointCollection();
            for (double i = 0; i <= N - 1; i++)
            {
                #pragma warning disable IDE0059 // Ignore unnecessary assignment of tangent
                pg.GetPointAtFractionLength(i / (N - 1), out Point curPoint, out Point tangent);
                sampledPoints.Add(new StylusPoint(curPoint.X, curPoint.Y));
            }

            return sampledPoints;
        }

        private StylusPoint GetCentroid(StylusPointCollection points)
        {
            double xSum = 0;
            double ySum = 0;
            foreach(StylusPoint p in points)
            {
                xSum += p.X;
                ySum += p.Y;
            }
            return new StylusPoint(xSum / points.Count, ySum / points.Count);
        }

        // DrawPointCircle - Draws a PointCircle at (x, y) in the given color.
        private void DrawPointCircle(double x, double y, Color color)
        {
            // Create a StylusPoint at (x, y).
            StylusPointCollection spc = new StylusPointCollection
            {
                new StylusPoint(x, y)
            };

            // Create a PointCircle at the StylusPoint, set the color, and add it.
            Stroke cir = new PointCircle(spc);
            cir.DrawingAttributes.Color = color;
            myCanvas.Strokes.Add(cir);
        }

        /// LeftMouseUpHandler - Controls what happens when the left mouse is released.
        /// This means that the algorithm is run.
        private void LeftMouseUpHandler(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // If there are no strokes stored, end here.
            if (myCanvas.Strokes.Count == 0)
                return;

            // Get the ink stroke so it can be utilized.
            Stroke curStroke = myCanvas.Strokes[myCanvas.Strokes.Count - 1];

            // Delete all previous strokes and circles.
            myCanvas.Strokes.Clear();
            myCanvas.Strokes.Add(curStroke);

            // Step 1 - Split the stroke into N points.
            StylusPointCollection points = SampleStroke(curStroke);

            // DEBUG: Draw a circle around each point.
            byte curShade = 0;
            foreach (StylusPoint p in points)
            {
                DrawPointCircle(p.X, p.Y, Color.FromRgb(curShade, 0, 0));
                curShade += 0x4;
            }

            // Step 2 - Rotate so the angle between the 1st point and centroid is zero.
            StylusPoint centroid = GetCentroid(points);
            double xCenter = myCanvas.ActualWidth / 2;
            double yCenter = myCanvas.ActualHeight / 2;

            // DEBUG: Draw a circle at the centroid.
            DrawPointCircle(centroid.X, centroid.Y, Colors.Orange);

            // Step 3 - Scale the points to fit in a boundary square.

            // Step 4 - Compare each point set to several predetermined and determine the closest.

            // Step 5 - Calculate a score for this predetermined and inform the user.
        }
    }

    public class PointCircle : Stroke
    {
        public PointCircle(StylusPointCollection pts) : base(pts)
        {
            this.StylusPoints = pts;
        }

        protected override void DrawCore(DrawingContext drawingContext, DrawingAttributes drawingAttributes)
        {
            SolidColorBrush brush2 = new SolidColorBrush(drawingAttributes.Color);
            brush2.Freeze();
            StylusPoint stp = this.StylusPoints[0];
            double radius = 5;

            drawingContext.DrawEllipse(brush2, null, new System.Windows.Point(stp.X, stp.Y), radius, radius);
        }
    }
}