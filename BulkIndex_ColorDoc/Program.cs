using Elastic;
using Elastic.Docs;
using Nest;

#region Connection
NestClient nestClient = new(
    new string[] { "https://elastic.home:9200" },
    "elastic",
    "ela5tic",
    "C:/elasticsearch-8.1.1/config/http.p12",
    "http"
    );

if (nestClient.Connect(out Exception? e))
{
    Console.WriteLine("The connection with elasticsearch was successfully established.");
}
else
{
    Console.WriteLine(e!.Message);
    return;
}
#endregion

static IEnumerable<NameValueDoc<string, string>> GenDocs(int count)
{
    for (int i = 0; i < count; i++)
    {
        yield return new NameValueDoc<string, string>
        {
            Name = "color",
            Value = $"#{i}"
        };
    }
}

int indexed = 0;
string GetBulkAllResponseString(BulkAllResponse r) => $"page: {r.Page}, count: {r.Items.Count}, retries: {r.Retries}, total: {Interlocked.Add(ref indexed, r.Items.Count)}";

void Test1()
{
    if (!nestClient.BulkIndex("colors", GenDocs(100000), out long _, out long _, out Exception? ex,
        maximumRuntimeSeconds: 1,
        portionSize: 1000,
        onNext: bulkAllResponse => Console.WriteLine(GetBulkAllResponseString(bulkAllResponse))))
    {
        Console.WriteLine("error: " + ex!.Message);
    }

    //page: 1, count: 1000, retries: 0, total: 3000
    //page: 0, count: 1000, retries: 0, total: 4000
    //page: 3, count: 1000, retries: 0, total: 2000
    //page: 2, count: 1000, retries: 0, total: 1000
    //page: 4, count: 1000, retries: 0, total: 5000
    //page: 5, count: 1000, retries: 0, total: 6000
    //page: 6, count: 1000, retries: 0, total: 7000
    //page: 7, count: 1000, retries: 0, total: 8000
    //page: 8, count: 1000, retries: 0, total: 9000
    //page: 9, count: 1000, retries: 0, total: 10000
    //page: 10, count: 1000, retries: 0, total: 11000
    //page: 11, count: 1000, retries: 0, total: 12000
    //page: 12, count: 1000, retries: 0, total: 13000
    //page: 13, count: 1000, retries: 0, total: 14000
    //page: 14, count: 1000, retries: 0, total: 15000
}





Test1(); // Short waiting time.
