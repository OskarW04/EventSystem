// EventSystem.API/Common/ApiResponse.cs
namespace EventSystem.API.Common;

/*
 * Helper class for consistent API responses.
 *
 * Instead of returning raw data, always return an ApiResponse object,
 * which contains information about the success, data (if successful) and an optional message.
 */
public class ApiResponse<T>
{
      public bool Success { get; init; }
      public T? Data { get; init; }
      public string? Message { get; init; }

      public static ApiResponse<T> Ok(T data, string? message = null) => new()
      {
            Success = true,
            Data = data,
            Message = message
      };

      public static ApiResponse<T> Fail(string message) => new()
      {
            Success = false,
            Data = default,
            Message = message
      };
}

/*
 * version without data
 * for endpoints that only need to indicate success/failure
 */
public class ApiResponse : ApiResponse<object>
{
      public static ApiResponse Ok(string? message = null) => new()
      {
            Success = true,
            Message = message
      };

      public new static ApiResponse Fail(string message) => new()
      {
            Success = false,
            Message = message
      };
}