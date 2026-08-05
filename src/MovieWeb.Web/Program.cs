using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using MovieWeb.Core.Services;

var builder = WebApplication.CreateBuilder(args);

string dataDir = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDir);

// 0. Configure Kestrel limits to prevent HTTP 431 header size issues
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestHeadersTotalSize = 1048576; // 1MB header limit
});

// 1. Single Authentication Registration
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.Cookie.Name = "MovieWeb.AuthCookie.v2";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    })
    .AddGoogle(options =>
    {
        var clientId = builder.Configuration["Authentication:Google:ClientId"];
        var clientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

        options.ClientId = !string.IsNullOrWhiteSpace(clientId) ? clientId : "433719344606-alqnof011ono509c7du9odc6fd1f1ln3.apps.googleusercontent.com";
        options.ClientSecret = !string.IsNullOrWhiteSpace(clientSecret) ? clientSecret : "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";
        options.CallbackPath = "/signin-google";
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.Events.OnRedirectToAuthorizationEndpoint = context =>
        {
            context.Response.Redirect(context.RedirectUri + "&prompt=select_account");
            return Task.CompletedTask;
        };
        options.Events.OnRemoteFailure = context =>
        {
            context.Response.Redirect("/Account/Login?error=" + Uri.EscapeDataString("Google Client Secret chưa đúng. Vui lòng điền Client Secret từ Google Cloud Console vào appsettings.json."));
            context.HandleResponse();
            return Task.CompletedTask;
        };
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
        services.AddSingleton<IBookmarkService, BookmarkService>();
        services.AddSingleton<ICheckinService, CheckinService>();
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
builder.Services.AddSingleton<IBookmarkService, BookmarkService>();
builder.Services.AddSingleton<ICheckinService, CheckinService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();

app.UseOrchardCore();

app.Run();
