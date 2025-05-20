using System.Drawing.Imaging;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace SpotTheDifference;

public partial class Form1 : Form
{
    private GameState gameState;
    private UIManager uiManager;
    
    public Form1()
    {
        InitializeComponent();
        InitializeGame();
    }

    private void InitializeGame()
    {
        // Initialize UI manager
        uiManager = new UIManager(this);
        
        // Setup event handlers
        uiManager.PictureBox1.MouseDown += PictureBox_MouseDown;
        uiManager.PictureBox2.MouseDown += PictureBox_MouseDown;
        uiManager.ShowBinaryButton.Click += ShowBinaryButton_Click;
        uiManager.SaveBinaryButton.Click += SaveBinaryButton_Click;
        
        // Add Select Images button
        Button selectImagesButton = new Button
        {
            Text = "Select Images",
            Location = new Point(10, 10),
            Size = new Size(100, 30)
        };
        selectImagesButton.Click += SelectImagesButton_Click;
        this.Controls.Add(selectImagesButton);
        
        // Hide the Show Boundaries button
        uiManager.ShowBoundariesButton.Visible = false;
        
        // Initialize game state with image paths
        gameState = new GameState("2.jpg", "2d.jpg");
        
        // Update display
        uiManager.UpdateDisplay(gameState);
    }

    private void SelectImagesButton_Click(object sender, EventArgs e)
    {
        using (OpenFileDialog openFileDialog = new OpenFileDialog())
        {
            openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";
            openFileDialog.Title = "Select Original Image";
            openFileDialog.Multiselect = false;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string originalImagePath = openFileDialog.FileName;
                
                // Open second dialog for modified image
                openFileDialog.Title = "Select Modified Image";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string modifiedImagePath = openFileDialog.FileName;
                    
                    // Dispose old game state
                    gameState?.Dispose();
                    
                    // Create new game state with selected images
                    gameState = new GameState(originalImagePath, modifiedImagePath);
                    
                    // Update display
                    uiManager.UpdateDisplay(gameState);
                }
            }
        }
    }

    private void ShowBinaryButton_Click(object sender, EventArgs e)
    {
        uiManager.ShowingBinaryDiff = !uiManager.ShowingBinaryDiff;
        
        // Turn off boundaries if binary diff is being shown
        if (uiManager.ShowingBinaryDiff)
            uiManager.ShowingBoundaries = false;
            
        uiManager.UpdateDisplay(gameState);
    }

    private void SaveBinaryButton_Click(object sender, EventArgs e)
    {
        uiManager.SaveBinaryImage(gameState.BinaryDiffImage);
    }

    private void PictureBox_MouseDown(object sender, MouseEventArgs e)
    {
        // Get the mouse position directly from the event args
        PictureBox clickedBox = (PictureBox)sender;
        Point clickPoint = e.Location;
        
        // Convert to image coordinates
        Point imagePoint = uiManager.GetImageCoordinates(clickPoint, clickedBox);
        
        // Check if clicked on a difference
        if (gameState.CheckForDifference(imagePoint))
        {
            // Show boundaries for found differences
            uiManager.ShowingBoundaries = true;
            uiManager.UpdateDisplay(gameState);
            
            MessageBox.Show($"You found a difference! {gameState.FoundDifferences} out of {gameState.TotalDifferences} found.");
            
            // Check if all differences have been found
            if (gameState.AllDifferencesFound())
            {
                MessageBox.Show("Congratulations! You've found all the differences!");
                this.Close();
            }
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        
        // Clean up resources
        gameState?.Dispose();
    }
}
