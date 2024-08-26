using License_Plate_API.Model;
using License_Plate_API.Utils;
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
        public ResponseModel predictAsync(IFormFile file, [FromForm] string gatename, [FromForm] string computername)
        {
            try
            {
                if (file == null)
                {
                    return new ResponseModelError("Vui lòng nhập file");
                }
                List<string> results = new List<string>();
                var stream = file.OpenReadStream();
                var image = Image.FromStream(stream);
                var listPredict = _detectModel.Predict(image);
                var mem = new MemoryStream();
                _ = Task.Run(() =>
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.CopyTo(mem);
                });
                Parallel.ForEach(listPredict, (prediction) =>
                {
                    double score = Math.Round(prediction.Score, 2);
                    var labelRect = prediction.Rectangle;
                    var twoLayers = (labelRect.Height / labelRect.Width) > 0.5;
                    Rectangle cropArea = new Rectangle((int)labelRect.X < 0 ? 0 : (int)labelRect.X, (int)labelRect.Y < 0 ? 0 : (int)labelRect.Y, (int)labelRect.Width, (int)labelRect.Height);
                    //  Bitmap bmpImage = new Bitmap(image);
                    //TODO: change image library for support linux and faster crop
                    Bitmap bmpCrop = Utility.CropImage(image, cropArea);//  bmpImage.Clone(cropArea, bmpImage.PixelFormat); 
                    var yoloOcrpredictions = _ocrModel.Predict(bmpCrop, 0.5f);

                    if (yoloOcrpredictions.Count == 0 || yoloOcrpredictions.Count < 7 || yoloOcrpredictions.Count > 10)
                    {
                        return;
                    }
                    List<(float[] x, string a)> centerList = new List<(float[], string)>();
                    float ySum = 0;
                    foreach (var bb in yoloOcrpredictions)
                    {
                        ySum += bb.yC;
                        centerList.Add((new float[] { bb.xC, bb.yC }, bb.Label?.Name ?? ""));
                    }
                    string LPType = "1";
                    var lPoint = centerList[0];
                    var rPoint = centerList[0];

                    foreach (var cp in centerList)
                    {
                        if (cp.x[0] < lPoint.x[0])
                        {
                            lPoint = cp;
                        }
                        if (cp.x[0] > rPoint.x[0])
                        {
                            rPoint = cp;
                        }
                        if (lPoint.x[0] != rPoint.x[0] && !Utility.CheckPointLinear(cp.x[0], cp.x[1], lPoint.x[0], lPoint.x[1], rPoint.x[0], rPoint.x[1]))
                        {
                            LPType = "2";
                        }
                    }

                    int yMean = (int)(ySum / yoloOcrpredictions.Count);

                    List<(float[] x, string a)> line1 = new List<(float[] x, string a)>();
                    List<(float[] x, string a)> line2 = new List<(float[] x, string a)>();
                    string licensePlate = "";

                    if (LPType == "2")
                    {
                        foreach (var c in centerList)
                        {
                            if (c.x[1] > yMean)
                            {
                                line2.Add(c);
                            }
                            else
                            {
                                line1.Add(c);
                            }
                        }

                        foreach (var l1 in line1.OrderBy(x => x.x[0]))
                        {
                            licensePlate += l1.a;
                        }
                        foreach (var l2 in line2.OrderBy(x => x.x[0]))
                        {
                            licensePlate += l2.a;
                        }
                    }
                    else
                    {
                        foreach (var l in centerList.OrderBy(x => x.x[0]))
                        {
                            licensePlate += l.a;
                        }
                    }
                    results.Add(licensePlate);
                });
                _ = Task.Run(() =>
                {
                    Utility.saveImage(mem.ToArray(), 
                        gatename, 
                        computername, 
                        string.Join("_", results) + Path.GetExtension(file.FileName));
                });
                return new ResponseModelSuccess("", results);
            }
            catch(Exception e)
            {
                return new ResponseModelError("Error " + e.Message);

            }
        }
    }
}
