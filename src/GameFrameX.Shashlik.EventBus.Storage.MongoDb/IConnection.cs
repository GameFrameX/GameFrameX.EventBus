using MongoDB.Driver;

namespace GameFrameX.Shashlik.EventBus.Storage.MongoDb;

public interface IConnection
{
    IMongoClient Client { get; }
}