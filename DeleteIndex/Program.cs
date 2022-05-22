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

while(true)
{
    Console.Write("Index name: ");
    
    string name = Console.ReadLine()!;
    bool success = nestClient.TryDeleteIndex(name, out string info, true);

    Console.WriteLine("Success: " + success);
    Console.WriteLine("Info: " + info);
    Console.WriteLine();
}

//The connection with elasticsearch was successfully established.
//Index name: test
//Success: True
//Info: OK

//Index name: x 1 @#@$<>
//Success: True
//Info: Server error: "no such index [x 1 @#@$<>]"