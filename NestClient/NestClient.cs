﻿using Elastic.Docs;
using Elasticsearch.Net;
using Nest;
using System.Security.Cryptography.X509Certificates;

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

    public bool Connect(out Exception? exception, bool enableApiVersioningHeader = true)
    {
        StaticConnectionPool connectionPool = new(Nodes);
        ConnectionSettings connectionSettings = new(connectionPool);
        bool result = true;
        exception = null;

        try
        {
            connectionSettings.PingTimeout(TimeSpan.FromSeconds(5));
            connectionSettings.BasicAuthentication(ElasticsearchUserName, elasticsearchUserPassword);
            connectionSettings.EnableApiVersioningHeader(enableApiVersioningHeader); // A solutin for "BulkAll halted after receiving failures that can not be retried from _bulk".
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

        if (!result && exception is null)
        {
            exception = new Exception("Failed to establish the connection with Elasticsearch.");
        }

        return result;
    }

    #region Index
    /// <summary>
    /// Creates an empty index with type <see cref="ColorDoc"/>.
    /// </summary>
    public CreateIndexResponse CreateIndexOfColorDoc(string name,
        int numberOfShards = 2,
        int numberOfReplicas = 2)
    {
        CreateIndexResponse result = elasticClient.Indices
            .Create(name, s => s
                .Map<ColorDoc>(ms => ms
                    .Properties(p => p
                        .Keyword(k => k.Name(n => n.Code))
                        .Text(t => t.Name(n => n.Name))
                    )
                    .Meta(m => m
                        .Add("created", DateTime.Now)
                        .Add("type", typeof(ColorDoc).Name)
                    )
                )
                .Settings(s => s
                    .NumberOfShards(numberOfShards)
                    .NumberOfReplicas(numberOfReplicas)
                )
            );

        return result;
    }

    /// <summary>
    /// Creates an empty index with type <see cref="ColorDoc"/>.
    /// </summary>
    /// <param name="errorMessage">
    /// <see cref="ResponseBase.OriginalException"/>.Message.<br/>
    /// Contains a <see cref="ResponseBase.ServerError"/> message if the server is not responding.
    /// </param>
    public bool TryCreateIndexOfColorDoc(string name, out string? errorMessage,
        int numberOfShards = 2,
        int numberOfReplicas = 2)
    {
        CreateIndexResponse result = CreateIndexOfColorDoc(name, numberOfShards, numberOfReplicas);

        errorMessage = result.Acknowledged ? null : result.OriginalException.Message;

        return result.Acknowledged;
    }

    /// <summary>
    /// Creates an empty index with type <see cref="CodeTextsDoc"/>.
    /// </summary>
    public CreateIndexResponse CreateIndexOfCodeTextsDoc(string name,
        int numberOfShards = 2,
        int numberOfReplicas = 2
        )
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
                .Settings(s => s
                    .NumberOfShards(numberOfShards)
                    .NumberOfReplicas(numberOfReplicas)
                )
            );

        return result;
    }

    /// <summary>
    /// Creates an empty index with type <see cref="CodeTextsDoc"/>.
    /// </summary>
    /// <param name="errorMessage">
    /// <see cref="ResponseBase.OriginalException"/>.Message.<br/>
    /// Contains a <see cref="ResponseBase.ServerError"/> message if the server is not responding.
    /// </param>
    public bool TryCreateIndexOfCodeTextsDoc(string name, out string? errorMessage,
        int numberOfShards = 2,
        int numberOfReplicas = 2)
    {
        CreateIndexResponse result = CreateIndexOfCodeTextsDoc(name, numberOfShards, numberOfReplicas);

        errorMessage = result.Acknowledged ? null : result.OriginalException.Message;

        return result.Acknowledged;
    }

    /// <summary>
    /// Deletes the specified index.
    /// </summary>
    /// <returns><c>true</c> if the operation completed with code 200 or 404.</returns>
    public bool TryDeleteIndex(string name, out string info, bool notFoundAsSuccess = true)
    {
        DeleteIndexResponse response = elasticClient.Indices.Delete(name);

        if (response.IsValid)
        {
            info = "OK";
            return true;
        }

        if (response.OriginalException is not null)
        {
            info = $"Client error: \"{response.OriginalException.Message}\"";
            return false;
        }

        if (response.ServerError is not null)
        {
            info = $"Server error: \"{response.ServerError.Error.Reason}\"";
            return notFoundAsSuccess && response.ServerError.Status == 404;
        }

        info = response.DebugInformation;
        return false;
    }
    #endregion

    #region Document
    /// <summary>
    /// Indexes the document.
    /// </summary>
    /// <param name="ifIndexExists">If true, then the document will not be created if there is no index.</param>
    /// <param name="id">If specified, then its document will be updated.</param>
    /// <param name="version">The indexed document version, starts at 1.</param>
    public Result IndexDoc<TDoc>(string indexName, TDoc doc,
        // out:
        out string? errorMessage,
        out long version,
        // options:
        bool ifIndexExists = false,
        bool allowUpdate = true,
        string? id = null
        ) where TDoc : class
    {
        version = 0;

        if (ifIndexExists)
        {
            ExistsResponse er = elasticClient.Indices.Exists(indexName);
            
            if (!er.Exists)
            {
                errorMessage = $"Index \"{indexName}\" does not exist.";               
                return Result.Error;
            }
        }

        if (!allowUpdate && id is not null)
        {
            ExistsResponse er = elasticClient.DocumentExists(new DocumentPath<TDoc>(id), s => s.Index(indexName));

            if (er.Exists)
            {
                errorMessage = "The document already exists.";
                return Result.Error;
            }
        }

        IndexResponse ir = elasticClient.Index(doc, i => i.Index(indexName).Id(id));
        errorMessage = ir.IsValid ? null : ir.OriginalException.Message;
        version = ir.Version;
        
        return ir.Result;
    }
    #endregion

    #region Bulk
    /// <summary>
    /// Bulk indexing of documents with an additional request to refresh the index after all bulk operations have been performed.
    /// </summary>
    /// 
    /// <param name="bulkResponseCallback">
    /// Intercepts the <see cref="BulkResponse"/> for each batch.<br/>
    /// Contains an <see cref="BulkResponse.Errors">error code flag</see>,
    /// <see cref="ResponseBase.DebugInformation">debug information</see>,
    /// <see cref="ResponseBase.OriginalException">original</see> and <see cref="ResponseBase.ServerError">server</see> errors.
    /// </param>
    /// 
    /// <param name="retryDocumentPredicate">
    /// A predicate to control which documents should be retried.<br/>
    /// If the number of retries exceeds the maximum value set in the methods <see cref="BulkAllDescriptor{T}.BackOffRetries(int?)"/> the method will fail.
    /// </param>
    /// 
    /// <param name="droppedDocumentCallback">
    /// Intercepts the documents of the failed batch.<br/>
    /// Used to feed into a dead letter queue.
    /// </param>
    /// 
    /// <param name="onNext">
    /// Intercepts the <see cref="BulkAllResponse"/> for each <u>successful</u> batch.<br/>
    /// Contains the <see cref="BulkAllResponse.Page">number of the batch</see>, its <see cref="BulkAllResponse.Items">items</see> and <see cref="BulkAllResponse.Retries">retries</see>.
    /// </param>
    /// 
    /// <param name="continueAfterDroppedDocuments">
    /// If <c>true</c> a failed bulk operation does not stop indexing of the following batches.<br/>
    /// Otherwise bulk all indexing will be halted.
    /// </param>
    /// 
    /// <returns>
    /// <code><paramref name="e"/> is null</code>
    /// </returns>
    public bool BulkIndex<TDoc>(string indexName, IEnumerable<TDoc> docs,
        // out:
        out long totalNumberOfRetries,
        out long totalNumberOfFailedBuffers,
        out Exception? e,
        
        CancellationToken? cancellationToken = null,
        
        // settings:
        string timeBetweenRetries = "2s",
        int numberOfRetries = 2,
        int portionSize = 5000,
        int maximumRuntimeSeconds = 60,
        bool continueAfterDroppedDocuments = true,
        
        // handlers:
        Action<BulkResponse>? bulkResponseCallback = null,
        Action<BulkResponseItemBase, TDoc>? droppedDocumentCallback = null,
        Func<BulkResponseItemBase, TDoc, bool>? retryDocumentPredicate = null,
        Action<BulkAllResponse>? onNext = null
        ) where TDoc : class
    {
        BulkAllObservable<TDoc> observable = elasticClient.BulkAll(docs, b => b
            .Index(indexName)
            .Size(portionSize)

            .BackOffTime(timeBetweenRetries)
            .BackOffRetries(numberOfRetries)

            .BulkResponseCallback(bulkResponseCallback)
            .RetryDocumentPredicate(retryDocumentPredicate)
            .DroppedDocumentCallback(droppedDocumentCallback)

            .RefreshOnCompleted()
            .ContinueAfterDroppedDocuments(continueAfterDroppedDocuments)
            .MaxDegreeOfParallelism(Environment.ProcessorCount), 
            cancellationToken ?? default);

        try
        {
            BulkAllObserver observer = observable.Wait(TimeSpan.FromSeconds(maximumRuntimeSeconds), onNext);

            totalNumberOfRetries = observer.TotalNumberOfRetries;
            totalNumberOfFailedBuffers = observer.TotalNumberOfFailedBuffers;
            e = null;
        }
        catch (Exception ex)
        {
            totalNumberOfRetries = -1;
            totalNumberOfFailedBuffers = -1;
            e = ex;
        }

        return e is null;
    }
    #endregion

    #region Alias
    /// <summary>
    /// Associates the alias only with the specified index.
    /// </summary>
    public bool TryTransferAlias(string alias, string newIndexName, out string info)
    {
        BulkAliasResponse response = elasticClient.Indices.BulkAlias(d => d
            .Remove(r => r.Index("*").Alias(alias))
            .Add(r => r.Index(newIndexName).Alias(alias)));

        info = response.IsValid ? "OK" : response.GetShortInfo();
        return response.IsValid;
    }

    /// <summary>
    /// Get indices by the name or associated with the alias.
    /// </summary>
    public bool TryGetIndices(string nameOrAlias, out string info, out string[] indices)
    {
        GetAliasResponse response = elasticClient.Indices.GetAlias(nameOrAlias);

        if (response.IsValid)
        {
            indices = response.Indices.Select(i => i.Key.Name).ToArray();
            info = "OK";
        }
        else
        {
            indices = Array.Empty<string>();
            info = response.GetShortInfo();
        }

        return response.IsValid;
    }

    /// <summary>
    /// Get indices associated with the alias only if it exists.
    /// </summary>
    public bool TryGetIndicesByAlias(string alias, out string info, out string[] indices, bool notFoundAsSuccess = true)
    {
        ExistsResponse aliasExistsResponse = elasticClient.Indices.AliasExists(alias);

        if (aliasExistsResponse.Exists)
        {
            return TryGetIndices(alias, out info, out indices);
        }

        indices = Array.Empty<string>();

        if (aliasExistsResponse.OriginalException is not null || aliasExistsResponse.ServerError is not null)
        {
            info = aliasExistsResponse.GetShortInfo();
            return false;
        }

        info = "The alias was not found.";
        return notFoundAsSuccess && true;
    }
    #endregion
}
