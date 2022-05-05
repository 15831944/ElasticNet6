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
    Console.WriteLine("The connection with Elasticsearch was successfully established.");
}
else
{
    Console.WriteLine(e!.Message);
    Console.ReadLine();

    return;
}
#endregion

CodeTextsDoc doc = new CodeTextsDoc();
doc.Code = "code123";
doc.TextParameters = new List<ListItemParameter<string>>
{
    new ListItemParameter<string> { Name = "car", Value = "BMW" },
    new ListItemParameter<string> { Name = "color", Value = "red" }
};

if (nestClient.TryAddCodeTextsDoc(doc, "test", out string? errorMessage))
{
    Console.WriteLine("Created.");
}
else
{
    Console.WriteLine(errorMessage);
}