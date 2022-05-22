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

