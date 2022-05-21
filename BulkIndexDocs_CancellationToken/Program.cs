using Elastic;
using Elastic.Docs;

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

static IEnumerable<NameValueDoc<string, string>> GenDocs(int count, CancellationTokenSource cancellationTokenSource)
{
    for (int i = 0; i < count; i++)
    {
        if (i == 10_500)
        {
            cancellationTokenSource.Cancel();
        }

        yield return new NameValueDoc<string, string>
        {
            Name = "color",
            Value = $"#{i}"
        };
    }
}

int indexed = 0;
CancellationTokenSource cancellationTokenSource = new();
CancellationToken cancellationToken = cancellationTokenSource.Token;

nestClient.BulkIndex("test", GenDocs(100_000, cancellationTokenSource), out long _, out long _, out Exception? ex,
    portionSize: 1000,
    cancellationToken: cancellationToken,
    onNext: response => Console.WriteLine(Interlocked.Add(ref indexed, response.Items.Count)));

// GET test/_count --> 10000