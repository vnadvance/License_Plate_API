using Newtonsoft.Json;
using System.Runtime.CompilerServices;

namespace License_Plate_API.Model
{
    public class ResponseModel
    {
        public string? Status { get; set; }
        public string? Message { get; set; }
        public List<ResultModel>? Data { get; set; }
        public string? RootImagePath { get; set; }
        public ResponseModel() { }
        public ResponseModel(string status, string message, List<ResultModel> data)
        {
            Status = status;
            Message = message;
            Data = data;
        }
        public ResponseModel(string status, string message)
        {
            Status = status;
            Message = message;
            Data = null;
        }
    }
    public class ResponseModelSuccess : ResponseModel
    {
        public ResponseModelSuccess()
        {
        }

        public ResponseModelSuccess(string message, List<ResultModel>? data = null,string? rootImage = null)
        {
            Status = "OK";
            Message = message;
            Data = data;
            RootImagePath = rootImage;
        }
    }
    public class ResponseModelError : ResponseModel
    {
        public ResponseModelError()
        {
        }

        public ResponseModelError(string message, List<ResultModel>? data = null)
        {
            Status = "Error";
            Message = message;
            Data = data;
        }
    }
}
