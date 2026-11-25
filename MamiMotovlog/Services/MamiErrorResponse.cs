using System.Net;
namespace MamiMotovlog.Services;
public static class MamiErrorResponse
{
    public static async Task WriteAsync(HttpContext context, string message)
    {
        if (context.Request.Headers["X-Mami-Async"] == "1" || context.Request.Headers["Accept"].ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase))
        {
            await context.Response.WriteAsJsonAsync(new { error = message });
            return;
        }
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.Headers["Cache-Control"] = "no-store";
        var safe = WebUtility.HtmlEncode(message);
        await context.Response.WriteAsync($"<!doctype html><html lang='tr'><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>MAMI MOTOVLOG | {context.Response.StatusCode}</title><body style='margin:0;background:#f5f5f2;color:#203b38;font:16px system-ui'><main style='max-width:560px;margin:12vh auto;padding:28px'><p>MAMI MOTOVLOG · {context.Response.StatusCode}</p><h1>İşlem tamamlanamadı</h1><p>{safe}</p><p><a href='/'>Ana sayfa</a> · <a href='/garaj'>Garajım</a></p></main></body></html>");
    }
}
