using System.Drawing.Imaging;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace SpotTheDifference;

public partial class Form1 : Form
{
    private PictureBox pictureBox1;
    private PictureBox pictureBox2;
    private int differences = 0;
    private int foundDifferences = 0;
    private List<Rectangle> differenceRegions;
    private Mat image1Mat;
    private Mat image2Mat;
    private Mat image1WithBoundaries;
    private Mat image2WithBoundaries;
    private Mat binaryDiffImage; // Store the binary difference image
    private bool showingBinaryDiff = false;
    
    public Form1()
    {
        InitializeComponent();
        InitializeGame();
    }

    private void InitializeGame()
    {
        this.Size = new Size(1200, 600);
        this.Text = "Spot the Difference Game";
        differenceRegions = new List<Rectangle>();

        pictureBox1 = new PictureBox
        {
            Size = new Size(500, 500),
            Location = new Point(50, 50),
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle
        };

        pictureBox2 = new PictureBox
        {
            Size = new Size(500, 500),
            Location = new Point(600, 50),
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle
        };

        // Add buttons in a horizontal layout
        var buttonPanel = new FlowLayoutPanel
        {
            Location = new Point(50, 520),
            Size = new Size(1100, 40),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        var showBoundariesButton = new Button
        {
            Text = "Show/Hide Boundaries",
            Size = new Size(150, 30),
            Margin = new Padding(10, 0, 10, 0)
        };
        showBoundariesButton.Click += ShowBoundariesButton_Click;

        var showBinaryButton = new Button
        {
            Text = "Show/Hide Binary Difference",
            Size = new Size(170, 30),
            Margin = new Padding(10, 0, 10, 0)
        };
        showBinaryButton.Click += ShowBinaryButton_Click;

        var saveBinaryButton = new Button
        {
            Text = "Save Binary Image",
            Size = new Size(150, 30),
            Margin = new Padding(10, 0, 10, 0)
        };
        saveBinaryButton.Click += SaveBinaryButton_Click;

        buttonPanel.Controls.AddRange(new Control[] { showBoundariesButton, showBinaryButton, saveBinaryButton });
        this.Controls.Add(buttonPanel);

        pictureBox1.Click += PictureBox_Click;
        pictureBox2.Click += PictureBox_Click;

        this.Controls.Add(pictureBox1);
        this.Controls.Add(pictureBox2);

        LoadImages();
    }

    private bool showingBoundaries = false;
    private void ShowBoundariesButton_Click(object sender, EventArgs e)
    {
        showingBoundaries = !showingBoundaries;
        UpdateImageDisplay();
    }

    private void ShowBinaryButton_Click(object sender, EventArgs e)
    {
        showingBinaryDiff = !showingBinaryDiff;
        showingBoundaries = false; // Turn off boundaries when showing binary
        UpdateImageDisplay();
    }

    private void SaveBinaryButton_Click(object sender, EventArgs e)
    {
        if (binaryDiffImage != null)
        {
            using (SaveFileDialog saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "PNG Image|*.png";
                saveDialog.Title = "Save Binary Difference Image";
                saveDialog.FileName = "binary_difference.png";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    binaryDiffImage.Save(saveDialog.FileName);
                    MessageBox.Show("Binary difference image saved successfully!");
                }
            }
        }
    }

    private void UpdateImageDisplay()
    {
        if (showingBinaryDiff && binaryDiffImage != null)
        {
            pictureBox1.Image = binaryDiffImage.ToBitmap();
            pictureBox2.Image = binaryDiffImage.ToBitmap();
        }
        else if (showingBoundaries)
        {
            pictureBox1.Image = image1WithBoundaries?.ToBitmap();
            pictureBox2.Image = image2WithBoundaries?.ToBitmap();
        }
        else
        {
            pictureBox1.Image = image1Mat?.ToBitmap();
            pictureBox2.Image = image2Mat?.ToBitmap();
        }
    }

