using System.Text.Json;
public class CountryService
{
    private readonly HttpClient _httpClient;

    public CountryService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<CountryModel>> FetchCountriesAsync()
    {
        var countryList = new List<CountryModel>();

        try
        {
            var json = await _httpClient.GetStringAsync("/data/countries.json");
            var countriesJson = JsonSerializer.Deserialize<List<JsonElement>>(json);

            foreach (var country in countriesJson)
            {
                if (country.TryGetProperty("name", out var nameDict) &&
                    nameDict.TryGetProperty("common", out var nameElement))
                {
                    string name = nameElement.GetString() ?? "Unknown";
                    string code = "N/A";
                    string flag = "";

                    if (country.TryGetProperty("idd", out var idd))
                    {
                        if (idd.TryGetProperty("root", out var rootElement) &&
                            idd.TryGetProperty("suffixes", out var suffixesElement) &&
                            suffixesElement.ValueKind == JsonValueKind.Array &&
                            suffixesElement.GetArrayLength() > 0)
                        {
                            var root = rootElement.GetString();
                            var suffix = suffixesElement[0].GetString();
                            if (!string.IsNullOrWhiteSpace(root) && !string.IsNullOrWhiteSpace(suffix))
                            {
                                code = root + suffix;
                            }
                        }
                    }

                    if (country.TryGetProperty("flags", out var flags) &&
                        flags.TryGetProperty("png", out var flagElement))
                    {
                        flag = flagElement.GetString() ?? "";
                    }

                    countryList.Add(new CountryModel
                    {
                        Name = name,
                        Code = code,
                        Flag = flag
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading countries: {ex.Message}");
        }

        return countryList;
    }
}