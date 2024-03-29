﻿using Elastic;

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
// POST _aliases
// {
//   "actions": [
//     {
//       "add": {
//         "index": "test*",
//         "alias": "test"
//       }
//     }
//   ]
// }

while (true)
{
    Console.Write("Alias: ");
    
    string alias = Console.ReadLine()!;
    bool success = nestClient.TryGetIndicesByAlias(alias, out string info, out string[] indices, notFoundAsSuccess: false);

    Console.WriteLine("Success: " + success);
    Console.WriteLine("Info: " + info);
    Console.WriteLine("Indices: " + string.Join(", ", indices));
    Console.WriteLine();
}

// Work

// Alias: test
// Success: True
// Info: OK
// Indices: test-1, test-2
   
// Alias: xxx
// Success: False
// Info: The alias was not found.
// Indices:
   
// Alias: test-1
// Success: False
// Info: The alias was not found.
// Indices:
