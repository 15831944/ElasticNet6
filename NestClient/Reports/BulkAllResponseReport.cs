using Nest;
using Newtonsoft.Json;

namespace Elastic.Reports;

public class BulkAllResponseReport
{
    public long Page;
    public long Retries;
    public long? CurrentTotal;
    public long ItemsWithErrorsCount;
    public Dictionary<int, int> ItemsByStatus;
    public List<string> ErrorsReasons;

    public BulkAllResponseReport(BulkAllResponse r)
    {
        var errors = r.Items
            .Where(i => i.Error is not null)
            .Select(i => i.Error.Reason);

        ItemsWithErrorsCount = errors.Count();
        ErrorsReasons = errors.Distinct().ToList();
        Page = r.Page;
        Retries = r.Retries;
        ItemsByStatus = r.Items
            .GroupBy(k => k.Status, v => v)
            .ToDictionary(k => k.Key, v => v.Count());
    }

    public BulkAllResponseReport(BulkAllResponse r, ref long currentTotal) : this(r)
    {
        CurrentTotal = Interlocked.Add(ref currentTotal, r.Items.Count);
    }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }
}
