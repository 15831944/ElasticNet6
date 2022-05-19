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

static IEnumerable<ColorDoc> GenColorDoc(int count)
{
    for (int i = 0; i < count; i++)
    {
        yield return new ColorDoc
        {
            Code = $"#{i}",
            Name = $"color-{i}"
        };
    }
}

#region Handlers
int indexed = 0;

Action<BulkAllResponse> onNext = (response) =>
{
    Console.WriteLine($"page: {response.Page}, count: {response.Items.Count}, total: {Interlocked.Add(ref indexed, response.Items.Count)}");
};
#endregion

#region Test 1, Wait(), short waiting time.
Console.WriteLine("test 1");

if (!nestClient.BulkIndex("color-docs", GenColorDoc(100_000), out long _, out long _, out Exception? ex,
    portionSize: 100,
    onNext: onNext,
    maximumRuntimeSeconds: 1))
{
    Console.WriteLine(ex!.Message);
}

//test 1
//page: 0, count: 100, total: 100
//page: 1, count: 100, total: 200
//page: 4, count: 100, total: 300
//page: 2, count: 100, total: 400
//page: 3, count: 100, total: 500
//page: 5, count: 100, total: 600
//page: 6, count: 100, total: 700
//page: 7, count: 100, total: 800
//page: 8, count: 100, total: 900
//page: 9, count: 100, total: 1000
//page: 11, count: 100, total: 1100
//page: 10, count: 100, total: 1200
//page: 12, count: 100, total: 1300
//page: 13, count: 100, total: 1400
#endregion