namespace HMEye.Services;

public record ReadResult<T>(T? Value, bool Error = false, string ErrorMessage = "");
public record WriteResult(bool Success, string ErrorMessage = "");