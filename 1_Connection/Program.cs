using Elasticsearch.Net;
using Nest;

Node[] nodes = { new Node(new Uri("http://localhost:9200")) };
StaticConnectionPool connectionPool = new(nodes);
ConnectionSettings connectionSettings = new(connectionPool);

connectionSettings.PingTimeout(TimeSpan.FromSeconds(5));
connectionSettings.BasicAuthentication("elastic", "ela5tic");
connectionSettings.DisableDirectStreaming(true);

ElasticClient client = new(connectionSettings);

// Test
Func<GetDescriptor<object>, IGetRequest> getSelector = s => s.Index("index-1");
var doc_1 = client.Get<object>("1", getSelector); // exists
var doc_2 = client.Get<object>("2", getSelector); // does not exists

Console.WriteLine(doc_1.Found);
if (!doc_1.Found)
    Console.WriteLine(doc_1.DebugInformation);

Console.WriteLine(doc_2.Found);
if (!doc_2.Found)
    Console.WriteLine(doc_2.DebugInformation);

// [Authenticated]
// Invalid NEST response built from a successful (404) low level call on GET: /index-1/_doc/2
// # Response:
// {"_index":"index-1","_id":"2","found":false}
//
// [Not authenticated]
// Invalid NEST response built from a unsuccessful (401) low level call on GET: /index-1/_doc/2
// # OriginalException:
//     Elasticsearch.Net.ElasticsearchClientException:
//         Failed to ping the specified node. Call: Status code 401 from: HEAD /
// ---> Elasticsearch.Net.PipelineException: Failed to ping the specified node.
// ---> Elasticsearch.Net.PipelineException: Could not authenticate with the specified node.
//      Try verifying your credentials or check your Shield configuration.
Console.ReadKey();