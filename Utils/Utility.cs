using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;

namespace License_Plate_API.Utils
{
    public static class Utility
    {
        private static Random random = new Random();
        public static Image<Rgba32> ResizeImage(Image<Rgba32> image, int targetWidth, int targetHeight)
        {
            // Calculate the resize ratio
            var (w, h) = (image.Width, image.Height); // image width and height
            var (xRatio, yRatio) = (targetWidth / (float)w, targetHeight / (float)h); // x, y ratios
            var ratio = Math.Min(xRatio, yRatio); // ratio = resized / original
            var (width, height) = ((int)(w * ratio), (int)(h * ratio)); // new width and height
            var (x, y) = ((targetWidth / 2) - (width / 2), (targetHeight / 2) - (height / 2)); // x and y coordinates

            // Resize and pad the image to the target size
            image.Mutate(ctx => ctx
                .Resize(new ResizeOptions
                {
                    Size = new SixLabors.ImageSharp.Size(width, height),
                    Mode = ResizeMode.Max // Maintain aspect ratio
                })
                .Pad(targetWidth, targetHeight, SixLabors.ImageSharp.Color.Transparent)); // Padding to center the image

            return image;
        }
        public static (float a, float b) LinearEquation(float x1, float y1, float x2, float y2)
        {
            float b = y1 - (y2 - y1) * x1 / (x2 - x1);
            float a = (y1 - b) / x1;
            return (a, b);
        }

        public static bool CheckPointLinear(float x, float y, float x1, float y1, float x2, float y2)
        {
            var (a, b) = LinearEquation(x1, y1, x2, y2);
            float yPred = a * x + b;
            return Math.Abs(yPred - y) <= 5; // 3 nếu model lớn
        }
        public static string RandomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
