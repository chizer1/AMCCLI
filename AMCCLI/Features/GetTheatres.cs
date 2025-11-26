using AMCCLI.Models;
using AMCCLI.Scrapers;
using AMCCLI.Views;
using Spectre.Console;

namespace AMCCLI.Features;

public class GetTheatres(ScrapeTheatres scraper, Inputs inputs)
{
    public async Task<List<Theatre>> GetAsync()
    {
        try
        {
            var zip = inputs.SelectZipCode();
            return await FetchTheatresAsync(zip!);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return [];
        }
    }

    private async Task<List<Theatre>> FetchTheatresAsync(string zipCode)
    {
        return await AnsiConsole
            .Status()
            .Spinner(Spinner.Known.Circle)
            .StartAsync("Getting theatres...", async _ => await scraper.GetAsync(zipCode));
    }
}
