namespace AMCCLI.Services;

public class HttpClientService(HttpClient httpClient)
{
    public async Task<string> GetAsync(string path)
    {
        var uriBuilder = new UriBuilder(new Uri(path));
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; 123/1.0)");

        var response = await httpClient.GetAsync(uriBuilder.Uri);

        if (response.IsSuccessStatusCode)
            return await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        return string.Empty;
    }
}
