using AMCCLI.Features;
using AMCCLI.Scrapers;
using AMCCLI.Services;
using AMCCLI.Views;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace AMCCLI;

public static class Program
{
    public static async Task Main()
    {
        var services = new ServiceCollection();

        ConfigureServices(services);

        var provider = services.BuildServiceProvider();

        while (true)
        {
            var app = provider.GetRequiredService<App>();
            await app.Run();

            Console.WriteLine();
            AnsiConsole.Markup("Press Enter to rerun or Ctrl+C to exit");
            Console.ReadLine();
            AnsiConsole.Clear();
        }
    }

    private static void ConfigureServices(ServiceCollection services)
    {
        services.AddSingleton<HttpClient>();
        services.AddSingleton<HttpClientService>();

        services.AddSingleton<ScrapeTheatres>();
        services.AddSingleton<ScrapeMovies>();

        services.AddSingleton<Inputs>();

        services.AddSingleton<GetTheatres>();
        services.AddSingleton<GetMovies>();

        services.AddSingleton<WelcomeView>();
        services.AddSingleton<MoviesView>();

        services.AddSingleton<App>();
    }
}
