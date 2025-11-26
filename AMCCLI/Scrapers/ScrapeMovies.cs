using System.Globalization;
using System.Web;
using AMCCLI.Models;
using AMCCLI.Services;
using HtmlAgilityPack;

namespace AMCCLI.Scrapers;

public class ScrapeMovies(HttpClientService httpClientService)
{
    public async Task<List<Movie>> GetAsync(List<Theatre> theatres, DateTime date)
    {
        var tasks = theatres.Select(t => GetMoviesForTheatreAsync(t, date));
        var results = await Task.WhenAll(tasks);

        var movieMap = new Dictionary<string, Movie>(StringComparer.OrdinalIgnoreCase);

        foreach (var movieList in results)
        {
            foreach (var movie in movieList)
            {
                if (!movieMap.TryGetValue(movie.Title, out var existing))
                {
                    movieMap[movie.Title] = movie;
                }
                else
                {
                    foreach (var st in movie.ShowTimes)
                    {
                        if (
                            !existing.ShowTimes.Any(x =>
                                x.Time == st.Time
                                && x.Slug == st.Slug
                                && x.TheatreName == st.TheatreName
                            )
                        )
                        {
                            existing.ShowTimes.Add(st);
                        }
                    }
                }
            }
        }

        return movieMap.Values.ToList();
    }

    private async Task<List<Movie>> GetMoviesForTheatreAsync(Theatre theatre, DateTime date)
    {
        var movies = new List<Movie>();

        var url =
            $"https://www.amctheatres.com/movie-theatres/{theatre.Slug}/showtimes?date={date:yyyy-MM-dd}";
        var content = await httpClientService.GetAsync(url);

        var doc = new HtmlDocument();
        doc.LoadHtml(content);

        var sections = doc.DocumentNode.SelectNodes("//section");
        if (sections == null)
            return movies;

        var movieTasks = sections
            .Select(section => CreateMovieFromSectionAsync(section, theatre.Name))
            .Where(task => task != null)
            .ToList();

        var moviesArray = await Task.WhenAll(movieTasks);

        movies.AddRange(moviesArray.Where(m => m != null)!);

        return movies;
    }

    private async Task<Movie?> CreateMovieFromSectionAsync(HtmlNode section, string theatreName)
    {
        var title = section.SelectText(".//h1//a");
        if (string.IsNullOrWhiteSpace(title))
            return null;

        var infoItems = section.SelectNodes(".//ul//li") ?? new HtmlNodeCollection(null);

        var runTimeStr = infoItems.Count > 0 ? infoItems[0].InnerText.Trim() : "";
        var parental = infoItems.Count > 1 ? infoItems[1].InnerText.Trim() : "";

        var movie = new Movie
        {
            Title = HtmlEntity.DeEntitize(title),
            RunTime = ParseRuntimeToMinutes(runTimeStr),
            ParentalGuidanceRating = HtmlEntity.DeEntitize(parental),
            ShowTimes = [],
            ImdbTags = []
        };

        var imdb = await GetImdbInfo(movie.Title);
        movie.ImdbDescription = imdb.Description;
        movie.ImdbRating = imdb.Score;
        movie.ImdbUrl = imdb.Url;
        movie.ImdbTags = imdb.Tags;

        ParseShowTimes(section, theatreName, movie);

        return movie;
    }

    private void ParseShowTimes(HtmlNode section, string theatreName, Movie movie)
    {
        var showTimes = section.SelectNodes(".//ul[@aria-label='Showtime Group Results']//li//a");
        if (showTimes == null)
            return;

        foreach (var node in showTimes)
        {
            var timeText = node.InnerTextOnly();
            if (string.IsNullOrWhiteSpace(timeText))
                continue;

            if (
                !TimeOnly.TryParseExact(
                    timeText,
                    ["h:mmtt", "hh:mmtt"],
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsed
                )
            )
                continue;

            var href = node.GetAttributeValue("href", "");
            var slug = href.StartsWith("/showtimes/") ? href["/showtimes/".Length..] : href;

            if (string.IsNullOrWhiteSpace(slug))
                continue;

            movie.ShowTimes.Add(
                new ShowTime
                {
                    Time = parsed,
                    Slug = slug,
                    TheatreName = theatreName
                }
            );
        }
    }

