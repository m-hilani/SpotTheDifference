using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace SpotTheDifference
{
    public class ImageProcessor
    {
        // Process images and find differences
        public static (List<Rectangle> differenceRegions, Mat binaryDiffImage) FindDifferences(Mat image1Mat, Mat image2Mat, int desiredDifferences = 7)
        {
            List<Rectangle> differenceRegions = new List<Rectangle>();
            
            // Convert images to grayscale
            Mat gray1 = new Mat();
            Mat gray2 = new Mat();
            CvInvoke.CvtColor(image1Mat, gray1, ColorConversion.Bgr2Gray);
            CvInvoke.CvtColor(image2Mat, gray2, ColorConversion.Bgr2Gray);

            // // Apply Gaussian blur to reduce noise
            // CvInvoke.GaussianBlur(gray1, gray1, new Size(3, 3), 0);
            // CvInvoke.GaussianBlur(gray2, gray2, new Size(3, 3), 0);

            // Calculate absolute difference between images
            Mat diffImage = new Mat();
            CvInvoke.AbsDiff(gray1, gray2, diffImage);

            // Apply threshold to get binary image
            Mat binaryDiffImage = new Mat();
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


                // Take only the top N largest and most significant differences
                for (int i = 0; i < Math.Min(desiredDifferences, mergedContours.Count); i++)
                {
                    differenceRegions.Add(mergedContours[i].box);
                }
            }

            // Dispose of temporary Mats
            gray1.Dispose();
            gray2.Dispose();
            diffImage.Dispose();
            thresholdImage.Dispose();
            
            return (differenceRegions, binaryDiffImage);
        }

        // Draw boundaries around difference regions
        public static void DrawBoundaries(Mat outputImage, List<Rectangle> differenceRegions)
        {
            foreach (Rectangle rect in differenceRegions)
            {
                // Draw red rectangle (BGR format in OpenCV)
                CvInvoke.Rectangle(outputImage, rect, new MCvScalar(0, 0, 255), 2);
                
                // Add number labels to the rectangles
                Point textPoint = new Point(rect.X, rect.Y - 10);
                int index = differenceRegions.IndexOf(rect) + 1;
                CvInvoke.PutText(outputImage, index.ToString(), textPoint,
                    FontFace.HersheyComplex, 1, new MCvScalar(0, 0, 255), 2);
            }
        }
    }
} 