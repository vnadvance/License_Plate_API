using Microsoft.ML.OnnxRuntime.Tensors;
using System.Drawing;

namespace License_Plate_API.Model
{
    public class YoloPrediction
    {
        public YoloLabel? Label { get; set; }
        public RectangleF Rectangle { get; set; }
        public float Score { get; set; }
        public float xC { get; set; }
        public float yC { get; set; }
        public YoloPrediction() { }

        public YoloPrediction(YoloLabel label, float confidence) : this(label)
        {
            Score = confidence;
        }

        public YoloPrediction(YoloLabel label)
        {
            Label = label;
        }
    }
}
