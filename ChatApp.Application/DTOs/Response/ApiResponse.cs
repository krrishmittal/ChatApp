namespace ChatApp.Application.DTOs.Response;

public class ApiError 
{
    public int Code {  get; set; }
    public string Message { get; set; }= null!;
    public string Location { get; set; } = null!;
}

public class ApiResponse<T> {
    public bool Success { get; set; }   
    public string Message { get; set; }
    public T? Data { get; set; }
    public IEnumerable<ApiError>? Errors { get; set; }
    
    public static ApiResponse<T>Ok(T data, string message = "Request successful")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> Fail(string message, int code, string location)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = new[]
            {
                new ApiError
                {
                    Code=code,
                    Message=message,
                    Location=location
                }
            }
        };
    }
}