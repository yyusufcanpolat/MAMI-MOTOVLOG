using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using MamiMotovlog.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using MamiMotovlog.Models;
using MamiMotovlog.Services;

var builder = WebApplication.CreateBuilder(args);
// HTML number inputs and route geometry use invariant decimal dots, regardless of IIS server locale.
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<AdminAuditFilter>();
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor((value, field) => $"{field} için geçerli bir değer girin.");
    options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(_ => "Bu alan zorunludur.");
    options.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(field => $"{field} sayısal olmalıdır.");
    options.ModelBindingMessageProvider.SetMissingBindRequiredValueAccessor(field => $"{field} zorunludur.");
});
builder.Services.AddDbContext<SiteDbContext>(options =>
{
    var provider = builder.Configuration["Database:Provider"] ?? (builder.Environment.IsDevelopment() ? "Sqlite" : "SqlServer");
    if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        var connection = builder.Configuration.GetConnectionString("SqlServer");
        if (string.IsNullOrWhiteSpace(connection)) throw new InvalidOperationException("SQL Server bağlantı dizesi eksik.");
        options.UseSqlServer(connection, sql => sql.EnableRetryOnFailure());
    }
    else if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "App_Data"));
        options.UseSqlite(builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=App_Data/mami.db");
    }
    else throw new InvalidOperationException("Database:Provider 'Sqlite' veya 'SqlServer' olmalı.");
});
// V3 Rider module uses a separate EF context: original V2 schema remains untouched.
builder.Services.AddDbContext<RiderDbContext>(options =>
{
    var provider = builder.Configuration["Database:Provider"] ?? (builder.Environment.IsDevelopment() ? "Sqlite" : "SqlServer");
    if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"), sql => sql.EnableRetryOnFailure());
    else
    {
        Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "App_Data"));
        options.UseSqlite("Data Source=App_Data/mami_rider_v3.db");
    }
});
builder.Services.AddIdentityCore<RiderMember>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = true;
    options.Password.RequiredLength = 10;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.MaxFailedAccessAttempts = 6;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(20);
})
.AddErrorDescriber<TurkishIdentityErrors>()
.AddEntityFrameworkStores<RiderDbContext>()
.AddSignInManager()
.AddDefaultTokenProviders();
builder.Services.Configure<DataProtectionTokenProviderOptions>(x => x.TokenLifespan = TimeSpan.FromHours(3));
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient("istanbul-fuel", client =>
{
    client.Timeout = TimeSpan.FromSeconds(25);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("MamiMotovlog/5.0 (+https://mamimotovlog.com)");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});
builder.Services.AddSingleton<IstanbulFuelPriceService>();
builder.Services.AddScoped<RiderMailer>();
builder.Services.AddScoped<RiderFileStore>();
builder.Services.AddScoped<RiderDeletion>();
builder.Services.AddScoped<AdminAuditFilter>();
builder.Services.AddHostedService<RiderReminderWorker>();
// The public routing provider requires an identifiable User-Agent. HttpClient sends none by default.
builder.Services.AddHttpClient("rider-routing", client =>
{
    client.Timeout = TimeSpan.FromSeconds(25);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("MamiMotovlog/4.0 (+https://mamimotovlog.com)");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});
// Kalıcı anahtarlar: IIS uygulama yeniden başladığında yönetici oturumu kaybolmaz.
var protection = builder.Services.AddDataProtection().SetApplicationName(builder.Environment.IsDevelopment() ? "MamiMotovlog.Development" : "MamiMotovlog.Production");
// Plesk'te yazılabilir bir yol tanımlanırsa anahtarları kalıcılaştırır.
// Paylaşımlı hostta izin verilmemiş dizine zorla yazmak 500.30'a yol açabilir.
var configuredKeysPath = builder.Configuration["DataProtection:KeysDirectory"];
if (!string.IsNullOrWhiteSpace(configuredKeysPath))
    protection.PersistKeysToFileSystem(new DirectoryInfo(configuredKeysPath));
