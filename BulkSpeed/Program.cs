using Elastic;
using Elastic.Docs;
using System.Diagnostics;

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
    Console.WriteLine("The connection with Elasticsearch was successfully established.");
}
else
{
    Console.WriteLine(e!.Message);
    Console.ReadLine();

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

int total = 1000;
Stopwatch sw = new();

sw.Start();
foreach (var doc in GenDocs(total))
{
    nestClient.IndexDoc("test-1", doc, out _, out _);
}
sw.Stop();
Console.WriteLine($"test-1: {sw.Elapsed.TotalSeconds}s");

sw.Restart();
nestClient.BulkIndex("test-2", GenDocs(total), out _, out _, out _);
sw.Stop();
Console.WriteLine($"test-2: {sw.Elapsed.TotalSeconds}s");

//test-1: 82,2295682s
//test-2: 1,4037853s

//test-1: 81,8809305s
//test-2: 0,4470201s

//test-1: 82,3672316s
//test-2: 0,4208362s
