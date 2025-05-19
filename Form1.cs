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
        uiManager.ShowBoundariesButton.Click += ShowBoundariesButton_Click;
        uiManager.ShowBinaryButton.Click += ShowBinaryButton_Click;
        uiManager.SaveBinaryButton.Click += SaveBinaryButton_Click;
        
        // Initialize game state with image paths
        gameState = new GameState("2.jpg", "2d.jpg");
        
        // Update display
        uiManager.UpdateDisplay(gameState);
    }

    private void ShowBoundariesButton_Click(object sender, EventArgs e)
    {
        uiManager.ShowingBoundaries = !uiManager.ShowingBoundaries;
        
        // Turn off binary diff view if boundaries are being shown
        if (uiManager.ShowingBoundaries)
            uiManager.ShowingBinaryDiff = false;
            
        uiManager.UpdateDisplay(gameState);
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
            MessageBox.Show($"You found a difference! {gameState.FoundDifferences} out of {gameState.TotalDifferences} found.");
            
            // Update the display if showing boundaries
            if (uiManager.ShowingBoundaries)
            {
                uiManager.UpdateDisplay(gameState);
            }
            
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
