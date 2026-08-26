using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using NfcHomeManager.Data;
using NfcHomeManager.Services;
using System.Threading.RateLimiting;

namespace NfcHomeManager;

public class Program
{
    public static void Main(string[] args)
    {
        // Pomocny prikaz pro vygenerovani AdminAuth:PasswordHash do appsettings:
        //   dotnet run -- hash-password MojeHeslo
        if (args.Length == 2 && args[0] == "hash-password")
        {
            if (args[1].Length < 10)
            {
                Console.Error.WriteLine("Varování: heslo kratší než 10 znaků - zvaž delší heslo nebo frázi.");
            }

            Console.WriteLine(AdminCredentialHasher.Hash(args[1]));
            return;
        }

        var builder = WebApplication.CreateBuilder(args);

        // Nevystavovat verzi/typ serveru v hlavicce odpovedi.
        builder.WebHost.ConfigureKestrel(o => o.AddServerHeader = false);

        builder.Services.AddRazorPages(options =>
        {
            options.Conventions.AuthorizeFolder("/");
            options.Conventions.AllowAnonymousToPage("/Admin/Login");
            options.Conventions.AllowAnonymousToFolder("/P");
            options.Conventions.AllowAnonymousToPage("/Error");
        });

        builder.Services.AddAuthorization();
        builder.Services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Admin/Login";
                options.AccessDeniedPath = "/Admin/Login";
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
                options.SlidingExpiration = true;
                options.Cookie.Name = "nfc_admin";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                // V produkci jen pres HTTPS; v Developmentu (http://localhost)
                // by "Always" cookie znemoznilo prihlaseni bez certifikatu.
                options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
            });

        // Ochrana proti hadani hesla hrubou silou: pro cely web mirny strop,
        // pro prihlasovaci formular mnohem prisnejsi limit na IP adresu.
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 300,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));

            options.AddPolicy("login", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 8,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        });

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=nfc-home.db;Foreign Keys=True";

        builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            DbInitializer.Initialize(context);
        }

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers[HeaderNames.XContentTypeOptions] = "nosniff";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["X-Frame-Options"] = "SAMEORIGIN";
            headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=(), payment=(), usb=()";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";
            headers["Cross-Origin-Resource-Policy"] = "same-origin";
            headers["Content-Security-Policy"] =
                "default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'self'; " +
                "form-action 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; script-src 'self';";

            await next();
        });

        app.UseStaticFiles();
        app.UseRouting();

        app.UseRateLimiter();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapRazorPages();

        app.Run();
    }
}
