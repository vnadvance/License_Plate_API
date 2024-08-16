using Microsoft.ML.OnnxRuntime;

using Microsoft.ML.OnnxRuntime.Tensors;
using System.Collections.Concurrent;
using License_Plate_API.Utils;
using System.Drawing;

namespace License_Plate_API.Model
{
    public class Yolov5DetectModel : Yolov5BaseModel
    {
        public Yolov5DetectModel() : base(Properties.Resources.plate_detect_v7, "input")
        {            
        }

        public List<YoloPrediction> Predict(Image image, float conf_thres = 0, float iou_thres = 0)
        {
            if (conf_thres > 0f)
            {
                _model.Confidence = conf_thres;
                _model.MulConfidence = conf_thres + 0.05f;
            }
            if (iou_thres > 0f)
            {
                _model.Overlap = iou_thres;
            }
            return Supress(ParseOutput(Inference(image), image));
        }
        private List<YoloPrediction> ParseOutput(DenseTensor<float>[] output, Image image)
        {
            return _model.UseDetect ? ParseDetect(output[0], image) : ParseSigmoid(output, image);
        }
        private List<YoloPrediction> ParseSigmoid(DenseTensor<float>[] output, Image image)
        {
            return new List<YoloPrediction>();
        }
       
        private List<YoloPrediction> ParseDetect(DenseTensor<float> output, Image image)
        {
            var result = new ConcurrentBag<YoloPrediction>();

            var (w, h) = (image.Width, image.Height); // image w and h
            var (xGain, yGain) = (_model.Width / (float)w, _model.Height / (float)h); // x, y gains
            var gain = Math.Min(xGain, yGain); // gain = resized / original

            var (xPad, yPad) = ((_model.Width - w * gain) / 2, (_model.Height - h * gain) / 2); // left, right pads

            Parallel.For(0, (int)output.Length / _model.Dimensions, (i) =>
            {
                if (output[0, i, 4] <= _model.Confidence) return; // skip low obj_conf results

                Parallel.For(5, _model.Dimensions, (j) =>
                {
                    output[0, i, j] = output[0, i, j] * output[0, i, 4]; // mul_conf = obj_conf * cls_conf
                });

                Parallel.For(5, _model.Dimensions, (k) =>
                {
                    if (output[0, i, k] <= _model.MulConfidence) return; // skip low mul_conf results

                    float xMin = ((output[0, i, 0] - output[0, i, 2] / 2) - xPad) / gain; // unpad bbox tlx to original
                    float yMin = ((output[0, i, 1] - output[0, i, 3] / 2) - yPad) / gain; // unpad bbox tly to original
                    float xMax = ((output[0, i, 0] + output[0, i, 2] / 2) - xPad) / gain; // unpad bbox brx to original
                    float yMax = ((output[0, i, 1] + output[0, i, 3] / 2) - yPad) / gain; // unpad bbox bry to original

                    xMin = Clamp(xMin, 0, w - 0); // clip bbox tlx to boundaries
                    yMin = Clamp(yMin, 0, h - 0); // clip bbox tly to boundaries
                    xMax = Clamp(xMax, 0, w - 1); // clip bbox brx to boundaries
                    yMax = Clamp(yMax, 0, h - 1); // clip bbox bry to boundaries

                    YoloLabel label = _model.Labels[k - 5];

                    var prediction = new YoloPrediction(label, output[0, i, k])
                    {
                        Rectangle = new RectangleF(xMin, yMin, xMax - xMin, yMax - yMin)
                    };

                    result.Add(prediction);
                });
            });

            return result.ToList();
        }
        private float Clamp(float value, float min, float max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }
    }

    
}
