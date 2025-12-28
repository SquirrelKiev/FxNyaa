namespace FxNyaa;

public class FxNyaaConfig
{
    public string DefaultNyaaInstanceUrl { get; init; } = "https://nyaa.si";
    public Dictionary<string, string>? NyaaInstanceHostOverrideUrls { get; init; }
    public string IconUrl { get; init; } = "https://nyaa.si/static/img/avatar/default.png";

    public string GetNyaaInstanceUrl(string host)
    {
        if (NyaaInstanceHostOverrideUrls == null)
            return DefaultNyaaInstanceUrl;

        return NyaaInstanceHostOverrideUrls.TryGetValue(host, out var instanceUrl) ? instanceUrl : DefaultNyaaInstanceUrl;
    }
}