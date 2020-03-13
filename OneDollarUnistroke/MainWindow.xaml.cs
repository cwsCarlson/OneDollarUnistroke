using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace OneDollarUnistroke
{
    /// Interaction logic for MainWindow.xaml
    public partial class MainWindow : Window
    {
        private const int BOX_SIZE = 100;
        private const int MARGIN_OF_RECOGNITION = 2;
        private const int MAX_RECOGNITION_ANGLE = 45;
        private const int N = 64;
        private const int SIDE_MARGIN = 25;
        
        private readonly Dictionary<string, StylusPointCollection> gestures;
        private readonly double GOLD_RATIO = (Math.Sqrt(5) - 1) / 2;
        private StylusPoint centroid;

        /// Constructor - Makes the MainWindow and loads the gestures.
        public MainWindow()
        {
            InitializeComponent();

            // Set the color of sideCanvas.
            sideCanvas.Background = Brushes.LightGray;

            // Create the gesture dictionary.
            gestures = new Dictionary<string, StylusPointCollection>();

            // Read in the gesture file and add what is listed.
            try
            {
                string[] lines = File.ReadAllLines("gestures.txt");
                foreach(string curLine in lines)
                {
                    string[] contents = curLine.Split(' ');
                    string name = contents[0];

                    StylusPointCollection curGesture = new StylusPointCollection();
                    for(int i = 1; i < contents.Length; i++)
                    {
                        string[] coordinates = contents[i].Split(',');
                        int curX = Int32.Parse(coordinates[0]);
                        int curY = Int32.Parse(coordinates[1]);
                        Console.Write("(" + curX + "," + curY + ") ");
                        curGesture.Add(new StylusPoint(curX, curY));
                    }
                    AddGesture(curGesture, name);
                    Console.WriteLine();
                }
            }
            // If the file is not found, use two predetermined gestures.
            catch (FileNotFoundException)
            {
                // Create the 1st predetermined gesture, Triangle.
                StylusPointCollection triGesture = new StylusPointCollection
                {
                    new StylusPoint(-1, 0),
                    new StylusPoint(0, 1),
                    new StylusPoint(1, 0),
                    new StylusPoint(-1, 0)
                };
                AddGesture(triGesture, "Triangle");

                // Create the 2nd predetermined gesture, X.
                StylusPointCollection xGesture = new StylusPointCollection
                {
                    new StylusPoint(-1, -1),
                    new StylusPoint(1, 1),
                    new StylusPoint(1, -1),
                    new StylusPoint(-1, 1)
                };
                AddGesture(xGesture, "X");
            }
        }

        /// AddGesture - Rotates toAdd, scales it, and adds it to the dictionary.
        private void AddGesture(StylusPointCollection toAdd, string name)
        {
            toAdd = SampleStroke(new Stroke(toAdd));
            centroid = GetCentroid(toAdd);
            double angleWithHorizontal = Math.Atan2(centroid.Y - toAdd[0].Y, toAdd[0].X - centroid.X);
            toAdd = RotateAndTranslate(toAdd, -1 * angleWithHorizontal);
            gestures.Add(name, ScaleToBox(toAdd, BOX_SIZE));
        }

        /// SampleStroke - returns a StylusPointCollection of N points,
        ///                which are an equal distance apart.
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

        /// GetCentroid - Calculates and returns the centroid of points.
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

        /// RotateAndTranslate - Rotate points around the centroid counterclockwise
        ///                      by angle (in radians), and then translates it to the origin.
        private StylusPointCollection RotateAndTranslate(StylusPointCollection points, double angle)
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

                // Add the point to the new figure.
                rotated.Add(curPoint);
            }

            // Set the centroid to the origin, since it has been translated there.
            centroid.X = 0;
            centroid.Y = 0;

            return rotated;
        }

        /// GetCollectionDimensions - Return the width and height of points.
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

        /// ScaleToBox - Scale points to fit in a square with sizes of boxSideLen.
        private StylusPointCollection ScaleToBox(StylusPointCollection points, double boxSideLen)
        {
            // Get the dimensions and set the ratios of the width and height.
            GetCollectionDimensions(points, out double width, out double height);
            double widthRatio = boxSideLen / width;
            double heightRatio = boxSideLen / height;

            // If either ratio is invalid, set it to a usable value.
            if (double.IsInfinity(widthRatio))
                widthRatio = boxSideLen;
            if (double.IsInfinity(heightRatio))
                heightRatio = boxSideLen;

            // Scale every point to the given ratios.
            StylusPointCollection scaled = new StylusPointCollection();
            for (int i = 0; i < points.Count; i++)
            {
                StylusPoint curPoint = points[i];

                curPoint.X *= widthRatio;
                curPoint.Y *= heightRatio;

                scaled.Add(curPoint);
            }
            return scaled;
        }

        /// GetPathDistance - Calculates the path distance between spc1 and spc2.
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

        /// GetBestPathDistance - Calculates the best path distance between spc1 and spc2
        ///                       by rotating spc1 until the boundaries of the
        ///                       potential angles are within the MARGIN_OF_RECOGNITION.
        private double GetBestPathDistance(StylusPointCollection spc1, StylusPointCollection spc2)
        {
            // Set the first minimum and maximum theta values.
            double minAngle = -1 * MAX_RECOGNITION_ANGLE;
            double maxAngle = MAX_RECOGNITION_ANGLE;

            // Set the first two angles of rotation.
            double angleA = GOLD_RATIO * minAngle + (1 - GOLD_RATIO) * maxAngle;
            double angleB = (1 - GOLD_RATIO) * minAngle + GOLD_RATIO * maxAngle;

            // Get the path distances at angleA and angleB.
            double pathDistA = GetPathDistance(RotateAndTranslate(spc1, angleA), spc2);
            double pathDistB = GetPathDistance(RotateAndTranslate(spc1, angleB), spc2);

            // While the min and max are not close, binary search for the best angle.
            while(Math.Abs(minAngle - maxAngle) > MARGIN_OF_RECOGNITION)
            {
                // If angleA has a better score, move the max there and recalculate.
                if(pathDistA < pathDistB)
                {
                    maxAngle = angleB;
                    angleB = angleA;
                    pathDistB = pathDistA;
                    angleA = GOLD_RATIO * minAngle + (1 - GOLD_RATIO) * maxAngle;
                    pathDistA = GetPathDistance(RotateAndTranslate(spc1, angleA), spc2);
                }
                // If angleB has a better score, move the min there and recalculate.
                else
                {
                    minAngle = angleA;
                    angleA = angleB;
                    pathDistA = pathDistB;
                    angleB = (1 - GOLD_RATIO) * minAngle + GOLD_RATIO * maxAngle;
                    pathDistB = GetPathDistance(RotateAndTranslate(spc1, angleB), spc2);
                }
            }

            // Return the best path distance.
            return Math.Min(pathDistA, pathDistB);
        }

        /// WriteText - Writes the given text with the center at (x, y).
        private void WriteText(double x, double y, string text)
        {
            // Create the TextBlock.
            TextBlock textBlock = new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush(Colors.Black)
            };

            // Get the coordinates for the TextBlock's upper left.
            ConvertTextCoordinates(x, y, textBlock, out x, out y);

            // Set the block's location and add it.
            Canvas.SetLeft(textBlock, x);
            Canvas.SetTop(textBlock, y);
            sideCanvas.Children.Add(textBlock);
        }

        /// ConvertTextCoordinates - Convert x and y, which refer to the text's center,
        ///                          to coordinates which refer to the upper-left.
        private void ConvertTextCoordinates(double x, double y, TextBlock text, out double xOut, out double yOut)
        {
            // Create the FormattedText object.
            FormattedText formattedText = new FormattedText(text.Text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                                                            new Typeface(text.FontFamily, text.FontStyle, text.FontWeight,
                                                            text.FontStretch), text.FontSize, Brushes.Black);

            // Calculate the new values.
            xOut = x - (formattedText.Width / 2.0);
            yOut = y - (formattedText.Height / 2.0);
        }

        /// WriteOutput - Writes the output (gesture and score) to the sideCanvas.
        private void WriteOutput(string gestureName, double score)
        {
            // Clear the canvas.
            sideCanvas.Children.Clear();

            // Write the name at the top.
            WriteText(sideCanvas.ActualWidth / 2, (sideCanvas.ActualHeight / 2) - (BOX_SIZE / 2.0) - SIDE_MARGIN, gestureName);

            // Place the gesture in the middle by drawing lines between StylusPoints.
            StylusPointCollection points = gestures[gestureName];
            for (int i = 0; i < points.Count - 1; i++)
            {
                Line line = new Line
                {
                    Stroke = Brushes.Black,

                    X1 = points[i].X + sideCanvas.ActualWidth / 2,
                    X2 = points[i + 1].X + sideCanvas.ActualWidth / 2,
                    Y1 = points[i].Y + sideCanvas.ActualHeight / 2,
                    Y2 = points[i + 1].Y + sideCanvas.ActualHeight / 2,

                    StrokeThickness = 2
                };
                sideCanvas.Children.Add(line);
            }

            // Write the score at the bottom.
            WriteText(sideCanvas.ActualWidth / 2, (sideCanvas.ActualHeight / 2) + (BOX_SIZE / 2.0) + SIDE_MARGIN, "Score:\n" + score);
        }

        /// LeftMouseUpHandler - Controls what happens when the left mouse is released.
        /// This means that the algorithm is run.
        private void LeftMouseUpHandler(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // If there are no strokes stored, end here.
            if (mainCanvas.Strokes.Count == 0)
                return;

            // Get the ink stroke so it can be utilized.
            Stroke curStroke = mainCanvas.Strokes[mainCanvas.Strokes.Count - 1];

            // Delete all previous strokes.
            mainCanvas.Strokes.Clear();
            mainCanvas.Strokes.Add(curStroke);

            // Step 1 - Split the stroke into N points.
            StylusPointCollection points = SampleStroke(curStroke);

            // Step 2 - Rotate so the angle between the 1st point and centroid is zero.
            // Additionally, move the centroid to (0, 0) so future calculations are easier.
            centroid = GetCentroid(points);

            // Get the angle between the first point and the centroid.
            double angleWithHorizontal = Math.Atan2(centroid.Y - points[0].Y, points[0].X - centroid.X);

            // Rotate the figure by this angle in the opposite direction, and translate to the origin.
            points = RotateAndTranslate(points, -1 * angleWithHorizontal);

            // Step 3 - Scale the points to fit in a boundary square.
            points = ScaleToBox(points, BOX_SIZE);

            // Step 4 - Compare each point set to predetermined gestures and determine the closest.
            // Calculate the path distance (the average distance between corresponding points
            // on the user's stroke and the template at the best angle) for each gesture and
            // record it if its path distance is smaller.
            string gestureName = null;
            double pathDist = double.MaxValue;
            foreach (KeyValuePair<string, StylusPointCollection> pair in gestures)
            {
                double curPathDist = GetBestPathDistance(points, pair.Value);
                if(curPathDist < pathDist)
                {
                    gestureName = pair.Key;
                    pathDist = curPathDist;
                }
            }

            // Calculate the score, showing how close the path is to the template from zero to one.
            double score = 1 - (pathDist / (0.5 * BOX_SIZE * Math.Sqrt(2)));

            // Write the output to the sideCanvas.
            WriteOutput(gestureName, score);
        }
    }
}