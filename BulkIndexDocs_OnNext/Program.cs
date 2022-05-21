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

void Test1_ShortWaitingTime()
{
    Console.WriteLine("[Test 1: short waiting time]");
    Console.WriteLine("----------------------------");

    long currentTotal = 0;

    if (!nestClient.BulkIndex("test", GenDocs(100000), out long _, out long _, out Exception? ex,
        maximumRuntimeSeconds: 1,
        portionSize: 1000,
        onNext: bulkAllResponse =>
        {
            BulkAllResponseReport m = new(bulkAllResponse, ref currentTotal);
            Console.WriteLine(m);
        }))
    {
        Console.WriteLine("Error: " + ex!.Message);
    }

    // Output:
    //{"Page":0,"Retries":0,"ItemsWithErrorsCount":0,"CurrentTotal":1000,"ItemsByStatus":{"201":1000},"ItemsByIsValid":{"True":1000},"ErrorsReasons":[]}
    //{"Page":1,"Retries":0,"ItemsWithErrorsCount":0,"CurrentTotal":2000,"ItemsByStatus":{"201":1000},"ItemsByIsValid":{"True":1000},"ErrorsReasons":[]}
    //{"Page":2,"Retries":0,"ItemsWithErrorsCount":0,"CurrentTotal":3000,"ItemsByStatus":{"201":1000},"ItemsByIsValid":{"True":1000},"ErrorsReasons":[]}
    //{"Page":3,"Retries":0,"ItemsWithErrorsCount":0,"CurrentTotal":4000,"ItemsByStatus":{"201":1000},"ItemsByIsValid":{"True":1000},"ErrorsReasons":[]}
    //{"Page":4,"Retries":0,"ItemsWithErrorsCount":0,"CurrentTotal":5000,"ItemsByStatus":{"201":1000},"ItemsByIsValid":{"True":1000},"ErrorsReasons":[]}
    //{"Page":5,"Retries":0,"ItemsWithErrorsCount":0,"CurrentTotal":6000,"ItemsByStatus":{"201":1000},"ItemsByIsValid":{"True":1000},"ErrorsReasons":[]}
    //{"Page":6,"Retries":0,"ItemsWithErrorsCount":0,"CurrentTotal":7000,"ItemsByStatus":{"201":1000},"ItemsByIsValid":{"True":1000},"ErrorsReasons":[]}
    //{"Page":7,"Retries":0,"ItemsWithErrorsCount":0,"CurrentTotal":8000,"ItemsByStatus":{"201":1000},"ItemsByIsValid":{"True":1000},"ErrorsReasons":[]}
    //{"Page":8,"Retries":0,"ItemsWithErrorsCount":0,"CurrentTotal":9000,"ItemsByStatus":{"201":1000},"ItemsByIsValid":{"True":1000},"ErrorsReasons":[]}

    // Summary:
    // 1. No exception was thrown.
    // 2. All responses have status 201 and are valid.
}

void Test2_InvalidIndexName()
{
    Console.WriteLine("[Test 2: invalid index name]");
    Console.WriteLine("----------------------------");

    long currentTotal = 0;

    if (!nestClient.BulkIndex("te st", GenDocs(1000), out long _, out long _, out Exception? ex,
        portionSize: 250,
        onNext: bulkAllResponse =>
        {
            BulkAllResponseReport m = new(bulkAllResponse, ref currentTotal);
            Console.WriteLine(m);
        }))
    {
        Console.WriteLine("Error: " + ex!.Message);
    }

    // Output:
    // {"Page":3,"Retries":0,"ItemsWithErrorsCount":250,"CurrentTotal":1000,"ItemsByStatus":{"400":250},"ItemsByIsValid":{"False":250},"ErrorsReasons":["Invalid index name [te st], must not contain the following characters [ , \", *, \\, <, |, ,, >, /, ?]"]}
    // {"Page":1,"Retries":0,"ItemsWithErrorsCount":250,"CurrentTotal":500,"ItemsByStatus":{"400":250},"ItemsByIsValid":{"False":250},"ErrorsReasons":["Invalid index name [te st], must not contain the following characters [ , \", *, \\, <, |, ,, >, /, ?]"]}
    // {"Page":0,"Retries":0,"ItemsWithErrorsCount":250,"CurrentTotal":250,"ItemsByStatus":{"400":250},"ItemsByIsValid":{"False":250},"ErrorsReasons":["Invalid index name [te st], must not contain the following characters [ , \", *, \\, <, |, ,, >, /, ?]"]}
    // {"Page":2,"Retries":0,"ItemsWithErrorsCount":250,"CurrentTotal":750,"ItemsByStatus":{"400":250},"ItemsByIsValid":{"False":250},"ErrorsReasons":["Invalid index name [te st], must not contain the following characters [ , \", *, \\, <, |, ,, >, /, ?]"]}
    // Error: Refreshing after all documents have indexed failed    

    // Summary:
    // 1. All batches has been sent but failed.
    // 2. The exception was thrown after all batches were sent.
    // 3. All responses have status 400 and are invalid.
}

void Test3_CatchingErrors()
{
    Console.WriteLine("[Test 3: catching errors]");
    Console.WriteLine("-------------------------");

    long currentTotal = 0;

    if (!nestClient.BulkIndex("test", GenDocs(1000).Select(doc => doc.Value == "#300" ? throw new Exception("Error on #300.") : doc), 
        out long _, out long _, out Exception? ex,
        portionSize: 250,
        onNext: bulkAllResponse =>
        {
            BulkAllResponseReport m = new(bulkAllResponse, ref currentTotal);
            Console.WriteLine(m);
        }))
    {
        Console.WriteLine("Error: " + ex!.Message);
    }

    // Output:
    // Error: Error on #300.

    // Summary:
    // 1. The exception has been thrown.
    // 2. GET /test/_count --> 250
}

//Test1_ShortWaitingTime();
//Test2_InvalidIndexName();
Test3_CatchingErrors();