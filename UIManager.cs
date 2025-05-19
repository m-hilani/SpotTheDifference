using System.Drawing;
using Emgu.CV;

namespace SpotTheDifference
{
    public class UIManager
    {
        // UI components
        public PictureBox PictureBox1 { get; private set; }
        public PictureBox PictureBox2 { get; private set; }
        public FlowLayoutPanel ButtonPanel { get; private set; }
        public Button ShowBoundariesButton { get; private set; }
        public Button ShowBinaryButton { get; private set; }
        public Button SaveBinaryButton { get; private set; }
        
        // Display state
        public bool ShowingBoundaries { get; set; } = false;
        public bool ShowingBinaryDiff { get; set; } = false;
        
        // Parent form
        private Form parentForm;
        
        public UIManager(Form parentForm)
        {
            this.parentForm = parentForm;
            InitializeComponents();
            SetupLayout();
        }
        
        private void InitializeComponents()
        {
            // Create PictureBoxes
            PictureBox1 = new PictureBox
            {
                Size = new Size(500, 500),
                Location = new Point(50, 50),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle
            };

            PictureBox2 = new PictureBox
            {
                Size = new Size(500, 500),
                Location = new Point(600, 50),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle
            };
            
            // Create button panel
            ButtonPanel = new FlowLayoutPanel
            {
                Location = new Point(50, 560),
                Size = new Size(1100, 40),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Visible = true,
                BackColor = Color.LightGray // Making it visible for debugging
            };
            
            // Create buttons
            ShowBoundariesButton = new Button
            {
                Text = "Show/Hide Boundaries",
                Size = new Size(160, 30),
                Margin = new Padding(10, 0, 10, 0),
                Visible = true
            };
            
            ShowBinaryButton = new Button
            {
                Text = "Show/Hide Binary Difference",
                Size = new Size(180, 30),
                Margin = new Padding(10, 0, 10, 0),
                Visible = true
            };
            
            SaveBinaryButton = new Button
            {
                Text = "Save Binary Image",
                Size = new Size(160, 30),
                Margin = new Padding(10, 0, 10, 0),
                Visible = true
            };
        }
        
        private void SetupLayout()
        {
            // First, clear any existing controls to avoid duplicates
            parentForm.Controls.Clear();
            
            // Configure form
            parentForm.Size = new Size(1200, 650);
            parentForm.Text = "Spot the Difference Game";
            
            // Add buttons to panel
            ButtonPanel.Controls.Clear(); // Clear first to ensure no duplicates
            ButtonPanel.Controls.Add(ShowBoundariesButton);
            ButtonPanel.Controls.Add(ShowBinaryButton);
            ButtonPanel.Controls.Add(SaveBinaryButton);
            
            // Add controls to form in the correct order
            parentForm.Controls.Add(PictureBox1);
            parentForm.Controls.Add(PictureBox2);
            parentForm.Controls.Add(ButtonPanel);
            
            // Ensure buttons are visible
            ButtonPanel.BringToFront();
        }
        
        public void UpdateDisplay(GameState gameState)
        {
            if (ShowingBinaryDiff && gameState.BinaryDiffImage != null)
            {
                PictureBox1.Image = gameState.BinaryDiffImage.ToBitmap();
                PictureBox2.Image = gameState.BinaryDiffImage.ToBitmap();
            }
            else if (ShowingBoundaries)
            {
                PictureBox1.Image = gameState.Image1WithBoundaries?.ToBitmap();
                PictureBox2.Image = gameState.Image2WithBoundaries?.ToBitmap();
            }
            else
            {
                PictureBox1.Image = gameState.Image1?.ToBitmap();
                PictureBox2.Image = gameState.Image2?.ToBitmap();
            }
        }
        
        public Point GetImageCoordinates(Point clickPoint, PictureBox pictureBox)
        {
            if (pictureBox.Image == null)
                return Point.Empty;
                
            // Handle Zoom mode by calculating the scaled image position within the PictureBox
            float imageRatio = (float)pictureBox.Image.Width / pictureBox.Image.Height;
            float boxRatio = (float)pictureBox.Width / pictureBox.Height;
            
            int imageWidth, imageHeight;
            int imageX, imageY;
            
            if (imageRatio > boxRatio) // Image is wider than box (horizontal letterboxing)
            {
                imageWidth = pictureBox.Width;
                imageHeight = (int)(pictureBox.Width / imageRatio);
                imageX = 0;
                imageY = (pictureBox.Height - imageHeight) / 2;
            }
            else // Image is taller than box (vertical letterboxing)
            {
                imageHeight = pictureBox.Height;
                imageWidth = (int)(pictureBox.Height * imageRatio);
                imageY = 0;
                imageX = (pictureBox.Width - imageWidth) / 2;
            }
            
            // Check if click is within the image area
            if (clickPoint.X < imageX || clickPoint.Y < imageY || 
                clickPoint.X >= imageX + imageWidth || clickPoint.Y >= imageY + imageHeight)
            {
                return Point.Empty; // Click outside the actual image area
            }
            
            // Map from PictureBox coordinates to image coordinates
            float scaleX = (float)pictureBox.Image.Width / imageWidth;
            float scaleY = (float)pictureBox.Image.Height / imageHeight;
            
            int imagePointX = (int)((clickPoint.X - imageX) * scaleX);
            int imagePointY = (int)((clickPoint.Y - imageY) * scaleY);
            
            return new Point(imagePointX, imagePointY);
        }
        
        public void SaveBinaryImage(Mat binaryDiffImage)
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
    }
} 