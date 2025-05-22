namespace ProductAPIRedisCache.Common
{
    public record ApiErrorResponse(
        bool Success,
        string Message,
        string? TraceId,
        string? RequestId);
}
