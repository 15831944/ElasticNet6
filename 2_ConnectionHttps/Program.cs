using Elasticsearch.Net;
using Nest;
using System.Security.Cryptography.X509Certificates;

Node[] nodes = { new Node(new Uri("https://elastic.home:9200")) };
StaticConnectionPool connectionPool = new(nodes);
ConnectionSettings connectionSettings = new(connectionPool);

connectionSettings.PingTimeout(TimeSpan.FromSeconds(5));
connectionSettings.BasicAuthentication("elastic", "ela5tic");
connectionSettings.DisableDirectStreaming(true);
connectionSettings.ServerCertificateValidationCallback(
    (sender, cert, chain, errors) =>
    {
        try
        {
            X509Certificate clientCert = new(@"C:\elasticsearch-8.1.1\config\http.p12", "http");
            return cert.GetCertHashString() == clientCert.GetCertHashString();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return false;
        }
    });

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

Console.ReadKey();