builder.Services.AddAuthentication("Mami.Request")
    .AddPolicyScheme("Mami.Request", "Yönetici / üye oturum ayrımı", options =>
    {
        options.ForwardDefaultSelector = context => context.Request.Path.StartsWithSegments("/yonetim")
            ? CookieAuthenticationDefaults.AuthenticationScheme : IdentityConstants.ApplicationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/yonetim/giris";
        options.AccessDeniedPath = "/yonetim/giris";
        options.ExpireTimeSpan = TimeSpan.FromHours(6);
        options.SlidingExpiration = true;
        options.Events.OnValidatePrincipal = async context =>
        {
            var db = context.HttpContext.RequestServices.GetRequiredService<SiteDbContext>();
            var account = await db.AdminAccounts.AsNoTracking().FirstOrDefaultAsync();
            if (account is null || context.Principal?.FindFirst("admin_stamp")?.Value != AdminSessionStamp.FromHash(account.PasswordHash))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
        options.Cookie.Name = "Mami.Admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    });
builder.Services.AddAuthentication().AddCookie(IdentityConstants.ApplicationScheme, options =>
{
    options.Events.OnValidatePrincipal = async context =>
    {
        var manager = context.HttpContext.RequestServices.GetRequiredService<UserManager<RiderMember>>();
        var member = context.Principal is null ? null : await manager.GetUserAsync(context.Principal);
        if (member is null || !member.EmailConfirmed || await manager.IsLockedOutAsync(member))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return;
        }
        await SecurityStampValidator.ValidatePrincipalAsync(context);
    };
    options.Events.OnRedirectToLogin = async context =>
    {
        if (context.Request.Headers["X-Mami-Async"] == "1")
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Oturumunuz sona erdi. Yeniden giriş yapın." });
        }
        else context.Response.Redirect(context.RedirectUri);
    };
    options.Cookie.Name = "Mami.Rider";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.LoginPath = "/uye/giris";
    options.AccessDeniedPath = "/uye/giris";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(4);
});
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("contact", context => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5, Window = TimeSpan.FromMinutes(10), QueueLimit = 0, AutoReplenishment = true
        }));
    options.AddPolicy("routing", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 24, Window = TimeSpan.FromMinutes(10), QueueLimit = 0, AutoReplenishment = true }));
    foreach (var (name, limit) in new[] { ("login", 12), ("admin-login", 12), ("register", 6), ("mail", 5), ("reset", 10), ("account-delete", 6), ("admin-password", 6), ("fuel", 30), ("weather", 30) })
    {
        options.AddPolicy(name, context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = limit, Window = TimeSpan.FromMinutes(10), QueueLimit = 0, AutoReplenishment = true }));
    }
    options.OnRejected = async (context, cancellationToken) =>
    {
        var response = context.HttpContext.Response;
        response.Headers["Cache-Control"] = "no-store";
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry))
            response.Headers["Retry-After"] = Math.Max(1, (int)Math.Ceiling(retry.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        await MamiErrorResponse.WriteAsync(context.HttpContext, "Çok fazla istek gönderildi. Bir süre sonra tekrar deneyin.");
    };
});
var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        await MamiErrorResponse.WriteAsync(context, "Beklenmeyen bir hata oluştu. Bilgileri kontrol ederek tekrar deneyin.");
    }));
    app.UseHsts();
}
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    var path=context.Request.Path;
    if(path.StartsWithSegments("/yonetim") || path.StartsWithSegments("/uye") || path.StartsWithSegments("/garaj") || path.StartsWithSegments("/rota-planlayici") || path.StartsWithSegments("/topluluk/rotalar"))
    {
        context.Response.Headers["Cache-Control"]="no-store";
        context.Response.Headers["X-Robots-Tag"]="noindex, nofollow";
    }
    await next();
});
app.UseStatusCodePages(async status =>
{
    var context = status.HttpContext;
    var message = context.Response.StatusCode switch
    {
        400 => "İstek doğrulanamadı. Sayfayı yenileyip tekrar deneyin.",
        401 => "Devam etmek için giriş yapın.",
        403 => "Bu işlem için yetkiniz yok.",
        404 => "Aradığınız kayıt veya sayfa bulunamadı.",
        405 => "Bu işlem bu şekilde gerçekleştirilemiyor.",
        413 => "Dosya veya istek izin verilen boyutu aşıyor.",
        _ => "İşlem tamamlanamadı. Daha sonra tekrar deneyin."
    };
    await MamiErrorResponse.WriteAsync(context, message);
});
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
// Rider V3 stays disabled until additive tables and SMTP settings are ready.
app.Use(async (ctx, next) =>
{
    var path = ctx.Request.Path;
    if (!app.Configuration.GetValue<bool>("Rider:Enabled") &&
        (path.StartsWithSegments("/garaj") || path.StartsWithSegments("/uye") ||
         path.StartsWithSegments("/rota-planlayici") || path.StartsWithSegments("/topluluk/rotalar") ||
         path.StartsWithSegments("/yonetim/rider")))
    {
        ctx.Response.StatusCode = 503;
        await ctx.Response.WriteAsync("Rider Hub kurulumu henüz etkinleştirilmedi.");
        return;
    }
    await next();
});
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
await DbInitializer.InitializeAsync(app.Services, app.Configuration, app.Logger);
await RiderSeed.InitializeAsync(app.Services, app.Configuration, app.Logger);
app.Run();
