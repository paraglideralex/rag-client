namespace RagClient.Api.Models;

/// <summary>
/// Unified response envelope matching the upstream RAG API format: { status, service, data, errors?, code? }.
/// Also used as the envelope returned to the frontend.
/// </summary>
public class ApiResponse<T>
{
    public string Status { get; set; } = string.Empty;
    public string? Service { get; set; }
    public T? Data { get; set; }
    public string? Errors { get; set; }
    public string? Code { get; set; }

    public bool IsSuccess => Status == "ok";

    public static ApiResponse<T> Error(string message, string? code = null) => new()
    {
        Status = "error",
        Errors = message,
        Code = code
    };
}
