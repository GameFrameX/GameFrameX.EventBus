using GameFrameX.EventBus.Dashboard.Areas.ShashlikEventBus.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace GameFrameX.EventBus.Dashboard.Areas.ShashlikEventBus.Controllers;

/// <summary>
///     secret认证类
/// </summary>
[AllowAnonymous]
[Area(Consts.AreaName)]
public class AuthController : Controller
{
    private readonly IDataProtectionProvider                  _dataProtector;
    private readonly IOptionsMonitor<EventBusDashboardOption> _options;

    public AuthController(IOptionsMonitor<EventBusDashboardOption> options,
        IDataProtectionProvider                                    dataProtector)
    {
        _options       = options;
        _dataProtector = dataProtector;
    }

    [ViewData]
    public string UrlPrefix
    {
        get { return _options.CurrentValue.UrlPrefix; }
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Index(SecretLoginModel secretLoginModel)
    {
        if (secretLoginModel.Secret == _options.CurrentValue.AuthenticateSecret
            && _options.CurrentValue.AuthenticateProvider == typeof(SecretCookieAuthenticate))
        {
            Response.Cookies.Append(
                _options.CurrentValue.AuthenticateSecretCookieName ?? EventBusDashboardOption.DefaultCookieName,
                _dataProtector.CreateProtector(EventBusDashboardOption.DataProtectorName)
                              .Protect(secretLoginModel.Secret),
                _options.CurrentValue.AuthenticateSecretCookieOptions?.Invoke(HttpContext) ?? new CookieOptions
                    { Expires = DateTimeOffset.Now.AddHours(2), });

            return RedirectToAction("Index", "Published");
        }

        ViewBag.Error = "Secret error!";
        return View("Index");
    }
}