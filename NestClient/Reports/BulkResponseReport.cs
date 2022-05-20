using Nest;
using Newtonsoft.Json;

namespace Elastic.Reports;

public class BulkResponseReport
{
    public bool HasError;
    public long? CurrentTotal;
    public long ItemsWithErrorsCount;
    public Dictionary<int, int> ItemsByStatus;
    public List<string> ErrorsReasons;

    public BulkResponseReport(BulkResponse r)
    {
        var errors = r.Items
            .Where(i => i.Error is not null)
            .Select(i => i.Error.Reason);

        HasError = errors.Any();
        ItemsWithErrorsCount = errors.Count();
        ErrorsReasons = errors.Distinct().ToList();
        ItemsByStatus = r.Items
            .GroupBy(k => k.Status, v => v)
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
