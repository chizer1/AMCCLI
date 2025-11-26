namespace AMCCLI.Models;

public class Movie
{
    public required string Title { get; init; }
    public string? ImdbDescription { get; set; }
    public decimal? ImdbRating { get; set; }
    public string? ImdbUrl { get; set; }
    public List<string>? ImdbTags { get; set; }
    public required decimal RunTime { get; init; }
    public required string ParentalGuidanceRating { get; init; }
    public required List<ShowTime> ShowTimes { get; init; }
}