    private class ImdbSearchResult
    {
        public string? Url { get; init; }
    }

    private class ImdbMovieDetails
    {
        public string? Url { get; init; }
        public string? Description { get; init; }
        public decimal? Score { get; init; }
        public List<string>? Tags { get; init; }
    }

    private async Task<ImdbMovieDetails> GetImdbInfo(string movieName)
    {
        var search = await GetFirstImdbUrlAsync(movieName);
        if (search?.Url == null)
            return new ImdbMovieDetails();

        return await GetMovieDetailsAsync(search.Url);
    }

    private async Task<ImdbSearchResult?> GetFirstImdbUrlAsync(string movieName)
    {
        var query = HttpUtility.UrlEncode(movieName);
        var url = $"https://www.imdb.com/find/?q={query}";

        var web = new HtmlWeb();
        var doc = await web.LoadFromWebAsync(url);

        var first = doc.DocumentNode.SelectSingleNode(
            "//section[@data-testid='find-results-section-title']//ul/li[1]//a[contains(@href, '/title/')]"
        );

        if (first == null)
            return null;

        var href = first.GetAttributeValue("href", "");
        if (string.IsNullOrWhiteSpace(href))
            return null;

        return new ImdbSearchResult { Url = $"https://www.imdb.com{href}" };
    }

    private async Task<ImdbMovieDetails> GetMovieDetailsAsync(string url)
    {
        var web = new HtmlWeb();

        HtmlDocument doc;
        try
        {
            doc = await web.LoadFromWebAsync(url);
        }
        catch
        {
            return new ImdbMovieDetails { Url = url };
        }

        var tagSection = doc.DocumentNode.SelectNodes("//div[@data-testid='interests']//a");
        var tags = tagSection.Select(tag => tag.InnerText).ToList();

        var description = HtmlEntity.DeEntitize(doc.SelectText("//span[@data-testid='plot-xl']"));

        var scoreText = doc.SelectText(
            "//div[@data-testid='hero-rating-bar__aggregate-rating__score']/span[1]"
        );

        decimal? score = null;
        if (decimal.TryParse(scoreText, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
            score = v;

        return new ImdbMovieDetails
        {
            Url = url,
            Description = description,
            Score = score,
            Tags = tags
        };
    }

    private int ParseRuntimeToMinutes(string runtime)
    {
        if (string.IsNullOrWhiteSpace(runtime))
            return 0;

        var total = 0;

        var parts = runtime.ToUpperInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        for (var i = 0; i < parts.Length - 1; i++)
        {
            if (!int.TryParse(parts[i], out var num))
                continue;

            var unit = parts[i + 1];
            if (unit.StartsWith("HR"))
            {
                total += num * 60;
                i++;
            }
            else if (unit.StartsWith("MIN"))
            {
                total += num;
                i++;
            }
        }

        return total;
    }
}

public static class HtmlNodeExtensions
{
    public static string SelectText(this HtmlNode node, string xpath) =>
        HtmlEntity.DeEntitize(node.SelectSingleNode(xpath)?.InnerText.Trim() ?? "");

    public static string SelectText(this HtmlDocument doc, string xpath) =>
        HtmlEntity.DeEntitize(doc.DocumentNode.SelectSingleNode(xpath)?.InnerText.Trim() ?? "");

    public static string InnerTextOnly(this HtmlNode node)
    {
        return string.Join(
                "",
                node.ChildNodes.Where(n => n.NodeType == HtmlNodeType.Text)
                    .Select(n =>
                        System.Text.RegularExpressions.Regex.Replace(n.InnerText, @"\s+", "")
                    )
            )
            .Trim();
    }
}
