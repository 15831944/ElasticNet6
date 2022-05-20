using Elastic;
using Elastic.Docs;
using Elastic.Reports;

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

void Test1_InvalidIndexName()
{
    Console.WriteLine("[Test 1: invalid index name]");
    Console.WriteLine("----------------------------");

    long currentTotal = 0;

    if (!nestClient.BulkIndex("te st", GenDocs(1000), out long _, out long _, out Exception? ex,
        portionSize: 250,
        bulkResponseCallback: bulkResponse =>
        {
            BulkResponseReport m = new(bulkResponse, ref currentTotal);
            Console.WriteLine(m);
        }))
    {
        Console.WriteLine("Error: " + ex!.Message);
    }

    // Output:
    //{"HasError":true,"CurrentTotal":1000,"ItemsWithErrorsCount":250,"ItemsByStatus":{"400":250},"ErrorsReasons":["Invalid index name [te st], must not contain the following characters [ , \", *, \\, <, |, ,, >, /, ?]"]}
    //{"HasError":true,"CurrentTotal":500,"ItemsWithErrorsCount":250,"ItemsByStatus":{"400":250},"ErrorsReasons":["Invalid index name [te st], must not contain the following characters [ , \", *, \\, <, |, ,, >, /, ?]"]}
    //{"HasError":true,"CurrentTotal":250,"ItemsWithErrorsCount":250,"ItemsByStatus":{"400":250},"ErrorsReasons":["Invalid index name [te st], must not contain the following characters [ , \", *, \\, <, |, ,, >, /, ?]"]}
    //{"HasError":true,"CurrentTotal":750,"ItemsWithErrorsCount":250,"ItemsByStatus":{"400":250},"ErrorsReasons":["Invalid index name [te st], must not contain the following characters [ , \", *, \\, <, |, ,, >, /, ?]"]}
    //Error: Refreshing after all documents have indexed failed

    // All responses have status 400.
}

void Test2_CatchingErrors()
{
    Console.WriteLine("[Test 2: catching errors]");
    Console.WriteLine("-------------------------");

    long currentTotal = 0;

    if (!nestClient.BulkIndex("test", GenDocs(1000).Select(doc => doc.Value == "#300" ? throw new Exception("Error on #300.") : doc),
        out long _, out long _, out Exception? ex,
        portionSize: 250,
        bulkResponseCallback: bulkResponse =>
        {
            BulkResponseReport m = new(bulkResponse, ref currentTotal);
            Console.WriteLine(m);
        }))
    {
        Console.WriteLine("Error: " + ex!.Message);
    }

    // Output:
    //Error: Error on #300.

    // GET /test/_count --> 250
}

//Test1_InvalidIndexName();
Test2_CatchingErrors();