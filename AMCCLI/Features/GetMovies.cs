using AMCCLI.Models;
using AMCCLI.Scrapers;
using AMCCLI.Views;
using Spectre.Console;

namespace AMCCLI.Features;

public class GetMovies(ScrapeMovies scraper, MoviesView moviesView)
{
    public async Task GetAsync(List<Theatre> theatres, DateTime date)
    {
        try
        {
            var movies = await FetchMoviesAsync(theatres, date);
            moviesView.Display(movies);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }

    private async Task<List<Movie>> FetchMoviesAsync(List<Theatre> theatres, DateTime date)
    {
        return await AnsiConsole
            .Status()
            .Spinner(Spinner.Known.Circle)
            .StartAsync("Getting movies...", async _ => await scraper.GetAsync(theatres, date));
    }
}
