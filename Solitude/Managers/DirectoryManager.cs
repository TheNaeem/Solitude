namespace Solitude.Managers;

public static class DirectoryManager
{
    public static string FilesDir = Path.Combine(Environment.CurrentDirectory, "Files");
    public static string BackupsDir = Path.Combine(FilesDir, "Backups");
    public static string ChunksDir = Path.Combine(FilesDir, "Chunks");
    public static string MappingsDir = Path.Combine(FilesDir, "Mappings");
    public static string OutputDir = Path.Combine(FilesDir, "Output");
    public static string BundlesDir = Path.Combine(OutputDir, "Bundles");
    public static string OutfitsDir = Path.Combine(OutputDir, "Outfits");
    public static string ExportsDir = Path.Combine(OutputDir, "Exports");

    static DirectoryManager()
    {
        foreach (var dir in new string[] { FilesDir, ChunksDir, MappingsDir, OutputDir, BundlesDir, OutfitsDir, ExportsDir, BackupsDir })
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                Log.Information("Created directory {Dir}", dir);
            }
        }
    }
}
