namespace EShoppingZone.Common.Exceptions
{
    public class ServiceException : Exception
    {
        public int StatusCode { get; set; }
        public string? ServiceName { get; set; }

        public ServiceException(string message, int statusCode = 500, string? serviceName = null)
            : base(message)
        {
            StatusCode = statusCode;
            ServiceName = serviceName;
        }
    }
}
