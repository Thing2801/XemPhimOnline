using Microsoft.AspNetCore.Authentication.Cookies;
using MovieWeb.Core.Services;

var builder = WebApplication.CreateBuilder(args);

string dataDir = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDir);

// 1. Single Authentication Registration
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.Cookie.Name = "MovieWeb.AuthCookie";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

// 2. Register Services for Orchard Core Tenants
builder.Services.AddOrchardCms(tenantBuilder =>
{
    tenantBuilder.ConfigureServices(services =>
    {
        services.AddScoped<IMovieService, OrchardCoreMovieService>();
        services.AddSingleton<IUserService>(sp => new UserService(dataDir));
        services.AddSingleton<ICommentService>(sp => new CommentService(dataDir));
    });
    tenantBuilder.AddGlobalFeatures(
        "MovieWeb.Modules.Home",
        "MovieWeb.Modules.Movies",
        "MovieWeb.Modules.Genres",
        "MovieWeb.Modules.Cinema",
        "MovieWeb.Modules.Series"
    );
});

// 3. Register Core Services
builder.Services.AddScoped<IMovieService, OrchardCoreMovieService>();
builder.Services.AddSingleton<IUserService>(sp => new UserService(dataDir));
builder.Services.AddSingleton<ICommentService>(sp => new CommentService(dataDir));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseOrchardCore();

app.Run();
