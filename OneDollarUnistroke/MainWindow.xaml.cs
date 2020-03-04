using System.Drawing;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Ink;
using System.Windows.Input;

namespace OneDollarUnistroke
{
    /// Interaction logic for MainWindow.xaml
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        /// LeftMouseUpHandler - Controls what happens when the left mouse is released.
        /// This means that the algorithm is run.
        private void LeftMouseUpHandler(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // If there are no strokes stored, end here.
            if (myCanvas.Strokes.Count == 0)
                return;

            // Delete all previous strokes.
            while(myCanvas.Strokes.Count != 1)
                myCanvas.Strokes.Remove(myCanvas.Strokes[0]);

            // Get the ink line so it can be utilized.
            Stroke curStroke = myCanvas.Strokes[0];

            // Step 1 - Split the stroke into N points.
            StylusPointCollection points = curStroke.StylusPoints;

            // DEBUG: Draws a circle around each point.
            foreach (StylusPoint p in points)
            {
                StylusPointCollection pts = new StylusPointCollection
                {
                    new StylusPoint(p.X, p.Y)
                };

                Stroke cir = new PointCircle(pts);

                cir.DrawingAttributes.Color = Colors.Red;
                myCanvas.Strokes.Add(cir);
            }

            // Step 2 - Rotate so the angle between the 1st point and centroid is zero.

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