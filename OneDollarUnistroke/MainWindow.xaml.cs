using System.Windows;
using System.Windows.Ink;

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

            // Get the ink line so it can be utilized.
            Stroke curStroke = myCanvas.Strokes[0];

            // Step 1 - Split the stroke into N points.

            // Step 2 - Rotate so the angle between the 1st point and centroid is zero.

            // Step 3 - Scale the points to fit in a boundary square.

            // Step 4 - Compare each point set to several predetermined and determine the closest.

            // Step 5 - Calculate a score for this predetermined and inform the user.

            // Step 6 - Delete the ink line.
            myCanvas.Strokes.Clear();
        }
    }
}
