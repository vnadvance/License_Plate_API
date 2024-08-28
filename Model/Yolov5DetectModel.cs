using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace License_Plate_API.Model
{
    public class Yolov5DetectModel : Yolov5BaseModel
    {
        public Yolov5DetectModel() : base(Properties.Resources.LP_detector, "images")
        {            
        }

    }

    
}
