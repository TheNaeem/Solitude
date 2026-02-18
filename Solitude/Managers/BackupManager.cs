using System.Diagnostics;
using System.Text.Json;
using RestSharp;
using Solitude.Managers.Models;

namespace Solitude.Managers;

public static class BackupManager
{
    public static async Task<string> DownloadBackup()
    {
        RestClient client = new RestClient();
        RestRequest request = new RestRequest("https://api.fortniteapi.com/v1/backups")
        {
            Timeout = TimeSpan.FromMilliseconds(3 * 1000)
        };
        
        var response = await client.ExecuteAsync<Backup[]>(request);

        if (!response.IsSuccessful || string.IsNullOrWhiteSpace(response.Content))
        {
            Log.Error("Response from the Dilly Backup API failed");
            return string.Empty;
        }

        Debug.Assert(response.Data != null, "response.Data != null");
        var backupPath = Path.Combine(DirectoryManager.BackupsDir, response.Data[0].FileName);

        var backupData = await client.DownloadDataAsync(new RestRequest(response.Data[0].Url));
        Log.Information($"Download {response.Data[0].FileName} at {backupPath}");
        
        if (backupData == null || backupData.Length <= 0)
        {
            Log.Error("Failed to download the backup");
        }

        await File.WriteAllBytesAsync(backupPath, backupData);

        return backupPath;
    }
}