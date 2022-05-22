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

// DELETE *
// PUT test-1
// PUT test-2

while (true)
{
    Console.Write("Alias name: ");
    string aliasName = Console.ReadLine()!;

    Console.Write("New index name: ");
    string newIndexName = Console.ReadLine()!;

    bool success = nestClient.TryTransferAlias(aliasName, newIndexName, out string info);

    Console.WriteLine("Success: " + success);
    Console.WriteLine("Info: " + info);
    Console.WriteLine();
}

// Work:

// Alias name: test
// New index name: test-1
// Success: True
// Info: OK
// (GET test --> test-1)

// Alias name: test
// New index name: test-2
// Success: True
// Info: OK
// (GET test --> test-2)

// Alias name: te st
// New index name: te st 3
// Success: False
// Info: Server error: "no such index [te st 3]"
// (GET test --> test-2)

// Alias name: te st
// New index name: test-1
// Success: False
// Info: Client error: "Request failed to execute.
//   Call: Status code 400 from: POST /_aliases.
//   ServerError:
//     Type: invalid_alias_name_exception
//     Reason: "Invalid alias name [te st]: must not contain the following characters [ , ", *, \, <, |, ,, >, /, ?]""
// (GET test --> test-2)
