using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shashlik.EventBus.Utils;

namespace GameFrameX.Shashlik.EventBus.Storage.MySql;

public class DefaultConnectionString : IConnectionString
{
    private readonly Lazy<string> _connectionString;

    public DefaultConnectionString(IServiceScopeFactory serviceScopeFactory, IOptions<EventBusMySqlOptions> options)
    {
        ServiceScopeFactory = serviceScopeFactory;
        Options             = options.Value;
        _connectionString   = new Lazy<string>(GetConnectionString);
    }

    private IServiceScopeFactory ServiceScopeFactory { get; }
    private EventBusMySqlOptions Options             { get; }

    public string ConnectionString
    {
        get { return _connectionString.Value; }
    }

    private string GetConnectionString()
    {
        if (Options.DbContextType is null)
        {
            if (Options.ConnectionString!.IsNullOrWhiteSpace())
            {
                throw new OptionsValidationException(
                    nameof(Options.ConnectionString),
                    typeof(EventBusMySqlOptions),
                    new[] { "ConnectionString and DbContextType can't all be empty", });
            }

            return Options.ConnectionString!;
        }

        using var scope     = ServiceScopeFactory.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService(Options.DbContextType) as DbContext;
        return dbContext!.Database.GetDbConnection().ConnectionString;
    }
}