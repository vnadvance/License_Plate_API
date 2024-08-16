using Newtonsoft.Json;
using System.Runtime.CompilerServices;

namespace License_Plate_API.Model
{
    public class ResponseModel
    {
        public string? Status { get; set; }
        public string? Messenge { get; set; }
        public string? Data { get; set; }
        public ResponseModel() { }
        public ResponseModel(string status, string messenge, Object data)
        {
            Status = status;
            Messenge = messenge;
            Data = JsonConvert.SerializeObject(data);
        }
        public ResponseModel(string status, string messenge)
        {
            Status = status;
            Messenge = messenge;
            Data = null;
        }
    }
    public class ResponseModelSuccess : ResponseModel
    {
        public ResponseModelSuccess()
        {
        }

        public ResponseModelSuccess(string messenge, Object? data = null)
        {
            Status = "OK";
            Messenge = messenge;
            Data = JsonConvert.SerializeObject(data);
        }
    }
    public class ResponseModelError : ResponseModel
    {
        public ResponseModelError()
        {
        }

        public ResponseModelError(string messenge, Object? data = null)
        {
            Status = "Error";
            Messenge = messenge;
            Data = JsonConvert.SerializeObject(data);
        }
    }
}
