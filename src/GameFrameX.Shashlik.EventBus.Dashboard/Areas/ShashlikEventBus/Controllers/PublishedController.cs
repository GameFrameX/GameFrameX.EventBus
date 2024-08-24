using GameFrameX.Shashlik.EventBus.Dashboard.Areas.ShashlikEventBus.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shashlik.EventBus;

namespace GameFrameX.Shashlik.EventBus.Dashboard.Areas.ShashlikEventBus.Controllers;

public class PublishedController : BaseDashboardController
{
    private readonly IMessageStorage _messageStorage;

    public PublishedController(IOptionsMonitor<EventBusDashboardOption> options, IMessageStorage messageStorage) :
        base(options)
    {
        _messageStorage = messageStorage;
    }


    public async Task<IActionResult> Index(string eventName, MessageStatus status, int pageSize = 20, int pageIndex = 1)
    {
        ViewBag.Title = "Published";
        ViewBag.Page  = "Published";
        var model = new MessageViewModel
        {
            StatusCount = await _messageStorage.GetPublishedMessageStatusCountsAsync(CancellationToken.None),
        };
        if (status == MessageStatus.None && model.StatusCount.Keys.Count > 0)
        {
            status = model.StatusCount.Keys.First();
        }

        model.Messages = await _messageStorage.SearchPublishedAsync(eventName, status, (pageIndex - 1) * pageSize,
                                                                    pageSize, CancellationToken.None);
        model.PageIndex = pageIndex;
        model.PageSize  = pageSize;
        model.EventName = eventName;
        var total = 0M;
        if (status != MessageStatus.None)
        {
            total = model.StatusCount.TryGetValue(status, out var value) ? value : total;
        }

        model.TotalPage = Convert.ToInt32(Math.Ceiling(total / pageSize));
        return View("Messages", model);
    }

    public async Task Retry(string[] ids, [FromServices] IPublishedMessageRetryProvider publishedMessageRetryProvider)
    {
        if (ids == null)
        {
            return;
        }

        foreach (var id in ids)
        {
            try
            {
                await publishedMessageRetryProvider.RetryAsync(id, CancellationToken.None);
            }
            catch (Exception)
            {
                //
            }
        }
    }
}