using System.Drawing;
using Emgu.CV;
using System.Media;

namespace SpotTheDifference
{
    public class GameState
    {
        // Game state properties
        public int TotalDifferences { get; private set; }
        public int FoundDifferences { get; private set; }
        public List<Rectangle> DifferenceRegions { get; private set; }
        public List<Rectangle> FoundDifferenceRegions { get; private set; }
        
        // Images
        public Mat Image1 { get; private set; }
        public Mat Image2 { get; private set; }
        public Mat Image1WithBoundaries { get; private set; }
        public Mat Image2WithBoundaries { get; private set; }
        public Mat BinaryDiffImage { get; private set; }

        // Sound player
        private SoundPlayer successSound;
        
        public GameState(string firstImagePath, string secondImagePath)
        {
            FoundDifferences = 0;
            DifferenceRegions = new List<Rectangle>();
            FoundDifferenceRegions = new List<Rectangle>();
            
            // Initialize sound player
            try
            {
                successSound = new SoundPlayer("Resources/success.wav");
                successSound.Load();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not load sound file: {ex.Message}");
            }
            
            LoadImages(firstImagePath, secondImagePath);
            ProcessImages();
        }

        private void LoadImages(string firstImagePath, string secondImagePath)
        {
            // Load images using Emgu CV
            Image1 = CvInvoke.Imread(firstImagePath, Emgu.CV.CvEnum.ImreadModes.Color);
            Image2 = CvInvoke.Imread(secondImagePath, Emgu.CV.CvEnum.ImreadModes.Color);
            
            // Create copies for drawing boundaries
            Image1WithBoundaries = Image1.Clone();
            Image2WithBoundaries = Image2.Clone();
        }
        
        private void ProcessImages()
        {
            // Find differences between images
            (DifferenceRegions, BinaryDiffImage) = ImageProcessor.FindDifferences(Image1, Image2);
            TotalDifferences = DifferenceRegions.Count;
            
            // Draw boundaries on the copies
            UpdateBoundaryImages();
        }
        
        public void UpdateBoundaryImages()
        {
            // Create fresh copies of the original images
            Image1WithBoundaries = Image1.Clone();
            Image2WithBoundaries = Image2.Clone();
            
            // Draw only found differences
            ImageProcessor.DrawBoundaries(Image1WithBoundaries, FoundDifferenceRegions);
            ImageProcessor.DrawBoundaries(Image2WithBoundaries, FoundDifferenceRegions);
        }
        
        public bool CheckForDifference(Point clickPoint)
        {
            // Skip invalid clicks
            if (clickPoint == Point.Empty)
                return false;
                
            // Debug info
            Console.WriteLine($"Click at {clickPoint.X}, {clickPoint.Y}");
            Console.WriteLine($"Checking against {DifferenceRegions.Count} regions");
            
            foreach (Rectangle region in DifferenceRegions.ToList())
            {
                Console.WriteLine($"Checking region: {region}");
                if (region.Contains(clickPoint))
                {
                    Console.WriteLine("FOUND DIFFERENCE!");
                    // Play success sound
                    try
                    {
                        successSound?.Play();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Could not play sound: {ex.Message}");
                    }

                    // Remove the found difference from the list to prevent double-counting
                    DifferenceRegions.Remove(region);
                    // Add to found differences list
                    FoundDifferenceRegions.Add(region);
                    FoundDifferences++;
                    UpdateBoundaryImages();
                    return true;
                }
            }
            return false;
        }
        
        public bool AllDifferencesFound()
        {
            return FoundDifferences >= TotalDifferences;
        }
        
        public void Dispose()
        {
            Image1?.Dispose();
            Image2?.Dispose();
            Image1WithBoundaries?.Dispose();
            Image2WithBoundaries?.Dispose();
            BinaryDiffImage?.Dispose();
            successSound?.Dispose();
        }
    }
} 