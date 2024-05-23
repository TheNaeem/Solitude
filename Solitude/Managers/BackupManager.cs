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
        RestRequest request = new RestRequest("https://api.fmodel.app/v1/backups/FortniteGame")
        {
            Timeout = 3000
        };
        
        var response = await client.ExecuteAsync<Backup[]>(request);

        if (!response.IsSuccessful || string.IsNullOrWhiteSpace(response.Content))
        {
            Log.Error("Response from the FModel Backup API failed");
            return string.Empty;
        }

        Debug.Assert(response.Data != null, "response.Data != null");
        var backupPath = Path.Combine(DirectoryManager.BackupsDir, response.Data[4].FileName);

        var backupData = await client.DownloadDataAsync(new RestRequest(response.Data[4].DownloadUrl));
        Log.Information($"Download {response.Data[4].FileName} at {backupPath}");
        
        if (backupData == null || backupData.Length <= 0)
        {
            Log.Error("Failed to download the backup");
        }

        await File.WriteAllBytesAsync(backupPath, backupData);

        return backupPath;
    }
}