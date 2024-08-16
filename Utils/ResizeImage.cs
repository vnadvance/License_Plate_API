using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing;

namespace License_Plate_API.Utils
{
    public static class ImageUtils
    {
        public static Bitmap ResizeImage(Image image, int target_width, int target_height)
        {
            PixelFormat format = image.PixelFormat;

            var output = new Bitmap(target_width, target_height, format);

            var (w, h) = (image.Width, image.Height); // image width and height
            var (xRatio, yRatio) = (target_width / (float)w, target_height / (float)h); // x, y ratios
            var ratio = Math.Min(xRatio, yRatio); // ratio = resized / original
            var (width, height) = ((int)(w * ratio), (int)(h * ratio)); // roi width and height
            var (x, y) = ((target_width / 2) - (width / 2), (target_height / 2) - (height / 2)); // roi x and y coordinates
            var roi = new Rectangle(x, y, width, height); // region of interest

            using (var graphics = Graphics.FromImage(output))
            {
                graphics.Clear(Color.FromArgb(0, 0, 0, 0)); // clear canvas

                graphics.SmoothingMode = SmoothingMode.None; // no smoothing
                graphics.InterpolationMode = InterpolationMode.Bilinear; // bilinear interpolation
                graphics.PixelOffsetMode = PixelOffsetMode.Half; // half pixel offset

                graphics.DrawImage(image, roi); // draw scaled
            }

            return output;
        }
    }
}
