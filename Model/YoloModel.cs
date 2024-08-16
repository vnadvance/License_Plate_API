namespace License_Plate_API.Model
{
    public class YoloModel
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int Depth { get; set; }

        public int Dimensions { get; set; }
        public float Confidence { get; set; } = 0.20f;
        public float MulConfidence { get; set; } = 0.25f;
        public float Overlap { get; set; } = 0.45f;
        public string[] Outputs { get; set; } = new string[0];
        public List<YoloLabel> Labels { get; set; } = new List<YoloLabel>();
        public bool UseDetect { get; set; }
    }
}
