using AMCCLI.Features;
using AMCCLI.Views;

namespace AMCCLI;

public class App(
    WelcomeView welcomeView,
    GetTheatres getTheatres,
    GetMovies getMovies,
    Inputs inputs
)
{
    public async Task Run()
    {
        welcomeView.Display();

        var theatres = await getTheatres.GetAsync();
        var selectedTheatres = inputs.SelectTheatres(theatres);
        var selectedDate = inputs.SelectDate();

        await getMovies.GetAsync(selectedTheatres, selectedDate);
    }
}
