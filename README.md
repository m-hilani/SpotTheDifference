# Spot The Difference Game

A simple Windows Forms application that lets you play a "spot the difference" game between two images.

## Setup

1. Make sure you have .NET 8.0 SDK installed
2. Build the project:
   ```
   dotnet build
   ```

## Running the Game

1. Place two images in the output directory (`bin/Debug/net8.0-windows/`):

   - `image1.jpg` - The first image
   - `image2.jpg` - The second image with some differences

2. Run the program:
   ```
   dotnet run
   ```

## How to Play

1. The program will show both images side by side
2. Click on spots where you notice differences between the images
3. The program will tell you when you've found a difference
4. Keep finding differences until you've found them all!

## Image Requirements

- Both images must be the same size
- Images should be in JPG format
- Name them exactly `image1.jpg` and `image2.jpg`
- Place them in the same directory as the executable

## Troubleshooting

If the program doesn't detect differences well:

1. Make sure your images are clear and have visible differences
2. The differences should be larger than a few pixels
3. Both images should be properly aligned
