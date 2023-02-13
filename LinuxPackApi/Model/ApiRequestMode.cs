namespace LinuxPackApi.Model
{
    public class ApiRequestMode
    {
        public object? Data { get; set; }
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }

        public ApiRequestMode(object data) 
        {
            Data = data;
            IsSuccess = true;
            ErrorMessage = string.Empty;
        }
        public ApiRequestMode(object data, string errorMessage)
        {
            Data = data;
            IsSuccess = string.IsNullOrEmpty(errorMessage) ? true : false;
            ErrorMessage = errorMessage;
        }
    }
}
