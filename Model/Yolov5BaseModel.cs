﻿using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using License_Plate_API.Utils;
using System.Collections.Concurrent;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

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

        private DenseTensor<float>[] Inference(Image<Rgba32> img)
        {
            Image<Rgba32> resized;
            if (img.Width != _model.Width || img.Height != _model.Height)
            {
                resized = Utility.ResizeImage(img.Clone(), _model.Width, _model.Height); // fit image size to specified input size
            }
            else
            {
                resized = img;
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
        private List<YoloPrediction> Supress(List<YoloPrediction> items)
        {
            var result = new List<YoloPrediction>(items);
            foreach (var item in items)
            {
                foreach (var current in result.ToList())
                {
                    if (current == item) continue;

                    var (rect1, rect2) = (item.Rectangle, current.Rectangle);


                    SixLabors.ImageSharp.RectangleF intersection = SixLabors.ImageSharp.RectangleF.Intersect(rect1, rect2);

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
        private Tensor<float> ExtractPixels(Image<Rgba32> image)
        {
            int width = image.Width;
            int height = image.Height;

            // Create a tensor with the same dimensions as the image
            var tensor = new DenseTensor<float>(new[] { 1, 3, height, width });

            // Process the image row by row
            Parallel.For(0, image.Height, y =>
            {
                Parallel.For(0, image.Width, x =>
                {
                    tensor[0, 0, y, x] = image[x, y].R / 255.0F; // r
                    tensor[0, 1, y, x] = image[x, y].G / 255.0F; // g
                    tensor[0, 2, y, x] = image[x, y].B / 255.0F; // b
                });
            });

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
        private float Clamp(float value, float min, float max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        private List<YoloPrediction> ParseDetect(DenseTensor<float> output, Image<Rgba32> image)
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
                        xC = (output[0, i, 0] + output[0, i, 2]) / 2,
                        yC = (output[0, i, 1] + output[0, i, 3]) / 2,
                        Rectangle = new SixLabors.ImageSharp.RectangleF(xMin, yMin, xMax - xMin, yMax - yMin)
                    };

                    result.Add(prediction);
                });
            });

            return result.ToList();
        }
        private List<YoloPrediction> ParseSigmoid(DenseTensor<float>[] output, SixLabors.ImageSharp.Image image)
        {
            return new List<YoloPrediction>();
        }
        public List<YoloPrediction> Predict(Image<Rgba32> image, float conf_thres = 0, float iou_thres = 0)
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

        private List<YoloPrediction> ParseOutput(DenseTensor<float>[] output, Image<Rgba32> image)
        {
            return _model.UseDetect ? ParseDetect(output[0], image) : ParseSigmoid(output, image);
        }
    }
}
