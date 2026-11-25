using System.Net;
using System.Net.Mail;

namespace MamiMotovlog.Services;

public sealed class RiderMailer(IConfiguration config, IWebHostEnvironment env, ILogger<RiderMailer> logger)
{
    public bool CanSend => !string.IsNullOrWhiteSpace(config["Mail:SmtpHost"]) &&
                           !string.IsNullOrWhiteSpace(config["Mail:From"]) &&
                           !string.IsNullOrWhiteSpace(config["Mail:Password"]);

    public async Task SendAsync(string to, string subject, string body, string? actionUrl = null, string? actionLabel = null)
    {
        if (env.IsDevelopment() && config.GetValue<bool>("Mail:LocalConsoleOnly"))
        {
            logger.LogWarning("SADECE GELİŞTİRME ORTAMI | Mail to {Receiver}: {Subject} | {Body}", to, subject, body + "\n" + actionUrl);
            return;
        }
        if (!CanSend) throw new InvalidOperationException("SMTP yapılandırması eksik. Plesk e-posta ayarlarını tanımlayın.");
        using var smtp = new SmtpClient(config["Mail:SmtpHost"], config.GetValue<int?>("Mail:SmtpPort") ?? 587)
        {
            EnableSsl = config.GetValue("Mail:EnableTls", true),
            Credentials = new NetworkCredential(config["Mail:User"], config["Mail:Password"]),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 15000
        };
        var plain = body + (string.IsNullOrWhiteSpace(actionUrl) ? "" : "\n\n" + actionLabel + ": " + actionUrl);
        using var message = new MailMessage(config["Mail:From"]!, to, subject, plain);
        var safeBody = WebUtility.HtmlEncode(body).Replace("\r\n", "\n").Replace("\n", "<br>");
        var button = string.IsNullOrWhiteSpace(actionUrl) ? "" :
            $"<p style='margin:30px 0'><a style='display:inline-block;background:#205954;color:#fff;padding:15px 20px;border-radius:8px;text-decoration:none;font-weight:bold' href='{WebUtility.HtmlEncode(actionUrl)}'>{WebUtility.HtmlEncode(actionLabel ?? "Devam et")}</a></p>";
        var html = $"<!doctype html><html lang='tr'><meta charset='utf-8'><meta name='viewport' content='width=device-width'><body style='margin:0;background:#f0f3f0;font-family:Arial,sans-serif;color:#243c39'><table role='presentation' width='100%' cellspacing='0' cellpadding='18'><tr><td align='center'><table role='presentation' width='100%' style='max-width:580px;background:white;border:1px solid #dce3de;border-radius:12px' cellspacing='0' cellpadding='28'><tr><td style='background:#173d3a;color:white;font-size:20px;font-weight:bold;letter-spacing:2px'>MAMI MOTOVLOG</td></tr><tr><td><h1 style='font-size:23px;margin:0 0 22px'>{WebUtility.HtmlEncode(subject.Replace("MAMI MOTOVLOG · ", ""))}</h1><p style='line-height:1.65'>{safeBody}</p>{button}<p style='font-size:13px;color:#63736f'>Güvenli yolculuklar, <br>MAMI MOTOVLOG</p></td></tr><tr><td style='font-size:12px;color:#677773;background:#f5f7f5'>Bu e-posta mamimotovlog.com tarafından otomatik gönderilmiştir.</td></tr></table></td></tr></table></body></html>";
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html, System.Text.Encoding.UTF8, "text/html"));
        message.BodyEncoding = System.Text.Encoding.UTF8;
        message.SubjectEncoding = System.Text.Encoding.UTF8;
        await smtp.SendMailAsync(message);
    }
}
