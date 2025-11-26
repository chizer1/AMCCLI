using AMCCLI.Models;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace AMCCLI.Views;

public class MoviesView
{
    private const int MaxDescriptionWidth = 60;

    public void Display(List<Movie> movies)
    {
        if (movies.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No movies found.[/]");
            return;
        }

        var consoleWidth = Console.WindowWidth;
        var columns = Math.Clamp(consoleWidth / 40, 1, 3);

        var grid = new Grid();
        for (var i = 0; i < columns; i++)
            grid.AddColumn(new GridColumn().NoWrap());

        var rowCells = new List<IRenderable>();

        foreach (var movie in movies)
        {
            if (movie.ShowTimes.Count == 0)
                continue;

            var showtimeTable = BuildShowtimeTable(movie);

            var description = WrapText(
                movie.ImdbDescription ?? "No description available.",
                MaxDescriptionWidth
            );
            var imdbRating = movie.ImdbRating.HasValue
                ? movie.ImdbRating.Value.ToString("0.0")
                : "N/A";
            var imdbUrlMarkup = !string.IsNullOrWhiteSpace(movie.ImdbUrl)
                ? $"[link={movie.ImdbUrl}]More info on IMDb[/]"
                : "[grey]No IMDb link available[/]";
            var imdbTags =
                movie.ImdbTags != null && movie.ImdbTags.Count > 0
                    ? WrapText(string.Join(", ", movie.ImdbTags), MaxDescriptionWidth)
                    : "No tags";

            var panelContent = new Rows(
                new Markup($"[bold]{description}[/]"),
                new Markup($"[grey]{movie.RunTime} minutes | {movie.ParentalGuidanceRating}[/]"),
                new Markup($"[bold green]IMDb Rating: {imdbRating}[/]"),
                new Markup(imdbUrlMarkup),
                new Markup($"[bold yellow]{imdbTags}[/]"),
                new Markup($"[bold underline]{movie.ShowTimes.Count} Showtime(s)[/]:"),
                showtimeTable
            );

            var panel = new Panel(panelContent)
                .Border(BoxBorder.Rounded)
                .BorderStyle(new Style(Color.Blue))
                .Padding(1, 1)
                .Header($"[bold blue]🎬 {TruncateText(movie.Title, 30)}[/]")
                .Expand();

            rowCells.Add(panel);

            if (rowCells.Count == columns)
            {
                grid.AddRow(rowCells.ToArray());
                rowCells.Clear();
            }
        }

        if (rowCells.Count > 0)
        {
            while (rowCells.Count < columns)
                rowCells.Add(new Markup(""));

            grid.AddRow(rowCells.ToArray());
        }

        AnsiConsole.Write(grid);
    }

    private Table BuildShowtimeTable(Movie movie)
    {
        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Grey)
            .Expand()
            .AddColumn("[bold cyan]Time[/]")
            .AddColumn("[bold cyan]Theatre[/]")
            .AddColumn("[bold cyan]Tickets[/]");

        foreach (var showTime in movie.ShowTimes.OrderBy(st => st.Time))
        {
            var url = $"https://www.amctheatres.com/showtimes/{showTime.Slug}";
            var ticketLink = $"[link={url}]🎟 Buy Tickets[/]";

            table.AddRow(
                $"[white]{showTime.Time:hh\\:mm tt}[/]",
                TruncateText(showTime.TheatreName, 20),
                ticketLink
            );
        }

        return table;
    }

    private string WrapText(string text, int width)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        var words = text.Split(' ');
        var line = "";
        var lines = new List<string>();

        foreach (var word in words)
        {
            if ((line.Length + word.Length + 1) > width)
            {
                lines.Add(line.TrimEnd());
                line = "";
            }
            line += word + " ";
        }

        if (!string.IsNullOrWhiteSpace(line))
            lines.Add(line.TrimEnd());

        return string.Join("\n", lines);
    }

    private string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
            return "";

        if (text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength - 3) + "...";
    }
}
