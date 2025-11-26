using AMCCLI.Models;
using AMCCLI.Services;
using HtmlAgilityPack;

namespace AMCCLI.Scrapers;

public class ScrapeTheatres(HttpClientService httpClientService)
{
    public async Task<List<Theatre>> GetAsync(string zipCode)
    {
        var url = $"https://www.amctheatres.com/movie-theatres?q={zipCode}";
        var content = await httpClientService.GetAsync(url);

        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(content);

        var parentNode = htmlDoc.DocumentNode.SelectSingleNode(
            "//div[@aria-label='Theatre Search Results' and contains(@class,'focus:outline-none')]"
        );
        var ulNode = parentNode.SelectSingleNode(".//ul");
        var liNodes = ulNode.SelectNodes("./li");

        var results = new List<Theatre>();

        foreach (var li in liNodes)
        {
            var name = GetText(li, ".//h2");
            var address = GetText(li, ".//address");
            var distance = GetText(
                li,
                ".//div[contains(@class,'flex') and contains(@class,'justify-between') and contains(@class,'text-200')]/span"
            );

            var slug = ParseSlug(li.SelectSingleNode(".//a[h2]")?.GetAttributeValue("href", null!));

            results.Add(
                new Theatre
                {
                    Name = name,
                    Address = address,
                    Distance = decimal.Parse(distance.Replace("mi", "").Trim()),
                    Slug = slug
                }
            );
        }

        return results;
    }

    private static string GetText(HtmlNode node, string xpath)
    {
        return node.SelectSingleNode(xpath)?.InnerText.Trim() ?? string.Empty;
    }

    private static string ParseSlug(string? href)
    {
        if (string.IsNullOrWhiteSpace(href))
            return string.Empty;

        var segments = href.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

        return segments.Length switch
        {
            >= 2 => $"{segments[^2]}/{segments[^1]}",
            1 => segments[0],
            _ => string.Empty
        };
    }
}
