using AMCCLI.Models;
using Spectre.Console;

namespace AMCCLI.Views;

public class Inputs
{
    public string SelectZipCode()
    {
        while (true)
        {
            var zipCode = AnsiConsole.Ask<string>("Enter your [green]zip code[/]:");

            if (!string.IsNullOrWhiteSpace(zipCode))
            {
                AnsiConsole.MarkupLine($"Zip code selected: [cyan]{zipCode}[/]");
                return zipCode;
            }

            AnsiConsole.MarkupLine("[red]Invalid zip code. Please try again.[/]");
        }
    }

    public List<Theatre> SelectTheatres(List<Theatre> theatres)
    {
        var formattedChoices = FormatTheatreChoices(theatres);

        var selected = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("[bold]Select theatre(s) near you[/]")
                .PageSize(10)
                .MoreChoicesText("[grey](Use ↑/↓ to navigate)[/]")
                .InstructionsText(
                    "[bold](Press [blue]<space>[/] to select, [green]<enter>[/] to confirm)[/]"
                )
                .HighlightStyle(new Style(foreground: Color.Blue, decoration: Decoration.Bold))
                .AddChoices(formattedChoices)
        );

        var selectedTheatres = theatres
            .Where(t => selected.Any(s => s.StartsWith(t.Name ?? string.Empty)))
            .ToList();

        AnsiConsole.MarkupLine(
            $"Theatre(s) selected: [cyan]{string.Join(", ", selectedTheatres.Select(t => t.Name))}[/]"
        );

        return selectedTheatres;
    }

    public DateTime SelectDate()
    {
        var date = AnsiConsole.Prompt(
            new TextPrompt<DateTime>("Enter a date (yyyy-MM-dd):")
                .DefaultValue(DateTime.Today)
                .Validate(d =>
                    d == DateTime.MinValue
                        ? ValidationResult.Error("[red]Invalid date[/]")
                        : ValidationResult.Success()
                )
        );

        AnsiConsole.MarkupLine($"Date selected: [cyan]{date:yyyy-MM-dd}[/]");

        return date;
    }

    private IEnumerable<string> FormatTheatreChoices(List<Theatre> theatres)
    {
        var maxName = theatres.Max(t => t.Name?.Length ?? 0);
        var maxAddress = theatres.Max(t => t.Address?.Length ?? 0);

        return theatres.Select(t =>
            $"{t.Name?.PadRight(maxName)} | {t.Address?.PadRight(maxAddress)} | {t.Distance} miles"
        );
    }
}
