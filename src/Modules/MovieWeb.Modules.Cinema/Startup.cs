using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MovieWeb.Core.Services;
using OrchardCore.Modules;

namespace MovieWeb.Modules.Cinema;

public class Startup : StartupBase
{
    public override int Order => -1000;

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IMovieService, OrchardCoreMovieService>();
    }

    public override void Configure(IApplicationBuilder app, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
    {
        routes.MapAreaControllerRoute(
            name: "CinemaIndex",
            areaName: "MovieWeb.Modules.Cinema",
            pattern: "phim-chieu-rap",
            defaults: new { controller = "Cinema", action = "Index" }
        );
    }
}
