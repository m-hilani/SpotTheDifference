using System.Drawing;
using Emgu.CV;
using System.Media;
using Emgu.CV.Structure;

namespace SpotTheDifference
{
    public class GameState
    {
        // Game state properties
        public int TotalDifferences { get; private set; }
        public int FoundDifferences { get; private set; }
        public int WrongAttempts { get; private set; }
        public int MaxWrongAttempts { get; private set; }
        public List<Rectangle> DifferenceRegions { get; private set; }
        public List<Rectangle> FoundDifferenceRegions { get; private set; }
        public List<Rectangle> WrongAttemptRegions { get; private set; }
        
        // Images
        public Mat Image1 { get; private set; }
        public Mat Image2 { get; private set; }
        public Mat Image1WithBoundaries { get; private set; }
        public Mat Image2WithBoundaries { get; private set; }
        public Mat BinaryDiffImage { get; private set; }

        // Sound players
        private SoundPlayer successSound;
        private SoundPlayer wrongSound;
        
        public GameState(string firstImagePath, string secondImagePath)
        {
            FoundDifferences = 0;
            WrongAttempts = 0;
            MaxWrongAttempts = 5; // Set maximum wrong attempts
            DifferenceRegions = new List<Rectangle>();
            FoundDifferenceRegions = new List<Rectangle>();
            WrongAttemptRegions = new List<Rectangle>();
            
            // Initialize sound players
            try
            {
                successSound = new SoundPlayer("Resources/success.wav");
                successSound.Load();
                wrongSound = new SoundPlayer("Resources/wrong.wav");
                wrongSound.Load();
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
            
            // Draw found differences
            ImageProcessor.DrawBoundaries(Image1WithBoundaries, FoundDifferenceRegions);
            ImageProcessor.DrawBoundaries(Image2WithBoundaries, FoundDifferenceRegions);

            // Draw wrong attempt regions with a different color
            foreach (var region in WrongAttemptRegions)
            {
                // Create a smaller rectangle for wrong attempts
                int padding = 10;
                Rectangle smallerRegion = new Rectangle(
                    region.X + padding,
                    region.Y + padding,
                    region.Width - (padding * 2),
                    region.Height - (padding * 2)
                );
                
                // Draw wrong attempt regions in red
                CvInvoke.Rectangle(Image1WithBoundaries, smallerRegion, new Bgr(0, 0, 255).MCvScalar, 2);
                CvInvoke.Rectangle(Image2WithBoundaries, smallerRegion, new Bgr(0, 0, 255).MCvScalar, 2);
            }
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

            // Wrong attempt
            WrongAttempts++;
            // Play wrong sound
            try
            {
                wrongSound?.Play();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not play sound: {ex.Message}");
            }

            // Add wrong attempt region
            WrongAttemptRegions.Add(new Rectangle(clickPoint.X - 20, clickPoint.Y - 20, 40, 40));
            UpdateBoundaryImages();

            return false;
        }
        
        public bool HasExceededWrongAttempts()
        {
            return WrongAttempts >= MaxWrongAttempts;
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
            wrongSound?.Dispose();
        }
    }
} 