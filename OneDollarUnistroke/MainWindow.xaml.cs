using System;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace OneDollarUnistroke
{
    /// Interaction logic for MainWindow.xaml
    public partial class MainWindow : Window
    {
        private const int N = 64;
        private const int BOX_SIZE = 100;
        private readonly StylusPointCollection testSymbol;

        public MainWindow()
        {
            InitializeComponent();

            // Create the testSymbol.
            testSymbol = new StylusPointCollection
            {
                new StylusPoint(-1, -1),
                new StylusPoint(1, 1),
                new StylusPoint(1, -1),
                new StylusPoint(-1, 1)
            };
            testSymbol = SampleStroke(new Stroke(testSymbol));
            StylusPoint centroid = GetCentroid(testSymbol);
            double angleWithHorizontal = Math.Atan2(centroid.Y - testSymbol[0].Y, testSymbol[0].X - centroid.X);
            testSymbol = RotateAboutCentroid(testSymbol, -1 * angleWithHorizontal, centroid);
            testSymbol = ScaleToBox(testSymbol, BOX_SIZE, centroid);
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

            // Convert the PathFigureCollection into a PathGeometry.
            PathGeometry pg = new PathGeometry{Figures = figureCollection};

            // For every 1/64th of the path, get the point and add it to sampledPoints.
            // (N - 1 is used so the last point is the end of the stroke.)
            StylusPointCollection sampledPoints = new StylusPointCollection();
            for (double i = 0; i <= N - 1; i++)
            {
                #pragma warning disable IDE0059 // Ignore unnecessary assignment of tangent
                pg.GetPointAtFractionLength(i / (N - 1), out Point curPoint, out Point tangent);
                sampledPoints.Add(new StylusPoint(curPoint.X, curPoint.Y));
            }

            return sampledPoints;
        }

        // GetCentroid - Calculates and returns the centroid of points.
        private StylusPoint GetCentroid(StylusPointCollection points)
        {
            double xSum = 0;
            double ySum = 0;
            foreach (StylusPoint p in points)
            {
                xSum += p.X;
                ySum += p.Y;
            }
            return new StylusPoint(xSum / points.Count, ySum / points.Count);
        }

        // RotateAboutCentroid - Rotate points around centroid counterclockwise by the radians of angle.
        private StylusPointCollection RotateAboutCentroid(StylusPointCollection points, double angle, StylusPoint centroid)
        {
            StylusPointCollection rotated = new StylusPointCollection();

            for (int i = 0; i < points.Count; i++)
            {
                // origX and Y describe curPoint's location relative to the centroid.
                StylusPoint curPoint = points[i];
                double origX = curPoint.X - centroid.X;
                double origY = curPoint.Y - centroid.Y;
                
                // Calculate the new position relative to the centroid.
                // This is slightly different than the typical formula, since InkCanvas
                // does not use Cartesian coordinates: y is highest at the bottom.
                curPoint.X = origX * Math.Cos(angle) + origY * Math.Sin(angle);
                curPoint.Y = origY * Math.Cos(angle) - origX * Math.Sin(angle);

                // Move the new point to its actual position and add it to the new figure.
                curPoint.X += centroid.X;
                curPoint.Y += centroid.Y;
                rotated.Add(curPoint);
            }
            return rotated;
        }

        // GetCollectionDimensions - Return the width and height of points.
        private void GetCollectionDimensions(StylusPointCollection points, out double width, out double height)
        {
            // Set the defaults.
            double minPosX = points[0].X;
            double maxPosX = points[0].X;
            double minPosY = points[0].Y;
            double maxPosY = points[0].Y;

            // Find the most extreme X and Y values and assign them to the variables.
            foreach(StylusPoint p in points)
            {
                if (p.X < minPosX)
                    minPosX = p.X;
                if (p.X > maxPosX)
                    maxPosX = p.X;
                if (p.Y < minPosY)
                    minPosY = p.Y;
                if (p.Y > maxPosY)
                    maxPosY = p.Y;
            }

            // Assign the values to output.
            width = maxPosX - minPosX;
            height = maxPosY - minPosY;
        }

        // ScaleToBox - Scale points to fit in a square with sizes of boxSideLen around the centroid.
        private StylusPointCollection ScaleToBox(StylusPointCollection points, double boxSideLen, StylusPoint centroid)
        {
            // Get the dimensions and set the ratios of the width and height.
            GetCollectionDimensions(points, out double width, out double height);
            double widthRatio = boxSideLen / width;
            double heightRatio = boxSideLen / height;
            StylusPointCollection scaled = new StylusPointCollection();

            // Scale every point to the given ratios and move the centroid to the origin.
            for (int i = 0; i < points.Count; i++)
            {
                StylusPoint curPoint = points[i];

                curPoint.X = (curPoint.X - centroid.X) * widthRatio;
                curPoint.Y = (curPoint.Y - centroid.Y) * heightRatio;

                scaled.Add(curPoint);
            }
            return scaled;
        }

        // DrawPointCircle - Draws a PointCircle at (x, y) in the given color.
        private void DrawPointCircle(double x, double y, Color color)
        {
            // Create a StylusPoint at (x, y).
            StylusPointCollection spc = new StylusPointCollection{new StylusPoint(x, y)};

            // Create a PointCircle at the StylusPoint, set the color, and add it.
            Stroke cir = new PointCircle(spc);
            cir.DrawingAttributes.Color = color;
            myCanvas.Strokes.Add(cir);
        }

        // GetPathDistance - Calculates the path distance between spc1 and spc2.
        private double GetPathDistance(StylusPointCollection spc1, StylusPointCollection spc2)
        {
            // Throw an exception if, for whatever reason, the counts do not match.
            if (spc1.Count != spc2.Count)
                throw new ArgumentException("Unequal StylusPoint counts.");
            
            // For each set, calculate the disparities in the X and Y coordinates
            // and use them to calculate the distance between the points.
            double sumOfDistances = 0.0;
            for(int i = 0; i < spc1.Count; i++)
            {
                double xDisp = Math.Pow(spc1[i].X - spc2[i].X, 2);
                double yDisp = Math.Pow(spc1[i].Y - spc2[i].Y, 2);
                sumOfDistances += Math.Sqrt(xDisp + yDisp);
            }

            // Return the average distance.
            return sumOfDistances / spc1.Count;
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

            // DEBUG: Draw a circle at the centroid.
            DrawPointCircle(centroid.X, centroid.Y, Colors.Orange);

            // Get the angle between the first point and the centroid.
            double angleWithHorizontal = Math.Atan2(centroid.Y - points[0].Y, points[0].X - centroid.X);

            // Rotate the figure by this angle in the opposite direction.
            points = RotateAboutCentroid(points, -1 * angleWithHorizontal, centroid);

            // DEBUG: Draw a circle around each rotated point.
            curShade = 0;
            foreach (StylusPoint p in points)
            {
                DrawPointCircle(p.X, p.Y, Color.FromRgb(0, 255, curShade));
                curShade += 0x4;
            }

            // Step 3 - Scale the points to fit in a boundary square.
            points = ScaleToBox(points, BOX_SIZE, centroid);

            // DEBUG: Draw a circle around each scaled point.
            curShade = 0;
            foreach (StylusPoint p in points)
            {
                DrawPointCircle(p.X, p.Y, Color.FromRgb(255, curShade, 0));
                curShade += 0x4;
            }

            // Step 4 - Compare each point set to several predetermined and determine the closest.
            // Calculate the path distance (the average distance between corresponding points
            // on the user's stroke and the template).
            double pathDist = GetPathDistance(points, testSymbol);
            Console.WriteLine(pathDist);

            // Calculate the score, showing how close the path is to the template from zero to one.
            double score = 1 - (pathDist / (0.5 * BOX_SIZE * Math.Sqrt(2)));
            Console.WriteLine(score);

            // Step 5 - Calculate a score for this predetermined and inform the user.
        }
    }

    // PointCircle - a circle drawn around a specific point.
    public class PointCircle : Stroke
    {
        public PointCircle(StylusPointCollection pts) : base(pts)
        {
            this.StylusPoints = pts;
        }

        // DrawCore - Sets up and draws the ellipse.
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