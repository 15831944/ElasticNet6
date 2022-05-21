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
    // {"IsValid":false,"HasError":true,"ItemsWithErrorsCount":250,"CurrentTotal":750,"ItemsByStatus":{"400":250},"ItemsByIsValid":{"False":250},"ErrorsReasons":["Invalid index name [te st], must not contain the following characters [ , \", *, \\, <, |, ,, >, /, ?]"],"OriginalExceptionMessage":null,"ServerErrorMessage":null}
    // {"IsValid":false,"HasError":true,"ItemsWithErrorsCount":250,"CurrentTotal":500,"ItemsByStatus":{"400":250},"ItemsByIsValid":{"False":250},"ErrorsReasons":["Invalid index name [te st], must not contain the following characters [ , \", *, \\, <, |, ,, >, /, ?]"],"OriginalExceptionMessage":null,"ServerErrorMessage":null}
    // {"IsValid":false,"HasError":true,"ItemsWithErrorsCount":250,"CurrentTotal":250,"ItemsByStatus":{"400":250},"ItemsByIsValid":{"False":250},"ErrorsReasons":["Invalid index name [te st], must not contain the following characters [ , \", *, \\, <, |, ,, >, /, ?]"],"OriginalExceptionMessage":null,"ServerErrorMessage":null}
    // {"IsValid":false,"HasError":true,"ItemsWithErrorsCount":250,"CurrentTotal":1000,"ItemsByStatus":{"400":250},"ItemsByIsValid":{"False":250},"ErrorsReasons":["Invalid index name [te st], must not contain the following characters [ , \", *, \\, <, |, ,, >, /, ?]"],"OriginalExceptionMessage":null,"ServerErrorMessage":null}
    // Error: Refreshing after all documents have indexed failed

    // Summary:
    // 1. All batches has been sent but failed.
    // 2. The exception was thrown after all batches were sent.
    // 3. All responses have status 400, HTTP error and are invalid.
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
    // Error: Error on #300.

    // Summary:
    // 1. The exception has been thrown.
    // 2. GET /test/_count --> 250
}

void Test3_ClusterShutdown()
{
    Console.WriteLine("[Test 3: cluster shutdown]");
    Console.WriteLine("--------------------------");

    if (!nestClient.BulkIndex("test", GenDocs(1000000),
        out long _, out long _, out Exception? ex,
        portionSize: 10,
        bulkResponseCallback: bulkResponse =>
        {
            BulkResponseReport m = new(bulkResponse);
            Console.WriteLine("bulkResponse    >>> " + m);
        },
        onNext: bulkAllResponse =>
        {
            BulkAllResponseReport m = new(bulkAllResponse);
            Console.WriteLine("bulkAllResponse >>> " + m);
        }))
    {
        Console.WriteLine("Error: " + ex!.Message);
    }

    // Output:
    //bulkAllResponse >>> {...,"ErrorsReasons":[]}
    //bulkResponse    >>> {"IsValid":true,"HasError":false,...}
    //...
    // see -->            !(1)                                                                                                                                                                !(2)
    //bulkResponse    >>> {"IsValid":false,"HasError":false,"ItemsWithErrorsCount":0,"CurrentTotal":null,"ItemsByStatus":{},"ItemsByIsValid":{},"ErrorsReasons":[],"OriginalExceptionMessage":"An error occurred while sending the request.","ServerErrorMessage":null}
    //bulkResponse    >>> {"IsValid":false,"HasError":false,"ItemsWithErrorsCount":0,"CurrentTotal":null,"ItemsByStatus":{},"ItemsByIsValid":{},"ErrorsReasons":[],"OriginalExceptionMessage":"Подключение не установлено, т.к. конечный компьютер отверг запрос на подключение. (elastic.home:9200)","ServerErrorMessage":null}
    //bulkResponse    >>> {"IsValid":false,"HasError":false,"ItemsWithErrorsCount":0,"CurrentTotal":null,"ItemsByStatus":{},"ItemsByIsValid":{},"ErrorsReasons":[],"OriginalExceptionMessage":"Подключение не установлено, т.к. конечный компьютер отверг запрос на подключение. (elastic.home:9200)","ServerErrorMessage":null}
    //bulkResponse    >>> {"IsValid":false,"HasError":false,"ItemsWithErrorsCount":0,"CurrentTotal":null,"ItemsByStatus":{},"ItemsByIsValid":{},"ErrorsReasons":[],"OriginalExceptionMessage":"Подключение не установлено, т.к. конечный компьютер отверг запрос на подключение. (elastic.home:9200)","ServerErrorMessage":null}
    //bulkResponse    >>> {"IsValid":false,"HasError":false,"ItemsWithErrorsCount":0,"CurrentTotal":null,"ItemsByStatus":{},"ItemsByIsValid":{},"ErrorsReasons":[],"OriginalExceptionMessage":"Failed to ping the specified node. Call: Status code unknown from: HEAD /","ServerErrorMessage":null}
    //bulkResponse    >>> {"IsValid":false,"HasError":false,"ItemsWithErrorsCount":0,"CurrentTotal":null,"ItemsByStatus":{},"ItemsByIsValid":{},"ErrorsReasons":[],"OriginalExceptionMessage":"Failed to ping the specified node. Call: Status code unknown from: HEAD /","ServerErrorMessage":null}
    //bulkResponse    >>> {"IsValid":false,"HasError":false,"ItemsWithErrorsCount":0,"CurrentTotal":null,"ItemsByStatus":{},"ItemsByIsValid":{},"ErrorsReasons":[],"OriginalExceptionMessage":"Failed to ping the specified node. Call: Status code unknown from: HEAD /","ServerErrorMessage":null}
    //bulkResponse    >>> {"IsValid":false,"HasError":false,"ItemsWithErrorsCount":0,"CurrentTotal":null,"ItemsByStatus":{},"ItemsByIsValid":{},"ErrorsReasons":[],"OriginalExceptionMessage":"Failed to ping the specified node. Call: Status code unknown from: HEAD /","ServerErrorMessage":null}
    //bulkResponse    >>> {"IsValid":false,"HasError":false,"ItemsWithErrorsCount":0,"CurrentTotal":null,"ItemsByStatus":{},"ItemsByIsValid":{},"ErrorsReasons":[],"OriginalExceptionMessage":"Failed to ping the specified node. Call: Status code unknown from: HEAD /","ServerErrorMessage":null}
    //Error: BulkAll halted after PipelineFailure.PingFailure from _bulk and exhausting retries (2)                                                                                                                !

    // Summary:
    // 1. The original exception has been thrown.
    // 2. The method stopped after exhaustive retries and gave an error about it.
    // 3. No messages from onNext().
    // 4. No items (ItemsWithErrorsCount, ItemsByStatus). 
    // 5. All batches before the error occurred were sent. GET /test/_count --> >0
}

//Test1_InvalidIndexName();
//Test2_CatchingErrors();
//Test3_ClusterShutdown();
