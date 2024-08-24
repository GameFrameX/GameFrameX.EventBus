using System;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace GameFrameX.Shashlik.EventBus.Storage.MongoDb;

public class DefaultConnection : IConnection
{
    private readonly Lazy<IMongoClient> _connectionString;

    public DefaultConnection(
        IOptions<EventBusMongoDbOptions> options)
    {
        Options           = options.Value;
        _connectionString = new Lazy<IMongoClient>(Get);
    }

    private EventBusMongoDbOptions Options { get; }

    public IMongoClient Client
    {
        get { return _connectionString.Value; }
    }

    public IMongoClient Get()
    {
        return new MongoClient(Options.ConnectionString);
    }
}