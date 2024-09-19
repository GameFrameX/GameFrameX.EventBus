using MongoDB.Driver;

namespace GameFrameX.EventBus.Storage.MongoDb;

public interface IConnection
{
    IMongoClient Client { get; }
}