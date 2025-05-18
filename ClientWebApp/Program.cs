using ClientWebApp.Data;
using ClientWebApp.Repositories;
using ClientWebApp.Utlities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace ClientWebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File($"Logs/log-{timestamp}.txt")
                .CreateLogger();

            builder.Host.UseSerilog();


            // Add services to the container.

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<ApplicationDbContext>();

            builder.Services.AddScoped<EncryptionUtility>();
            builder.Services.AddScoped<FileRepository>();
            builder.Services.AddScoped<PermisionRepository>();
            builder.Services.AddScoped<LogsRepository>();

            builder.Services.AddAuthentication()
            .AddMicrosoftAccount(options =>
            {
                options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"]!;
                options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"]!;
            });

            builder.Services.AddControllersWithViews();

            builder.Services.Configure<IdentityOptions>(options =>
            {
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5); // Lockout duration
                options.Lockout.MaxFailedAccessAttempts = 3; // Number of failed attempts before lockout
                options.Lockout.AllowedForNewUsers = true; // Enable lockout for new users

                options.Password.RequireDigit = true;  // Require at least one number (0-9)
                options.Password.RequiredLength = 8;   // Minimum password length
                options.Password.RequireNonAlphanumeric = true; // Require special characters (e.g., !, @, #)
                options.Password.RequireUppercase = true; // Require at least one uppercase letter
                options.Password.RequireLowercase = true; // Require at least one lowercase letter
                options.Password.RequiredUniqueChars = 2; // Require at least 2 unique characters
            });

            var app = builder.Build();

            app.UseExceptionHandler("/Home/Error");
            app.UseStatusCodePagesWithReExecute("/Home/StatusCode", "?code={0}");


            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {

                app.UseMigrationsEndPoint();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            app.Run();
        }
    }
}
