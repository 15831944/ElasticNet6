using System.Security.Cryptography.X509Certificates;
using Elastic.Docs;
using Elasticsearch.Net;
using Nest;

namespace Elastic;

public class NestClient
{
#nullable disable
    private ElasticClient elasticClient;
    public ElasticClient ElasticClient => elasticClient;
    public readonly List<Node> Nodes = new();
    public readonly string ElasticsearchUserName;
    public readonly string HttpsCertificatePath;
    private readonly string elasticsearchUserPassword;
#nullable enable
    private readonly string? httpsCertificatePassword;

    public NestClient(
        IEnumerable<string> nodesUrls,
        string elasticsearchUserName,
        string elasticsearchUserPassword,
        string httpsCertificatePath,
        string? httpsCertificatePassword
    )
    {
        foreach (string url in nodesUrls)
        {
            Nodes.Add(new Node(new Uri(url)));
        }

        ElasticsearchUserName = elasticsearchUserName;
        HttpsCertificatePath = httpsCertificatePath;
        this.elasticsearchUserPassword = elasticsearchUserPassword;
        this.httpsCertificatePassword = httpsCertificatePassword;
    }

    public bool Connect(out Exception? exception)
    {
        StaticConnectionPool connectionPool = new(Nodes);
        ConnectionSettings connectionSettings = new(connectionPool);
        bool result = true;
        exception = null;

        try
        {
            connectionSettings.PingTimeout(TimeSpan.FromSeconds(5));
            connectionSettings.BasicAuthentication(ElasticsearchUserName, elasticsearchUserPassword);
            connectionSettings.DisableDirectStreaming(true);
            connectionSettings.ServerCertificateValidationCallback(
                (sender, cert, chain, errors) =>
                {
                    X509Certificate clientCert = new(HttpsCertificatePath, httpsCertificatePassword);
                    return cert.GetCertHashString() == clientCert.GetCertHashString();
                });

            elasticClient = new(connectionSettings);
            result = elasticClient.Cluster.Stats().IsValid;
        }
        catch (Exception ex)
        {
            exception = ex;
            result = false;
        }

        if (!result)
        {
            exception = new Exception("Failed to establish the connection with Elasticsearch.");
        }

        return result;
    }

    #region Index
    /// <summary>
    /// Создает пустой индекс для <see cref="CodeTextsDoc"/>.
    /// </summary>
    public CreateIndexResponse CreateIndexOfCodeTextsDoc(string name)
    {
        CreateIndexResponse result = elasticClient.Indices
            .Create(name, s => s
                .Map<CodeTextsDoc>(ms => ms
                    .Properties(p => p
                        .Keyword(k => k.Name(n => n.Code))
                        .Object<ListItemParameter<string>>(o => o
                            .Name(n => n.TextParameters)
                            .Properties(p => p
                                .Keyword(k => k.Name(n => n.Name))
                                .Text(t => t.Name(n => n.Value))
                            )
                        )
                    )
                    .Meta(m => m
                        .Add("created", DateTime.Now)
                        .Add("type", typeof(CodeTextsDoc).Name)
                    )
                )
            );

        return result;
    }

    /// <summary>
    /// Создает пустой индекс для <see cref="CodeTextsDoc"/>.
    /// </summary>
    /// <param name="errorMessage">
    /// <see cref="ResponseBase.OriginalException"/>.Message.<br/>
    /// Содержит сообщение <see cref="ResponseBase.ServerError"/>, если сервер отвечает.
    /// </param>
    public bool TryCreateIndexOfCodeTextsDoc(string name, out string? errorMessage)
    {
        CreateIndexResponse result = CreateIndexOfCodeTextsDoc(name);

        errorMessage = result.Acknowledged ? null : result.OriginalException.Message;

        return result.Acknowledged;
    }
    #endregion

    #region Document
    public bool TryAddCodeTextsDoc(CodeTextsDoc doc, string indexName, out string? errorMessage)
    {
        IndexResponse response = elasticClient.Index(doc, i => i.Index(indexName));

        errorMessage = response.IsValid ? null : response.OriginalException.Message;

        return response.IsValid;
    }
    #endregion
}
