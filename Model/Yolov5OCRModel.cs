using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Text;
using System.Drawing;

namespace License_Plate_API.Model
{
    public class Yolov5OCRModel  : Yolov5BaseModel
    {
        private static string PLATE_NAME =
           "#京沪津渝冀晋蒙辽吉黑苏浙皖闽赣鲁豫鄂湘粤桂琼川贵云藏陕甘青宁新" +
           "学警港澳挂使领民航危0123456789ABCDEFGHJKLMNPQRSTUVWXYZ险品";

        public Yolov5OCRModel(): base(Properties.Resources.plate_rec, "images")
        {
        }

        public string Predict(Bitmap image, float conf_thres = 0, float iou_thres = 0)
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
            return ParseOutput(Inference(image), image);
        }

        private string ParseOutput(DenseTensor<float>[] output, Bitmap image)
        {
            int allValues = output[0].Count();
            int[] dims = output[0].Dimensions.ToArray();
            int crnnRows = allValues / dims[2];
            var resultsTxt = ScoreToTextLine(output[0].AsEnumerable<float>().ToArray(), crnnRows, dims[2]);
            return resultsTxt;
        }
        private string ScoreToTextLine(float[] srcData, int rows, int cols)
        {
            StringBuilder sb = new StringBuilder();
            // TextLine textLine = new TextLine();

            int lastIndex = 0;
            List<float> scores = new List<float>();

            for (int i = 0; i < rows; i++)
            {
                int maxIndex = 0;
                float maxValue = -1000F;

                //do softmax
                List<float> expList = new List<float>();
                for (int j = 0; j < cols; j++)
                {
                    float expSingle = (float)Math.Exp(srcData[i * cols + j]);
                    expList.Add(expSingle);
                }
                float partition = expList.Sum();
                for (int j = 0; j < cols; j++)
                {
                    float softmax = expList[j] / partition;
                    if (softmax > maxValue)
                    {
                        maxValue = softmax;
                        maxIndex = j;
                    }
                }

                if (maxIndex > 0 && maxIndex < PLATE_NAME.Length && (!(i > 0 && maxIndex == lastIndex)))
                {
                    scores.Add(maxValue);
                    sb.Append(PLATE_NAME[maxIndex]);
                }
                lastIndex = maxIndex;
            }

            return sb.ToString();
        }
    }
}
