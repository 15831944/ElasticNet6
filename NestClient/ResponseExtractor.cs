using Nest;

namespace Elastic;

public static class ResponseExtractor
{
    public static string GetShortInfo(this ResponseBase r)
    {
        if (r.OriginalException is not null)
        {
            return $"Client error: \"{r.OriginalException.Message}\"";
        }

        if (r.ServerError is not null)
        {
            return $"Server error: \"{r.ServerError.Error.Reason}\"";
        }

        return r.DebugInformation;
    }
}
