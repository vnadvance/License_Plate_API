using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace License_Plate_API.Utils
{
    public class Utility
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
        public static Bitmap CropImage(Image image, Rectangle cropArea)
        {
            lock (image)
            {
                Bitmap bmpCrop = new Bitmap(cropArea.Width, cropArea.Height);
                using (Graphics g = Graphics.FromImage(bmpCrop))
                {
                    g.DrawImage(image, new Rectangle(0, 0, bmpCrop.Width, bmpCrop.Height), cropArea, GraphicsUnit.Pixel);
                }

                return bmpCrop;
            }
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
    }
}
