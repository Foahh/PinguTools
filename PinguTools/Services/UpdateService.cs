using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace PinguTools.Services;

public interface IUpdateService
{
    Task<(Version Version, string Url)> CheckForUpdatesAsync();
}

public class GitHubUpdateService : IUpdateService
{
    private const string RELEASES_URL = "https://api.github.com/repos/Foahh/PinguTools/releases/latest";
    private readonly HttpClient httpClient;

    public GitHubUpdateService()
    {
        httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(App.Name, App.VersionString));
    }

    public async Task<(Version Version, string Url)> CheckForUpdatesAsync()
    {
        var response = await httpClient.GetAsync(RELEASES_URL);
        if (!response.IsSuccessStatusCode) throw new Exception("Could not retrieve release information from GitHub.");

        var jsonContent = await response.Content.ReadAsStringAsync();

        using var document = JsonDocument.Parse(jsonContent);
        var root = document.RootElement;
        var tagName = root.GetProperty("tag_name").GetString();
        var htmlUrl = root.GetProperty("html_url").GetString();

        if (!string.IsNullOrEmpty(tagName) && tagName.StartsWith("v", StringComparison.OrdinalIgnoreCase)) tagName = tagName[1..];
        if (!Version.TryParse(tagName, out var version)) throw new Exception("The release version string is not in a valid format.");
        if (string.IsNullOrEmpty(htmlUrl)) throw new Exception("The release URL is not in a valid format.");

        return (version, htmlUrl);
    }
}