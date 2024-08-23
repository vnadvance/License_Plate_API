using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using License_Plate_API.Utils;
using System.Drawing;
using OpenCvSharp.Extensions;
using OpenCvSharp;
using System.Drawing.Imaging;

namespace License_Plate_API.Model
{
    public abstract class Yolov5BaseModel : IDisposable
    {
        protected readonly InferenceSession _inferenceSession;
        protected string _inputName = "";
        protected YoloModel _model = new YoloModel();

        public void SetupLabels(string[] labels)
        {
            labels.Select((s, i) => new { i, s }).ToList().ForEach(item =>
            {
                _model.Labels.Add(new YoloLabel { Id = item.i, Name = item.s });
            });
        }

        public DenseTensor<float>[] Inference(Image img)
        {
            Bitmap resized;
            if (img.Width != _model.Width || img.Height != _model.Height)
            {
                resized = Utility.ResizeImage(img, _model.Width, _model.Height); // fit image size to specified input size
            }
            else
            {
                resized = new Bitmap(img);
            }
            var inputs = new List<NamedOnnxValue> // add image as input
            {
                NamedOnnxValue.CreateFromTensor(_inputName, ExtractPixels(resized))
            };

            var result = _inferenceSession.Run(inputs); // run inference

            var output = new List<DenseTensor<float>>();

            foreach (var item in _model.Outputs!) // add outputs for processing
            {

                output.Add(result.First(x => x.Name == item).Value as DenseTensor<float>);
            };

            return output.ToArray();
        }
        protected List<YoloPrediction> Supress(List<YoloPrediction> items)
        {
            var result = new List<YoloPrediction>(items);
            foreach (var item in items)
            {
                foreach (var current in result.ToList())
                {
                    if (current == item) continue;

                    var (rect1, rect2) = (item.Rectangle, current.Rectangle);


                    RectangleF intersection = RectangleF.Intersect(rect1, rect2);

                    float intArea = intersection.Area();

                    float unionArea = rect1.Area() + rect2.Area() - intArea;

                    float overlap = intArea / unionArea;

                    if (overlap >= _model.Overlap)
                    {
                        if (item.Score >= current.Score)
                        {
                            result.Remove(current);
                        }
                    }
                }
            }
            return result;
        }
        private Tensor<float> ExtractPixels(Bitmap image)
        {
            var bitmap = (Bitmap)image;
            var rectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bitmapData = bitmap.LockBits(rectangle, ImageLockMode.ReadOnly, bitmap.PixelFormat);
            int bytesPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;

            var tensor = new DenseTensor<float>(new[] { 1, 3, bitmap.Height, bitmap.Width });

            unsafe // speed up conversion by direct work with memory
            {
                Parallel.For(0, bitmapData.Height, (y) =>
                {
                    byte* row = (byte*)bitmapData.Scan0 + (y * bitmapData.Stride);

                    Parallel.For(0, bitmapData.Width, (x) =>
                    {
                        tensor[0, 0, y, x] = row[x * bytesPerPixel + 2] / 255.0F; // r
                        tensor[0, 1, y, x] = row[x * bytesPerPixel + 1] / 255.0F; // g
                        tensor[0, 2, y, x] = row[x * bytesPerPixel + 0] / 255.0F; // b
                    });
                });

                bitmap.UnlockBits(bitmapData);
            }
            return tensor;
        }
        public void SetupYoloDefaultLabels()
        {
            var s = new string[] { "person", "bicycle", "car", "motorcycle", "airplane", "bus", "train", "truck", "boat", "traffic light", "fire hydrant", "stop sign", "parking meter", "bench", "bird", "cat", "dog", "horse", "sheep", "cow", "elephant", "bear", "zebra", "giraffe", "backpack", "umbrella", "handbag", "tie", "suitcase", "frisbee", "skis", "snowboard", "sports ball", "kite", "baseball bat", "baseball glove", "skateboard", "surfboard", "tennis racket", "bottle", "wine glass", "cup", "fork", "knife", "spoon", "bowl", "banana", "apple", "sandwich", "orange", "broccoli", "carrot", "hot dog", "pizza", "donut", "cake", "chair", "couch", "potted plant", "bed", "dining table", "toilet", "tv", "laptop", "mouse", "remote", "keyboard", "cell phone", "microwave", "oven", "toaster", "sink", "refrigerator", "book", "clock", "vase", "scissors", "teddy bear", "hair drier", "toothbrush", "single", "double" };
            SetupLabels(s);
        }

        protected Yolov5BaseModel(byte[] modelbytes,string input)
        {
            Microsoft.ML.OnnxRuntime.SessionOptions opts = new();
            _inferenceSession = new InferenceSession(modelbytes, opts);
            _inputName = input;
            get_input_details();
            get_output_details();
            SetupYoloDefaultLabels();
        }

        private void get_input_details()
        {
            _model.Height = _inferenceSession.InputMetadata[_inputName].Dimensions[2];
            _model.Width = _inferenceSession.InputMetadata[_inputName].Dimensions[3];
        }

        private void get_output_details()
        {
            _model.Outputs = _inferenceSession.OutputMetadata.Keys.ToArray();
            _model.Dimensions = _inferenceSession.OutputMetadata[_model.Outputs[0]].Dimensions[2];
            _model.UseDetect = !(_model.Outputs.Any(x => x == "score"));
        }

        public void Dispose()
        {
            _inferenceSession.Dispose();
        }
        protected float Clamp(float value, float min, float max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }
    }
}
