namespace FitControlWeb.Helpers
{
    public class ServiceResult
    {
        public bool Success { get; protected set; }

        public string Message { get; protected set; } = string.Empty;

        public string? Code { get; protected set; }

        public static ServiceResult Ok(string? message = null) =>
            new() { Success = true, Message = message ?? string.Empty };

        public static ServiceResult Fail(string message, string? code = null) =>
            new() { Success = false, Message = message, Code = code };
    }

    public class ServiceResult<T> : ServiceResult
    {
        public T? Data { get; protected set; }

        public static ServiceResult<T> Ok(T data, string? message = null) =>
            new() { Success = true, Data = data, Message = message ?? string.Empty };

        public static new ServiceResult<T> Fail(string message, string? code = null) =>
            new() { Success = false, Message = message, Code = code };
    }
}