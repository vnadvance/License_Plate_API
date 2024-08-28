using License_Plate_API.Model;
using License_Plate_API.Utils;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Net.Mime;
using System.Reflection;

namespace License_Plate_API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MainController : ControllerBase
    {
        private readonly ILogger<MainController> _logger;
        private readonly Yolov5DetectModel _detectModel;
        private readonly Yolov5OCRModel _ocrModel;
        private readonly IWebHostEnvironment _currentEnvironment;
        private static string _path = "";
        public MainController(ILogger<MainController> logger, Yolov5DetectModel detectModel,Yolov5OCRModel ocrModel, IWebHostEnvironment currentEnvironment)
        {
            _logger = logger;
            _detectModel = detectModel;
            _ocrModel = ocrModel;
            _currentEnvironment = currentEnvironment;

            if (_path == string.Empty)
            {
                _path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            }
        }
        [HttpPost("predict")]
        public async Task<ResponseModel> predict(IFormFile file, [FromForm] string gatename, [FromForm] string computername)
        {
            try
            {
                //string DomainName = _currentEnvironment.IsDevelopment() ?  HttpContext.Request.GetDisplayUrl().Replace("/predict","") : "";
                string DomainName = HttpContext.Request.GetDisplayUrl().Replace("/predict", "");
                if (file == null)
                {
                    return new ResponseModelError("Vui lòng nhập file");
                }
                List<ResultModel> results = new List<ResultModel>();
                var image = await Image.LoadAsync<Rgba32>(file.OpenReadStream());
                DateTime now = DateTime.Now;
                string savePath = string.Join(Path.DirectorySeparatorChar,
                    _path,
                    gatename,
                    computername,
                    now.ToString("yyyy"),
                    now.ToString("MM"),
                    now.ToString("dd"),
                    "");
                Directory.CreateDirectory(savePath);
                string randomName = Utility.RandomString(20) + ".jpg";
                _ = image.SaveAsJpegAsync(savePath + randomName);
                var listPredict = _detectModel.Predict(image);
                Parallel.ForEach(listPredict, (prediction,state,index) =>
                {
                    double score = Math.Round(prediction.Score, 2);
                    var labelRect = prediction.Rectangle;
                    var twoLayers = (labelRect.Height / labelRect.Width) > 0.5;

                    var cropImage = image.Clone();
                    cropImage.Mutate(ctx => ctx.Crop(new Rectangle(
                        (int)labelRect.X, 
                        (int)labelRect.Y, 
                        (int)labelRect.Width, 
                        (int)labelRect.Height)));

                    var yoloOcrpredictions = _ocrModel.Predict(cropImage, 0.6f);

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
                    string file = $"{savePath}{licensePlate}_{randomName}";
                    _ = cropImage.SaveAsJpegAsync(file);
                    results.Add(new ResultModel
                    {
                        Predict = licensePlate,
                        ImagePath = DomainName + file.Replace(_path, "").Replace(Path.DirectorySeparatorChar, '/')
                    });
                });
                string newName = savePath + string.Join('-', results.Select(t=>t.Predict)) + "_master_" + randomName;
                System.IO.File.Move(savePath + randomName, newName);
                return new ResponseModelSuccess("", results, DomainName + newName.Replace(_path, "").Replace(Path.DirectorySeparatorChar, '/'));
            }
            catch(Exception e)
            {
                return new ResponseModelError("Error " + e.Message);
            }
        }

        [HttpGet("{gatename}/{computername}/{year}/{month}/{day}/{filename}")]
        public IActionResult getFile(string gatename,string computername,string year,string month,string day,string filename)
        {
            string filePath = string.Join(Path.DirectorySeparatorChar,_path, gatename, computername,year,month,day, filename);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }
            var filestream = System.IO.File.OpenRead(filePath);
            return File(filestream, "image/jpeg",  enableRangeProcessing: true);
        }
    }
}
