using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.VirtualFileSystem;
using CUE4Parse_Conversion.Textures;
using EpicManifestParser.Api;
using GenericReader;
using K4os.Compression.LZ4.Streams;
using RestSharp;
using SkiaSharp;
using Solitude.Extensions;
using Solitude.Managers;
using Solitude.Objects.Graphics;
using Solitude.Objects.Profile;

namespace Solitude.Objects;

public class Dataminer
{
    public ESolitudeMode Mode { get; set; }
    private StreamedFileProvider _provider;
    private ChunkDownloader? _chunks;
    private string _backup;
    private List<VfsEntry>? _newFiles;

    public Dataminer(string mappingsPath, string backupPath)
    {
        _backup = backupPath;
        _provider = new(string.Empty, true, new VersionContainer(EGame.GAME_UE5_5));
        _provider.MappingsContainer = new FileUsmapTypeMappingsProvider(mappingsPath);
    }

    public async Task InstallDependenciesAsync(ManifestInfo manifestInfo)
    {
        _chunks = new ChunkDownloader();
        
        if (manifestInfo is null)
        {
            Log.Error("Manifest response content was empty.");
            return;
        }

        await _chunks.DownloadManifestAsync(manifestInfo);

        _chunks.LoadFileForProvider("FortniteGame/Content/Paks/global.utoc", ref _provider);
        _chunks.LoadFileForProvider("FortniteGame/Content/Paks/pakchunk10-WindowsClient.utoc", ref _provider); // hahahahahahahahahahahahahaha
    }

    public async Task LoadFilesAsync()
    {
        await _provider.MountAsync();
    }

    // would rather just support fmodel backups than make a seperate format
    public async Task LoadNewEntriesAsync() // https://github.com/4sval/FModel/blob/c014478abc4e455c7116504be92aa00eb00d757b/FModel/ViewModels/Commands/LoadCommand.cs#L144
    {
        var sw = Stopwatch.StartNew();

        await using FileStream fileStream = new FileStream(_backup, FileMode.Open);
        await using MemoryStream memoryStream = new MemoryStream();
        using var reader = new GenericStreamReader(fileStream);

        if (reader.Read<uint>() == 0x184D2204u)
        {
            fileStream.Position -= 4;
            await using LZ4DecoderStream compressionStream = LZ4Stream.Decode(fileStream);
            await compressionStream.CopyToAsync(memoryStream).ConfigureAwait(false);
        }
        else
            await fileStream.CopyToAsync(memoryStream).ConfigureAwait(false);

        memoryStream.Position = 0;
        await using FStreamArchive archive = new FStreamArchive(fileStream.Name, memoryStream);
        _newFiles = new List<VfsEntry>();

        var paths = new Dictionary<string, int>();
        while (archive.Position < archive.Length)
        {
            archive.Position += 29;
            paths[archive.ReadString().ToLower()[1..]] = 0;
            archive.Position += 4;
        }

        foreach (var (key, value) in _provider.Files)
        {
            if (value is not VfsEntry entry || paths.ContainsKey(key) || entry.Path.EndsWith(".uexp") ||
                entry.Path.EndsWith(".ubulk") || entry.Path.EndsWith(".uptnl")) continue;

            _newFiles.Add(entry);
        }

        sw.Stop();

        Log.Information("Found {Count} new files in {Milliseconds} ms", _newFiles.Count, sw.ElapsedMilliseconds);
    }

    public async Task DoYourThing()
    {
        Log.Information("Prepare for leaks");

        if (_newFiles is null)
        {
            Log.Error("New files are null");
            return;
        }

        var sw = Stopwatch.StartNew();

        var newTextures = _newFiles.Where(x => x.PathWithoutExtension.StartsWith("FortniteGame/Content/UI/Foundation/Textures"));
        var newBundles = newTextures.Where(x => x.NameWithoutExtension.StartsWith("T-AthenaBundle"));
        var newOutfits = newTextures.Where(x => x.NameWithoutExtension.StartsWith("T-AthenaSoldier"));

        // to multithread or not to? multithreading usually leads to nothing getting exported or the corruption of some exported images. come on cue4

        foreach (var bundlePath in newBundles)
            _provider.SaveTextureToDisk(bundlePath.PathWithoutExtension, DirectoryManager.BundlesDir);

        foreach (var outfitPath in newOutfits)
            _provider.SaveTextureToDisk(outfitPath.PathWithoutExtension, DirectoryManager.OutfitsDir);

        sw.Stop();

        Log.Information("Exported all textures in {Time} ms", sw.ElapsedMilliseconds);

        RunCosmetics();

        foreach (var texturePath in newTextures)
            _provider.SaveTextureToDisk(texturePath.PathWithoutExtension, DirectoryManager.ExportsDir);

        await FinishOff();
    }

