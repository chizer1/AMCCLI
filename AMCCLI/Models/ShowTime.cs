namespace AMCCLI.Models;

public class ShowTime
{
    public required string Slug { get; init; }
    public required TimeOnly Time { get; init; }
    public required string TheatreName { get; init; }
}
