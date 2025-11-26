using Spectre.Console;

namespace AMCCLI.Views;

public class WelcomeView
{
    public void Display()
    {
        var figlet = new FigletText("AMC CLI").Color(Color.Red);

        AnsiConsole.Write(figlet);
        AnsiConsole.Write(
            new Markup("Find a movie to watch by searching across multiple AMC theatres near you!")
        );
        Console.WriteLine();
    }
}
