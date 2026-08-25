using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using NfcHomeManager.Data;
using NfcHomeManager.Services;

namespace NfcHomeManager;

public class Program
{
    public static void Main(string[] args)
    {
        // Pomocny prikaz pro vygenerovani AdminAuth:PasswordHash do appsettings:
        //   dotnet run -- hash-password MojeHeslo
        if (args.Length == 2 && args[0] == "hash-password")
        {
            Console.WriteLine(AdminCredentialHasher.Hash(args[1]));
            return;
        }

        var builder = WebApplication.CreateBuilder(args);

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
            headers["Content-Security-Policy"] =
                "default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'self'; " +
                "img-src 'self' data:; style-src 'self' 'unsafe-inline'; script-src 'self';";

            await next();
        });

        app.UseStaticFiles();
        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapPost("/admin/logout", async (HttpContext ctx) =>
        {
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/Admin/Login");
        });

        app.MapRazorPages();

        app.Run();
    }
}
