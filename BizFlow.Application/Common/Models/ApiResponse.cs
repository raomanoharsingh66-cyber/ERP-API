namespace BizFlow.Application.Common.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ApiResponse<T> SuccessResult(T data, string message = "Operation completed successfully.")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            Errors = new List<string>()
        };
    }

    public static ApiResponse<T> FailureResult(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Data = default,
            Errors = errors ?? new List<string>()
        };
    }

    public static ApiResponse<T> FailureResult(string message, string error)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Data = default,
            Errors = new List<string> { error }
        };
    }

    public static ApiResponse<T> SuccessResponse(T data, string message = "Operation completed successfully.")
    {
        return SuccessResult(data, message);
    }

    public static ApiResponse<T> FailureResponse(string message, List<string>? errors = null)
    {
        return FailureResult(message, errors);
    }

    public static ApiResponse<T> FailureResponse(string message, string error)
    {
        return FailureResult(message, error);
    }
}

public class ApiResponse : ApiResponse<object?>
{
    public static ApiResponse SuccessResult(string message = "Operation completed successfully.")
    {
        return new ApiResponse
        {
            Success = true,
            Message = message,
            Data = null,
            Errors = new List<string>()
        };
    }

    public static new ApiResponse FailureResult(string message, List<string>? errors = null)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            Data = null,
            Errors = errors ?? new List<string>()
        };
    }

    public static new ApiResponse FailureResult(string message, string error)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            Data = null,
            Errors = new List<string> { error }
        };
    }

    public static ApiResponse SuccessResponse(string message = "Operation completed successfully.")
    {
        return SuccessResult(message);
    }

    public static new ApiResponse FailureResponse(string message, List<string>? errors = null)
    {
        return FailureResult(message, errors);
    }

    public static new ApiResponse FailureResponse(string message, string error)
    {
        return FailureResult(message, error);
    }
}
