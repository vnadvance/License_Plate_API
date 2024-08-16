using License_Plate_API.Model;
using Microsoft.AspNetCore.Mvc;
using System.Drawing;

namespace License_Plate_API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MainController : ControllerBase
    {
        private readonly ILogger<MainController> _logger;
        private readonly Yolov5DetectModel _detectModel;
        private readonly Yolov5OCRModel _ocrModel;
        public MainController(ILogger<MainController> logger, Yolov5DetectModel detectModel,Yolov5OCRModel ocrModel)
        {
            _logger = logger;
            _detectModel = detectModel;
            _ocrModel = ocrModel;
        }
        [HttpPost("predict")]
        public ResponseModel predictAsync(IFormFile file)
        {
            try
            {
                if (file == null)
                {
                    return new ResponseModelError("Vui lòng nhập file");
                }
                List<string> results = new List<string>();
                var image = Image.FromStream(file.OpenReadStream());
                var listPredict = _detectModel.Predict(image);
                foreach (var prediction in listPredict)
                {
                    double score = Math.Round(prediction.Score, 2);
                    var labelRect = prediction.Rectangle;
                    var twoLayers = (labelRect.Height / labelRect.Width) > 0.5;
                    Rectangle cropArea = new Rectangle((int)labelRect.X < 0 ? 0 : (int)labelRect.X, (int)labelRect.Y < 0 ? 0 : (int)labelRect.Y, (int)labelRect.Width, (int)labelRect.Height);
                    Bitmap bmpImage = new Bitmap(image);
                    Bitmap bmpCrop = bmpImage.Clone(cropArea, bmpImage.PixelFormat);

                    if (twoLayers)
                    {
                        var img_upper_H = labelRect.Height / 2;

                        var width = (int)labelRect.Width + 1;
                        var height = (int)img_upper_H;
                        Bitmap resultBitmap = new Bitmap(width, height);
                        using (Graphics g = Graphics.FromImage(resultBitmap))
                        {
                            Rectangle resultRectangle = new Rectangle(0, 0, width, height);
                            Rectangle sourceRectangle = new Rectangle(0, 0, width, height);
                            g.DrawImage(bmpCrop, resultRectangle, sourceRectangle, GraphicsUnit.Pixel);
                        }

                        Bitmap resultBitmap1 = new Bitmap(width, height);
                        using (Graphics g = Graphics.FromImage(resultBitmap1))
                        {
                            Rectangle resultRectangle = new Rectangle(0, 0, width, height);
                            Rectangle sourceRectangle = new Rectangle(0, height, width, height);
                            g.DrawImage(bmpCrop, resultRectangle, sourceRectangle, GraphicsUnit.Pixel);
                        }

                        bmpCrop = JoinImage(resultBitmap, resultBitmap1);


                    }

                    var yoloOcrpredictions = _ocrModel.Predict(bmpCrop);




                    results.Add(yoloOcrpredictions ?? "unknow");
                }
                return new ResponseModelSuccess("", results);


            }
            catch(Exception e)
            {
                return new ResponseModelError("Error " + e.Message);

            }
        }



        private static Bitmap JoinImage(Image sourceImg, Image newImg)
        {
            int imgHeight = 0, imgWidth = 0;

            imgWidth = sourceImg.Width + newImg.Width;
            imgHeight = sourceImg.Height > newImg.Height ? sourceImg.Height : newImg.Height;

            Bitmap joinedBitmap = new Bitmap(imgWidth, imgHeight);
            using (Graphics graph = Graphics.FromImage(joinedBitmap))
            {
                graph.DrawImage(sourceImg, 0, 0, sourceImg.Width, sourceImg.Height);

                graph.DrawImage(newImg, sourceImg.Width, 0, newImg.Width, newImg.Height);
            }
            return joinedBitmap;
        }
    }
}
