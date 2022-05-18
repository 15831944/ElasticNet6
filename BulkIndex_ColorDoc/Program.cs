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
    Console.WriteLine($"page: {response.Page}, items count: {Interlocked.Add(ref indexed, response.Items.Count)}");
};
#endregion

//
// Test 1
// Wait()
//
Console.WriteLine("Test 1");

nestClient.BulkIndex("color-docs", GenColorDoc(100_000), out long _, out long _,
    portionSize: 100,
    onNext: onNext,
    maximumRuntimeSeconds: 1);
//page: 0, items count: 100
//page: 3, items count: 200
//page: 1, items count: 300
//page: 2, items count: 400
//...
//page: 15, items count: 1600