    private UTexture2D GetIconForCosmetic(UObject cosmetic, IEnumerable<VfsEntry>? offerImages)
    {
        if (cosmetic.ExportType == "AthenaPickaxeItemDefinition" &&
            cosmetic.TryGetValue(out FPackageIndex pickaxePtr, "WeaponDefinition") &&
            pickaxePtr.TryLoad(out var wid) &&
            wid is not null &&
            wid.TryGetValue(out UTexture2D pickaxeIcon, "LargePreviewImage"))
        {
            return pickaxeIcon;
        }

        if (cosmetic.TryGetValue<FSoftObjectPath>(out var displayAssetPtr, "DisplayAssetPath") &&
            displayAssetPtr.TryLoad(out var displayAsset) &&
            displayAsset.TryGetValue<FStructFallback>(out var tileImage, "TileImage") &&
            tileImage.TryGetValue<UTexture2D>(out var resourceObject, "ResourceObject"))
        {
            return resourceObject;
        }
        else if (cosmetic.TryGetValue(out FPackageIndex heroDefPtr, "HeroDefinition") &&
            heroDefPtr.TryLoad(out var heroDef) &&
            heroDef is not null &&
            heroDef.TryGetValue(out UTexture2D heroDefIcon, "LargePreviewImage"))
        {
            return heroDefIcon;
        }
        else if (cosmetic.TryGetValue(out UTexture2D cosmeticIcon, "LargePreviewImage"))
        {
            return cosmeticIcon;
        }

        return _provider.LoadObject<UTexture2D>("FortniteGame/Content/Athena/Prototype/Textures/T_Placeholder_Item_Outfit");
    }

    private static bool TryGetIconFromFile(UObject cosmetic, [NotNullWhen(true)] out SKBitmap? outIcon)
    {
        outIcon = null;

        if (cosmetic.ExportType != "AthenaCharacterItemDefinition")
            return false;

        var fileName = cosmetic.Name.Replace('_', '-');
        var iconFilePath = Path.Combine(DirectoryManager.OutfitsDir, $"T-AthenaSoldiers-{fileName}.png");

        if (!File.Exists(iconFilePath))
        {
            return false;
        }

        outIcon = SKBitmap.Decode(iconFilePath);

        return true;
    }

    public void RunCosmetics()
    {
        if (_newFiles is null)
            return;

        Log.Information("Creating merged cosmetics image");

        var sw = Stopwatch.StartNew();

        var imageInfo = new SKImageInfo(512, 562);
        var cosmeticIconInfo = new SKImageInfo(512, 512);
        var newCosmetics = _newFiles.Where(x => x.PathWithoutExtension.ToLower().StartsWith("fortnitegame/content/athena/items/cosmetics"));
        var offerImages = _newFiles.Where(x => x.PathWithoutExtension.StartsWith("FortniteGame/Content/Catalog/NewDisplayAssets"));

        if (newCosmetics.Count() == 0)
        {
            Log.Warning("No new cosmetics");
            return;
        }

        var profile = new ProfileBuilder(newCosmetics.Count());
        using var mergedImage = new MergedImageCreator(imageInfo, newCosmetics.Count());

        foreach (var cosmeticFile in newCosmetics)
        {
            profile.OnCosmeticAdded(cosmeticFile.NameWithoutExtension);

            if (!_provider.TryLoadObject(cosmeticFile.PathWithoutExtension, out var cosmetic))
                continue;

            using var icon = new FortniteIconCreator(imageInfo);

            if (cosmetic.TryGetValue<UObject>(out var seriesPtr, "Series"))
            {
                icon.DrawRarityBackground(seriesPtr.Name, cosmeticIconInfo);
            }
            else if (cosmetic.TryGetValue<FName>(out var rarity, "Rarity"))
            {
                icon.DrawRarityBackground(rarity.Text, cosmeticIconInfo);
            }
            else icon.DrawRarityBackground("Unattainable");

            if (TryGetIconFromFile(cosmetic, out var cosmeticIcon))
            {
                icon.DrawAndResizeImage(cosmeticIcon, 0, 0, cosmeticIconInfo);
            }
            else icon.DrawTexture(GetIconForCosmetic(cosmetic, offerImages), 0, 0, cosmeticIconInfo);

            if (cosmetic.TryGetValue<FText>(out var displayName, "DisplayName"))
            {
                icon.DrawDisplayName(displayName.Text.ToUpper());
            }

            var img = icon.GetImage();

            if (img is not null)
                mergedImage.AddIcon(img);
        }

        using var mergedBmp = mergedImage.Build();
        using var encoded = mergedBmp.Encode(SKEncodedImageFormat.Webp, 80);

        sw.Stop();

        Log.Information("Created merged image with {Num} cosmetics in {Time} ms", newCosmetics.Count(), sw.ElapsedMilliseconds);

        using var fs = File.Create(Path.Join(DirectoryManager.OutputDir, "merged.webp"));
        encoded?.AsStream().CopyTo(fs);

        File.WriteAllText(Path.Join(DirectoryManager.OutputDir, "profile_athena.json"), profile.Build());
    }

    private async Task FinishOff()
    {
        _chunks.LoadAllPaksForProvider(ref _provider); // download everything else because we got the quick stuff out 
        await LoadNewEntriesAsync();

        if (_newFiles is null)
            return;

        var textures = _newFiles.Where(x => x.NameWithoutExtension.StartsWith("T-"));

        foreach (var t in textures)
        {
            if (File.Exists(Path.Join(DirectoryManager.ExportsDir, $"{t.NameWithoutExtension}.png")))
                continue;

            if (!_provider.TryLoadObject<UTexture2D>(t.PathWithoutExtension, out var texture))
                continue;

            texture.SaveToDisk(DirectoryManager.ExportsDir);
        }

        if (_provider.TryLoadObject("FortniteGame/Content/Athena/Apollo/Maps/UI/Apollo_Terrain_Minimap.Apollo_Terrain_Minimap", out UTexture2D map))
        {
            using var mapImage = map.Decode()?.Encode(SKEncodedImageFormat.Webp, 80);
            map?.SaveToDisk(DirectoryManager.OutputDir);

            Log.Information("Saved map image");
        }

        // the rest? do it yourself ;)
    }


}
