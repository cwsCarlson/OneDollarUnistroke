# OneDollarUnistroke
This is an implementation of Wobbrock, Wilson, and Lee's [$1 gesture recognizer](http://faculty.washington.edu/wobbrock/pubs/uist-07.01.pdf).
The final application, as well as a gesture file, are available in [the release](https://github.com/cwsCarlson/OneDollarUnistroke/releases/tag/v0.1).

## Using this Program
This program will determine the shape of a stroke made in the main panel and output the results to the side panel.
The score, shown below the determined gesture, represents the confidence of the determination, with 1 being an exact match.

## Adding Gestures
This program may optionally run with an input file, which provides additional gestures for recognition.
If the program is run without a gestures.txt in the same directory, it will recognize a triangle and a single-stroke X.

### Formatting Gestures
Each line of an input file is a gesture. It must consist of a gesture name followed by space-separated coordinates formatted as "x,y" (without quotes).

## Compiling
If you would like to compile this yourself, download the repo and open the solution file in Visual Studio.
