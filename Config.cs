using System.Text.Json;
using System.Text.Json.Serialization;

namespace WindowCloak;

public class Config
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("allow_by_default")]
    public bool AllowByDefault { get; set; } = true;
    
    [JsonPropertyName("fully_hide_windows")]
    public bool FullyHideWindows { get; set; }

    [JsonPropertyName("windows")]
    public Dictionary<string, bool> Windows { get; set; } = new();

    private static string GetConfigFolderPath()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WindowCloak");
    }
    
    private static string GetConfigFilePath()
    {
        return Path.Combine(GetConfigFolderPath(), "config.json");
    }
    
    public static Config Load()
    {
        if(!Directory.Exists(GetConfigFolderPath()))
            Directory.CreateDirectory(GetConfigFolderPath());
        
        if(!File.Exists(GetConfigFilePath()))
            return new Config();
        
        return JsonSerializer.Deserialize<Config>(File.ReadAllText(GetConfigFilePath())) ?? new Config();
    }
    
    public void Save()
    {
        File.WriteAllText(GetConfigFilePath(), JsonSerializer.Serialize(this));
    }
}