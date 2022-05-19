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
    Console.WriteLine("The connection with Elasticsearch was successfully established.");
}
else
{
    Console.WriteLine(e!.Message);
    Console.ReadLine();

    return;
}
#endregion

CodeTextsDoc doc = new();
doc.Code = "code123";
doc.TextParameters = new List<ListItemParameter<string>>
{
    new ListItemParameter<string> { Name = "car", Value = "BMW" },
    new ListItemParameter<string> { Name = "color", Value = "red" }
};

// Tests

Result result;
string? errorMessage;
long version;

//
// Parsing exception
//
result = nestClient.IndexDoc<ColorDoc>(null, "test-1",
    out errorMessage,
    out version);
Console.WriteLine("\nResult: " + result);
Console.WriteLine("Error: " + (errorMessage ?? "null"));
Console.WriteLine("Version: " + version);
//Result: Error
//Error: Request failed to execute.
//  Call: Status code 400 from: POST /test-1/_doc.
//  ServerError:
//    Type: mapper_parsing_exception
//    Reason: "failed to parse"
//    ...
//Version: 0

//
// POST /<index name>/_doc/
//
for (int i = 0; i < 3; i++)
{
    result = nestClient.IndexDoc(doc, "test-1",
        out errorMessage,
        out version);

    Console.WriteLine("\nResult: " + result);
    Console.WriteLine("Error: " + (errorMessage ?? "null"));
    Console.WriteLine("Version: " + version);
}
//Result: Created
//Error: null
//Version: 1
//Result: Created
//Error: null
//Version: 1
//Result: Created
//Error: null
//Version: 1

//
// ifIndexExists: false
// 
result = nestClient.IndexDoc(doc, "test-2",
    out errorMessage,
    out version,
    ifIndexExists: true);
Console.WriteLine("\nResult: " + result);
Console.WriteLine("Error: " + (errorMessage ?? "null"));
Console.WriteLine("Version: " + version);
//Result: Error
//Error: Index "test-2" does not exist.
//Version: 0

//
// allowUpdate: false
//
nestClient.IndexDoc(doc, "test-3",
    out _,
    out _,
    id: "id123");
result = nestClient.IndexDoc(doc, "test-3",
    out errorMessage,
    out version,
    id: "id123",
    allowUpdate: false);
Console.WriteLine("\nResult: " + result);
Console.WriteLine("Error: " + (errorMessage ?? "null"));
Console.WriteLine("Version: " + version);
//Result: Error
//Error: The document already exists.
//Version: 0