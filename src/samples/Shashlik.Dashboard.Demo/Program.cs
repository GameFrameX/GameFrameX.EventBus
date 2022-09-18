using Microsoft.EntityFrameworkCore;
using Shashlik.Dashboard.Demo;
using Shashlik.EventBus;
using Shashlik.EventBus.Dashboard;
using Shashlik.EventBus.MemoryQueue;
using Shashlik.EventBus.MySql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
var connectionString = builder.Configuration.GetValue<string>("Mysql");
builder.Services.AddDbContext<DataContext>(
    x => x.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
builder.Services.AddEventBus(r =>
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
    })
    // ʹ��ef DbContext mysql
    .AddMySql<DataContext>()
    .AddMemoryQueue()
    .AddDashboard<TokenCookieAuth>()
    ;

var app = builder.Build();
using var serviceScope = app.Services.CreateScope();
var dataContext = serviceScope.ServiceProvider.GetRequiredService<DataContext>();
dataContext.Database.Migrate();

// Configure the HTTP request pipeline.

app.UseAuthorization();
app.UseRouting();
app.UseEventBusDashboard();

app.MapControllers();

app.Run();