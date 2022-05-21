using Elasticsearch.Net;
using Nest;
using Newtonsoft.Json;

namespace Elastic.Reports;

public class BulkResponseReport
{
    public bool IsValid;
    public bool HasError;
    public long ItemsWithErrorsCount;
    public long? CurrentTotal;
    public Dictionary<int, int> ItemsByStatus;
    public Dictionary<bool, int> ItemsByIsValid;
    public List<string> ErrorsReasons;

    [JsonIgnore]
    public Exception? OriginalException;
    public string? OriginalExceptionMessage => OriginalException?.Message;

    [JsonIgnore]
    public ServerError? ServerError;
    public string? ServerErrorMessage => ServerError?.Error?.Reason;

    public BulkResponseReport(BulkResponse r)
    {
        IsValid = r.IsValid;
        OriginalException = r.OriginalException;
        ServerError = r.ServerError;

        HasError = r.ItemsWithErrors.Any();
        ItemsWithErrorsCount = r.ItemsWithErrors.Count();
        
        ErrorsReasons = r.ItemsWithErrors
            .Where(i => i.Error?.Reason is not null)
            .Select(i => i.Error.Reason)
            .Distinct()
            .ToList();
        
        ItemsByStatus = r.Items
            .GroupBy(k => k.Status, v => v)
            .ToDictionary(k => k.Key, v => v.Count());

        ItemsByIsValid = r.Items
            .GroupBy(k => k.IsValid, v => v)
            .ToDictionary(k => k.Key, v => v.Count());
    }

    public BulkResponseReport(BulkResponse r, ref long currentTotal) : this(r)
    {
        CurrentTotal = Interlocked.Add(ref currentTotal, r.Items.Count);
    }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }
}
