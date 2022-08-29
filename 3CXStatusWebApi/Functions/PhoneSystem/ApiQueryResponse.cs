namespace WebAPI.Functions
{
    public class ApiQueryResponse
    {
        public ApiQueryResponse(string message, string status)
        {
            Message = message;
            Status = status;
            TimeStamp = DateTime.Now;
        }
        public string? Message { get; set; }
        public string? Status { get; set; }
        public DateTime TimeStamp { get; set; }
    }

}
