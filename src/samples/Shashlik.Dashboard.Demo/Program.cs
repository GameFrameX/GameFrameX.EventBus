using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Shashlik.Dashboard.Demo;
using Shashlik.EventBus;
using Shashlik.EventBus.Dashboard;
using Shashlik.EventBus.MemoryQueue;
using Shashlik.EventBus.MongoDb;
using Shashlik.EventBus.MySql;
using Shashlik.EventBus.PostgreSQL;
using Shashlik.EventBus.SqlServer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

Console.WriteLine("��ѡ�����ݿ�����:");
Console.WriteLine("1: mysql");
Console.WriteLine("2: postgres");
Console.WriteLine("3: sqlserver");
Console.WriteLine("4: mongodb");

var type = Console.ReadLine();

string connectionString;
switch (type)
{
    case "1":
        connectionString = builder.Configuration.GetValue<string>("mysql");
        break;
    case "2":
        connectionString = builder.Configuration.GetValue<string>("postgres");
        break;
    case "3":
        connectionString = builder.Configuration.GetValue<string>("sqlserver");
        break;
    case "4":
        connectionString = builder.Configuration.GetValue<string>("mongodb");
        builder.Services.AddSingleton<IMongoClient>(new MongoClient(connectionString));
        break;
    default:
        throw new ArgumentException();
}

if (type != "4")
{
    builder.Services.AddDbContext<DataContext>(
        x =>
        {
            switch (type)
            {
                case "1":
                    x.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                    break;
                case "2":
                    x.UseNpgsql(connectionString);
                    break;
                case "3":
                    x.UseSqlServer(connectionString);
                    break;
                default:
                    break;
            }
        });
}

var eventBusBuilder = builder.Services.AddEventBus(r =>
{
    // ��Щ����ȱʡ���ã�����ֱ��services.AddEventBus()
    // ���л�����ע�ᵽMQ���¼����ƺ��¼��������ƻ���ϴ˺�׺
    r.Environment = "Production";
    // ���ʧ�����Դ�����Ĭ��60��
    r.RetryFailedMax = 1;
    // ��Ϣ���Լ����Ĭ��2����
    r.RetryInterval = 1;
    // ����������Ϣ�������ƣ�Ĭ��100
    r.RetryLimitCount = 100;
    // �ɹ�����Ϣ����ʱ�䣬Ĭ��3�죬ʧ�ܵ���Ϣ�������ڣ����봦��
    r.SucceedExpireHour = 24 * 3;
    // ��Ϣ����ʧ�ܺ�����������ʱ�䣬Ĭ��5���Ӻ�
    r.StartRetryAfter = 1;
    // �����ύ��ʱʱ��,��λ��,Ĭ��60��
    r.TransactionCommitTimeout = 60;
    // ������ִ��ʱ��Ϣ����ʱ��
    r.LockTime = 110;
});
switch (type)
{
    case "1":
        eventBusBuilder = eventBusBuilder.AddMySql<DataContext>();
        break;
    case "2":
        eventBusBuilder = eventBusBuilder.AddNpgsql<DataContext>();
        break;
    case "3":
        eventBusBuilder = eventBusBuilder.AddSqlServer<DataContext>();
        break;
    case "4":
        eventBusBuilder = eventBusBuilder.AddMongoDb(connectionString);
        break;
    default:
        throw new ArgumentException();
}

// ʹ��ef DbContext mysql
eventBusBuilder
    .AddMemoryQueue()
    // ע��dashboard service, ��ʹ���Զ�����֤��TokenCookieAuth
    .AddDashboard<TokenCookieAuth>()
    ;

var app = builder.Build();

if (type != "4")
{
    using var serviceScope = app.Services.CreateScope();
    var dataContext = serviceScope.ServiceProvider.GetRequiredService<DataContext>();
    dataContext.Database.Migrate();
}

// Configure the HTTP request pipeline.

app.UseAuthorization();
app.UseRouting();
// ���� dashboard
//app.UseEventBusDashboard();

app.MapControllers();

app.Run();