﻿using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using CUE4Parse.Compression;
using EpicManifestParser.Api;
using RestSharp;
using Solitude.Objects;
using Solitude.Objects.Endpoints;
using Spectre.Console;

namespace Solitude.Managers;

public static class Core
{
    public static Dataminer? Init()
    {
        Console.Title = "Solitude";

        Log.Logger =
            new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console()
            .CreateLogger();

        var backups = Directory.GetFiles(DirectoryManager.BackupsDir);

        if (backups.Length == 0)
        {
            Log.Error("No backups in backups folder.");
            Console.ReadKey();
            return null;
        }

        var backupFile = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Choose a [45]backup file[/] to load for getting new files.")
                .PageSize(10)
                .HighlightStyle("45")
                .MoreChoicesText("[grey](Move up and down to see more options)[/]")
                .AddChoices(backups));

        Log.Information("Selected backup file {BackupPath}", backupFile);

        OodleInit();
        ZLibInit();

        if (!MappingsManager.TryGetMappings(out var mappings))
        {
            Log.Error("Couldn't retrieve mappings");
            Console.ReadKey();
            return null;
        }

        Log.Information("Pulling mappings from {MappingsPath}", mappings);

        return new Dataminer(mappings, backupFile);
    }

    private static bool TryGetVersion(string manifestResponse, [NotNullWhen(true)] out string? version)
    {
        version = null;

        using var doc = JsonDocument.Parse(manifestResponse);

        if (!doc.RootElement.TryGetProperty("elements", out var elements) ||
            !elements[0].TryGetProperty("buildVersion", out var buildVersion))
        {
            return false;
        }

        var versionStr = buildVersion.GetString();

        if (string.IsNullOrEmpty(versionStr))
            return false;

        version = versionStr;
        return true;
    }

    private static async Task<RestResponse?> WatchForUpdateAsync(EpicManifestEndpoint endpoint)
    {
        var initialRes = await endpoint.GetResponseAsync();

        if (!initialRes.IsSuccessful ||
            string.IsNullOrEmpty(initialRes.Content) ||
            !TryGetVersion(initialRes.Content, out var oldVersion))
            return null;

        RestResponse ret;

        while (true)
        {
            await Task.Delay(5000);

            Log.Verbose("Watching for change in manifest from {Version}", oldVersion);
            var response = await endpoint.GetResponseAsync();

            if (!response.IsSuccessful)
            {
                Log.Error("Manifest request unsuccessful with status code {Status} while checking for change", response.StatusCode);
                continue;
            }

            if (string.IsNullOrEmpty(response.Content) ||
                !TryGetVersion(response.Content, out var version) ||
                version == oldVersion)
            {
                continue;
            }

            ret = response;
            break;
        }

        for (int i = 0; i < 5; i++)
        {
            Log.Information("NEW VERSION DETECTED");
        }

        return ret;
    }

    public static void OodleInit()
    {
        var oodlePath = Path.Combine(DirectoryManager.FilesDir, OodleHelper.OODLE_DLL_NAME);
        if (!File.Exists(oodlePath))
        {
            OodleHelper.DownloadOodleDll(oodlePath);
        }

        OodleHelper.Initialize(oodlePath);
    }

    public static void ZLibInit()
    {
        var dllPath = Path.Combine(DirectoryManager.FilesDir, ZlibHelper.DLL_NAME);
        if (!File.Exists(dllPath))
        {
            ZlibHelper.DownloadDll(dllPath);
        }
        
        ZlibHelper.Initialize(dllPath);
    }

    public static async Task<ManifestInfo> GetManifestAsync()
    {
        AuthManager.TryCreateToken(out string? token);

        var client = new RestClient();
        var request = new RestRequest("https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/public/assets/v2/platform/Windows/namespace/fn/catalogItem/4fe75bbc5a674f4f9b356b5c90567da5/app/Fortnite/label/Live");
        
        request.AddHeader("Authorization", $"bearer {token}");
        var response = await client.ExecuteAsync(request).ConfigureAwait(false);
        Log.Information("[{Method}] [{Status}({StatusCode})] '{Resource}'", request.Method, response.StatusDescription, (int) response.StatusCode, response.ResponseUri?.OriginalString);

        return response.IsSuccessful ? ManifestInfo.Deserialize(response.RawBytes) : null;
    }

    public static ManifestInfo GetManifest()
    {
        return GetManifestAsync().GetAwaiter().GetResult();
    } 

    public static async Task RunAsync(ESolitudeMode mode, Dataminer dataminer)
    {
        RestResponse? manifestResponse;
        EpicManifestEndpoint endpoint = new("launcher/api/public/assets/v2/platform/Windows/namespace/fn/catalogItem/4fe75bbc5a674f4f9b356b5c90567da5/app/Fortnite/label/Live");

        if (mode == ESolitudeMode.UpdateMode)
        {
            manifestResponse = await WatchForUpdateAsync(endpoint);

            if (manifestResponse is null)
            {
                Log.Error("Couldn't watch for new manifest");
                return;
            }
        }
        else manifestResponse = endpoint.GetResponse();
        var manifestInfo = ManifestInfo.Deserialize(manifestResponse.RawBytes);

        dataminer.Mode = mode;

        await dataminer.InstallDependenciesAsync(manifestInfo);
        await dataminer.LoadFilesAsync();
        await dataminer.LoadNewEntriesAsync();

        await dataminer.DoYourThing();
    }
}