    private void LoadImages()
    {
        try
        {
            // Load images using Emgu CV
            image1Mat = CvInvoke.Imread("1.png", ImreadModes.Color);
            image2Mat = CvInvoke.Imread("1d.png", ImreadModes.Color);

            // Create copies for drawing boundaries
            image1WithBoundaries = image1Mat.Clone();
            image2WithBoundaries = image2Mat.Clone();

            // Convert Mat to Bitmap for display
            pictureBox1.Image = image1Mat.ToBitmap();
            pictureBox2.Image = image2Mat.ToBitmap();

            // Find differences between images
            FindDifferences();
            
            // Draw boundaries on the copies
            DrawBoundaries();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error loading images: " + ex.Message);
        }
    }

    private void DrawBoundaries()
    {
        // Create fresh copies of the original images
        image1WithBoundaries = image1Mat.Clone();
        image2WithBoundaries = image2Mat.Clone();

        // Draw rectangles around all differences
        foreach (Rectangle rect in differenceRegions)
        {
            // Draw red rectangle (BGR format in OpenCV)
            CvInvoke.Rectangle(image1WithBoundaries, rect, new MCvScalar(0, 0, 255), 2);
            CvInvoke.Rectangle(image2WithBoundaries, rect, new MCvScalar(0, 0, 255), 2);
            
            // Add number labels to the rectangles
            Point textPoint = new Point(rect.X, rect.Y - 10);
            int index = differenceRegions.IndexOf(rect) + 1;
            CvInvoke.PutText(image1WithBoundaries, index.ToString(), textPoint,
                FontFace.HersheyComplex, 1, new MCvScalar(0, 0, 255), 2);
            CvInvoke.PutText(image2WithBoundaries, index.ToString(), textPoint,
                FontFace.HersheyComplex, 1, new MCvScalar(0, 0, 255), 2);
        }
    }

    private void FindDifferences()
    {
        // Convert images to grayscale
        Mat gray1 = new Mat();
        Mat gray2 = new Mat();
        CvInvoke.CvtColor(image1Mat, gray1, ColorConversion.Bgr2Gray);
        CvInvoke.CvtColor(image2Mat, gray2, ColorConversion.Bgr2Gray);

        // Apply Gaussian blur to reduce noise
        CvInvoke.GaussianBlur(gray1, gray1, new Size(3, 3), 0);
        CvInvoke.GaussianBlur(gray2, gray2, new Size(3, 3), 0);

        // Calculate absolute difference between images
        Mat diffImage = new Mat();
        CvInvoke.AbsDiff(gray1, gray2, diffImage);

        // Apply threshold to get binary image
        binaryDiffImage = new Mat();
        CvInvoke.Threshold(diffImage, binaryDiffImage, 10, 255, ThresholdType.Binary);

        // Store a copy of the binary image before morphological operations
        Mat thresholdImage = binaryDiffImage.Clone();

        // Use larger kernel for morphological operations to connect nearby differences
        Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(15, 15), new Point(-1, -1));
        
        // Dilate to connect nearby components
        CvInvoke.Dilate(thresholdImage, thresholdImage, kernel, new Point(-1, -1), 2, BorderType.Default, new MCvScalar());
        
        // Erode to shrink back while maintaining connections
        CvInvoke.Erode(thresholdImage, thresholdImage, kernel, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());

