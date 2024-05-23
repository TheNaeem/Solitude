using J = Newtonsoft.Json.JsonPropertyAttribute;
using I = Newtonsoft.Json.JsonIgnoreAttribute;

namespace Solitude.Managers.Models;

public class Backup
{
   [J] public string FileName { get; set; }
   [J] public string DownloadUrl { get; set; }
   [I][J] public string GameName { get; private set; }
   [I][J] public string FileSize { get; private set; }
}