using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace License_Plate_API.Model
{
    public class Yolov5OCRModel  : Yolov5BaseModel
    {
        public Yolov5OCRModel(): base(Properties.Resources.LP_ocr, "images")
        {
            _model.Labels.Clear();
            SetupLabels(new string[] { 
                "1", "2", "3", "4", "5", "6", "7", "8", "9", 
                "A", "B", "C", "D", "E", "F", "G", "H", "K",
                "L", "M", "N", "P", "S", "T", "U", "V", "X", "Y", "Z", "0" });
        }


    }
}