        // Find contours in the difference image
        using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
        {
            CvInvoke.FindContours(thresholdImage, contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);

            var sizedContours = new List<(Rectangle box, int area, double score)>();

            // Process each contour
            for (int i = 0; i < contours.Size; i++)
            {
                Rectangle boundingBox = CvInvoke.BoundingRectangle(contours[i]);
                int area = boundingBox.Width * boundingBox.Height;
                
                // Calculate the percentage of white pixels in the region
                using (Mat roi = new Mat(thresholdImage, boundingBox))
                {
                    MCvScalar sum = CvInvoke.Sum(roi);
                    double whitePixelRatio = sum.V0 / (255.0 * area);
                    
                    // Only consider regions with significant white pixels (real differences)
                    if (area > 250 && whitePixelRatio > 0.2) // Reduced ratio threshold since we're combining regions
                    {
                        double score = area * whitePixelRatio;
                        sizedContours.Add((boundingBox, area, score));
                    }
                }
            }

            // Merge overlapping or nearby rectangles
            var mergedContours = new List<(Rectangle box, int area, double score)>();
            var processedIndices = new HashSet<int>();

            for (int i = 0; i < sizedContours.Count; i++)
            {
                if (processedIndices.Contains(i)) continue;

                var currentRect = sizedContours[i].box;
                var currentArea = sizedContours[i].area;
                var currentScore = sizedContours[i].score;
                var merged = false;

                // Expand the rectangle slightly to detect nearby differences
                var expandedRect = new Rectangle(
                    currentRect.X - 20,
                    currentRect.Y - 20,
                    currentRect.Width + 40,
                    currentRect.Height + 40
                );

                for (int j = i + 1; j < sizedContours.Count; j++)
                {
                    if (processedIndices.Contains(j)) continue;

                    var otherRect = sizedContours[j].box;
                    
                    // Check if rectangles are close or overlapping
                    if (expandedRect.IntersectsWith(otherRect))
                    {
                        // Merge the rectangles
                        currentRect = Rectangle.Union(currentRect, otherRect);
                        currentArea += sizedContours[j].area;
                        currentScore += sizedContours[j].score;
                        processedIndices.Add(j);
                        merged = true;
                    }
                }

                if (merged)
                {
                    mergedContours.Add((currentRect, currentArea, currentScore));
                }
                else if (!processedIndices.Contains(i))
                {
                    mergedContours.Add(sizedContours[i]);
                }
                processedIndices.Add(i);
            }

            // Sort by score
            mergedContours.Sort((a, b) => b.score.CompareTo(a.score));

            // Clear previous differences
            differenceRegions.Clear();

            // Take only the top N largest and most significant differences
            int desiredDifferences = 7;
            for (int i = 0; i < Math.Min(desiredDifferences, mergedContours.Count); i++)
            {
                differenceRegions.Add(mergedContours[i].box);
            }
        }

        differences = differenceRegions.Count;
        
        // Draw boundaries after finding differences
        DrawBoundaries();

        // Dispose of temporary Mats
        gray1.Dispose();
        gray2.Dispose();
        diffImage.Dispose();
        thresholdImage.Dispose();
    }

    private void PictureBox_Click(object sender, EventArgs e)
    {
        Point clickPoint = this.PointToClient(Cursor.Position);
        PictureBox clickedBox = (PictureBox)sender;
        
        // Convert click coordinates to image coordinates
        float scaleX = (float)clickedBox.Image.Width / clickedBox.Width;
        float scaleY = (float)clickedBox.Image.Height / clickedBox.Height;
        
        Point imagePoint = new Point(
            (int)((clickPoint.X - clickedBox.Left) * scaleX),
            (int)((clickPoint.Y - clickedBox.Top) * scaleY)
        );

        if (IsClickedOnDifference(imagePoint))
        {
            foundDifferences++;
            MessageBox.Show("You found a difference! " + foundDifferences + " out of " + differences + " found.");

            // Redraw boundaries after finding a difference
            DrawBoundaries();
            if (showingBoundaries)
            {
                UpdateImageDisplay();
            }

            if (foundDifferences >= differences)
            {
                MessageBox.Show("Congratulations! You've found all the differences!");
                this.Close();
            }
        }
    }

    private bool IsClickedOnDifference(Point clickPoint)
    {
        foreach (Rectangle region in differenceRegions.ToList())
        {
            if (region.Contains(clickPoint))
            {
                // Remove the found difference from the list to prevent double-counting
                differenceRegions.Remove(region);
                return true;
            }
        }
        return false;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        // Dispose of Emgu CV resources
        if (image1Mat != null) image1Mat.Dispose();
        if (image2Mat != null) image2Mat.Dispose();
        if (image1WithBoundaries != null) image1WithBoundaries.Dispose();
        if (image2WithBoundaries != null) image2WithBoundaries.Dispose();
        if (binaryDiffImage != null) binaryDiffImage.Dispose();
    }
}
