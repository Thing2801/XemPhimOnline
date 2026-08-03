using MovieWeb.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// Register Orchard Core CMS Services
builder.Services.AddOrchardCms(tenantBuilder =>
{
    tenantBuilder.ConfigureServices(services =>
    {
        services.AddScoped<IMovieService, OrchardCoreMovieService>();
    });
    tenantBuilder.AddGlobalFeatures(
        "MovieWeb.Modules.Home",
        "MovieWeb.Modules.Movies",
        "MovieWeb.Modules.Genres",
        "MovieWeb.Modules.Cinema",
        "MovieWeb.Modules.Series"
    );
});

// Register Movie Core Services
builder.Services.AddScoped<IMovieService, OrchardCoreMovieService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Serve Static Files
app.UseStaticFiles();

// Register Orchard Core CMS Middleware & MVC Routes
app.UseOrchardCore();

app.Run();
