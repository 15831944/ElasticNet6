using Elastic;

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

do
{
    Console.Write("Index name: ");

    string? indexName = Console.ReadLine();

    if (string.IsNullOrEmpty(indexName))
    {
        Console.WriteLine("Write a valid index name.");   
        continue;
    }

    try
    {
        var result = nestClient.CreateIndexOfColorDoc(indexName);

        if (result.Acknowledged)
        {
            Console.WriteLine($"Index \"{result.Index}\" created.");
            Console.WriteLine($"Shards acknowledged: {result.ShardsAcknowledged}.");
        }
        else
        {
            Console.WriteLine("Server side error description: " + result.ServerError?.Error.Reason);
            Console.WriteLine("Client side error description: " + result.OriginalException?.Message); // Contains ServerError.Error.Reason.
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
    }
}
while (true);
