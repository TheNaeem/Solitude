using System.Diagnostics;
using System.Text.RegularExpressions;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Readers;
using EpicManifestParser.Objects;
using Solitude.Managers;

namespace Solitude.Objects;

public class ChunkDownloader 
{
    public string ChunkBaseUrl { get; init; }
    public Manifest? Manifest { get; private set; }

    // https://github.com/4sval/FModel/blob/c014478abc4e455c7116504be92aa00eb00d757b/FModel/ViewModels/CUE4ParseViewModel.cs#L53
    private static Regex PakFinder = new(@"^FortniteGame(/|\\)Content(/|\\)Paks(/|\\)(pakchunk(?:0|10.*|20.*|\w+)-WindowsClient|global)\.(pak|utoc)$",
       RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant); 

    public ChunkDownloader(string chunkBaseUrl)
    {
        ChunkBaseUrl = chunkBaseUrl;
    }

    public void LoadFileForProvider(FileManifest file, ref StreamedFileProvider provider)
    {
        if (Manifest is null)
        {
            Log.Error("{FileName} could not be found", file.Name);
            return;
        }

        var sw = Stopwatch.StartNew();

        if (file.Name.EndsWith(".utoc"))
        {
            var versions = provider.Versions;

            // https://github.com/4sval/FModel/blob/c014478abc4e455c7116504be92aa00eb00d757b/FModel/ViewModels/CUE4ParseViewModel.cs#L196
            provider.RegisterVfs(file.Name, new Stream[] { file.GetStream() },
                it => new FStreamArchive(it, Manifest.FileManifests.First(x => x.Name.Equals(it)).GetStream(), versions));
        }
        else
        {
            using var pakStream = file.GetStream();
            provider.RegisterVfs(file.Name, new[] { pakStream });
        }

        var ms = sw.ElapsedMilliseconds;

        Log.Information("Downloaded {FileName} in {Milliseconds} ms", file.Name, ms);
    }

    public void LoadFileForProvider(string fileName, ref StreamedFileProvider provider)
    {
        var file = Manifest?.FileManifests.Find(x => x.Name == fileName);

        if (file is null)
        {
            Log.Error("{FileName} could not be found", fileName);
            return;
        }

        LoadFileForProvider(file, ref provider);
    }

    public void LoadAllPaksForProvider(ref StreamedFileProvider provider)
    {
        if (Manifest is null)
            return;

        foreach (var file in Manifest.FileManifests)
        {
            if (!PakFinder.IsMatch(file.Name) || file.Name.Contains("optional"))
                continue;

            LoadFileForProvider(file, ref provider);
        }

        provider.Mount();
    }

    public async Task DownloadManifestAsync(ManifestInfo info)
    {
        Manifest = new(await info.DownloadManifestDataAsync(), new()
        {
            ChunkBaseUri = new(ChunkBaseUrl, UriKind.Absolute),
            ChunkCacheDirectory = new(DirectoryManager.ChunksDir)
        });
    }
}
