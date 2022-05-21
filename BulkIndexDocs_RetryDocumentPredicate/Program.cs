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

if (nestClient.Connect(out Exception? e, false))
{
    Console.WriteLine("The connection with elasticsearch was successfully established.");
}
else
{
    Console.WriteLine(e!.Message);
    return;
}
#endregion

// ? Work only with connectionSettings.EnableApiVersioningHeader(false)...

static IEnumerable<IdNameValueDoc<string, string>> GenDocs(int count)
{
    for (int i = 0; i < count; i++)
    {
        yield return new IdNameValueDoc<string, string>
        {
            Name = "color",
            Value = i > 0 && i % 7 == 0 
                ? "'Times New Roman'"
                : $"#{i}"
        };
    }
}

void Test1_1Retry()
{
    Console.WriteLine("[Test 1: 1 retry]");
    Console.WriteLine("-----------------");

    string defaultValue = "unset";
    int updated = 0;

    if (!nestClient.BulkIndex("test", GenDocs(30), out long _, out long _, out Exception? ex,
        portionSize: 10,
        retryDocumentPredicate: (response, doc) =>
        {
            if (!doc.Value.StartsWith('#') && doc.Value != defaultValue)
            {
                Interlocked.Increment(ref updated);

                doc.Value = defaultValue;
                doc.Id = response.Id;

                return true;
            }

            if (response.Status == 401)
            {
                //...
            }

            return false;
        }))
    {
        Console.WriteLine("Error: " + ex!.Message);
    }

    Console.WriteLine("Updated: " + updated);

    // Output:
    // Updated: 4

    // Kibana:
    // GET test/_count --> 30
    // GET test/_search --> 4
    // {
    //   "query": {
    //     "query_string": {
    //       "default_field": "value",
    //       "query": "unset"
    //     }
    //   }
    // }
}

void Test2_NRetries()
{
    Console.WriteLine("[Test 2: The number of retries exceeds the maximum]");

    int indexed = 0;

    if (!nestClient.BulkIndex("test", GenDocs(10000), out long _, out long _, out Exception? ex,
        numberOfRetries: 3,
        timeBetweenRetries: "1s",
        portionSize: 10,
        retryDocumentPredicate: (response, doc) =>
        {
            if (doc.Value == "#15")
            {
                doc.Id = response.Id;
                return true;
            }

            return false;
        },
        onNext: response => Interlocked.Add(ref indexed, response.Items.Count)))
    {
        Console.WriteLine("Error: " + ex!.Message);
    }

    Console.WriteLine("Indexed: " + indexed);

    // Output:
    // Error: Bulk indexing failed and after retrying 3 times
    // Indexed: 660

    // Summary:
    // 1. When the number of retries exceeds the maximum, an exception is thrown that aborts the execution of the method.
    // 2. Retrying does not affect the sending of other batches.
}

//Test1_1Retry();
//Test2_NRetries();
