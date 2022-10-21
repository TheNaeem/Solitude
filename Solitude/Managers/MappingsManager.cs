using System.Text.Json;
using RestSharp;

namespace Solitude.Managers;

public static class MappingsManager
{
    private static bool TryFindSavedMappings(out string mappingsPath)
    {
        DirectoryInfo mappingsDir = new(DirectoryManager.MappingsDir);

        var mostRecentMappings =
            (from usmap in mappingsDir.GetFiles("*.usmap")
             orderby usmap.LastWriteTime descending
             select usmap).FirstOrDefault();

        if (mostRecentMappings is not null)
        {
            mappingsPath = mostRecentMappings.FullName;
            return true;
        }

        mappingsPath = string.Empty;

        return false;
    }

    public static bool TryGetMappings(out string mappingsPath)
    {
        Log.Information("Attempting to retrieve mappings");

        mappingsPath = string.Empty;

        using var client = new RestClient();

        var request = new RestRequest("https://fortnitecentral.gmatrixgames.ga/api/v1/mappings", Method.Get)
        {
            Timeout = 3000
        };

        var response = client.Execute(request);

        if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
        {
            Log.Error("Request to FortniteCentral for mappings failed.");

            return TryFindSavedMappings(out mappingsPath);
        }

        using var doc = JsonDocument.Parse(response.Content);
        var root = doc.RootElement;

        if (root.GetArrayLength() <= 0) return false;

        foreach (var mappings in root.EnumerateArray())
        {
            if (!mappings.TryGetProperty("meta", out var meta) ||
                !meta.TryGetProperty("compressionMethod", out var compressionMethod) ||
                (compressionMethod.GetString() != "Oodle" && compressionMethod.GetString() != "None") ||
                !mappings.TryGetProperty("fileName", out var fileName) ||
                !mappings.TryGetProperty("url", out var url))
            {
                continue;
            }

            mappingsPath = Path.Join(DirectoryManager.MappingsDir, fileName.GetString());

            if (File.Exists(mappingsPath))
            {
                return true;
            }

            var mappingsData = client.DownloadData(new(url.GetString()));

            if (mappingsData is null || mappingsData.Length <= 0)
            {
                Log.Error("Mappings data downloaded from FortniteCentral is null.");

                return TryFindSavedMappings(out mappingsPath);
            }

            File.WriteAllBytes(mappingsPath, mappingsData);

            return true;
        }

        return false;
    }
}
