using MamiMotovlog.Data;
using MamiMotovlog.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace MamiMotovlog.Services;
public sealed class AdminAuditFilter(IServiceScopeFactory scopes, IConfiguration config, ILogger<AdminAuditFilter> logger) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var result=await next();
        var request=context.HttpContext.Request;
        if(!config.GetValue<bool>("Rider:Enabled") || !HttpMethods.IsPost(request.Method) || !request.Path.StartsWithSegments("/yonetim") || request.Path.StartsWithSegments("/yonetim/giris") || request.Path.StartsWithSegments("/yonetim/cikis") || result.Exception is not null || (context.Controller is Controller controller && controller.TempData.ContainsKey("Error")) || !context.ModelState.IsValid || result.Result is not (RedirectToActionResult or RedirectResult)) return;
        // Do not include query strings, form bodies, tokens, passwords or email addresses.
        try { using var scope=scopes.CreateScope(); var db=scope.ServiceProvider.GetRequiredService<RiderDbContext>(); var operation = context.ActionDescriptor.AttributeRouteInfo?.Template ?? "yonetim"; db.AdminAudits.Add(new AdminAudit{Operation=operation.Length>250?operation[..250]:operation,CreatedUtc=DateTime.UtcNow}); await db.SaveChangesAsync(); }
        catch(Exception ex){logger.LogWarning(ex,"Yönetici işlem kaydı yazılamadı");}
    }
}
