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

void Test1_WorkWithoutErrors()
{
    Console.WriteLine("[Test 1: work without errors]");
    Console.WriteLine("-----------------------------");

    int droppedCount = 0;

    if (!nestClient.BulkIndex("test", GenDocs(10000), out long _, out long _, out Exception? ex,
        portionSize: 100,
        droppedDocumentCallback: (response, doc) =>
        {
            Interlocked.Increment(ref droppedCount);
        }))
    {
        Console.WriteLine("Error: " + ex!.Message);
    }

    Console.WriteLine($"Dropped docs: {droppedCount}");

    // Output:
    // Dropped docs: 0

    // Summary:
    // 'droppedDocumentCallback' was not called.
}

void Test2_InvalidIndexName_ContinueAfterDroppedDocuments()
{
    Console.WriteLine("[Test 2: invalid index name, continueAfterDroppedDocuments: true]");
    Console.WriteLine("-----------------------------------------------------------------");

    int total = 10000;
    int droppedCount = 0;

    if (!nestClient.BulkIndex("te st", GenDocs(total), out long _, out long _, out Exception? ex,
        portionSize: 100,
        continueAfterDroppedDocuments: true,
        droppedDocumentCallback: (response, doc) => 
        {
            Interlocked.Increment(ref droppedCount);
        }))
    {
        Console.WriteLine("Error: " + ex!.Message);
    }

    Console.WriteLine($"Dropped docs: {droppedCount}");
    Console.WriteLine($"Dropped all: {droppedCount == total}");

    // Output:
    // Error: Refreshing after all documents have indexed failed
    // Dropped docs: 10000
    // Dropped all: True

    // Summary:
    // 1. 'droppedDocumentCallback' was called after each dropping.
    // 2. Dropping does not throw an exception and does not stop the method.
}

void Test3_InvalidIndexName_DoNotContinueAfterDroppedDocuments()
{
    Console.WriteLine("[Test 2: invalid index name, continueAfterDroppedDocuments: false]");
    Console.WriteLine("------------------------------------------------------------------");

    int total = 10000;
    int droppedCount = 0;

    if (!nestClient.BulkIndex("te st", GenDocs(total), out long _, out long _, out Exception? ex,
        portionSize: 100,
        continueAfterDroppedDocuments: false,
        droppedDocumentCallback: (response, doc) =>
        {
            Interlocked.Increment(ref droppedCount);
        }))
    {
        Console.WriteLine("Error: " + ex!.Message);
    }

    Console.WriteLine($"Dropped docs: {droppedCount}");
    Console.WriteLine($"Dropped all: {droppedCount == total}");

    // Output:
    // Error: BulkAll halted after receiving failures that can not be retried from _bulk
    // Dropped docs: 400
    // Dropped all: False

    // Summary:
    // 1. 'droppedDocumentCallback' was not called after each dropping.
    // 2. Dropping throws an exception and stops the method.
}

//Test1_WorkWithoutErrors();
//Test2_InvalidIndexName_ContinueAfterDroppedDocuments();
//Test3_InvalidIndexName_DoNotContinueAfterDroppedDocuments();
