using System.Text.Json;
using GcpvWatcher.App.Services;

namespace GcpvWatcher.App.Models;

public class UserPreferences
{
    public string WatchDirectory { get; set; } = string.Empty;
    public string FinishLynxDirectory { get; set; } = string.Empty;
    public bool ConsolidateSingleRacerRaces { get; set; }

    public static UserPreferences Load()
    {
        try
        {
            var preferencesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "userpreferences.json");
            
            if (!File.Exists(preferencesPath))
            {
                return new UserPreferences();
            }

            var json = File.ReadAllText(preferencesPath);
            var preferences = JsonSerializer.Deserialize<UserPreferences>(json);
            
            return preferences ?? new UserPreferences();
        }
        catch (Exception ex)
        {
            // Log error but don't fail startup
            ApplicationLogger.LogException("Error loading user preferences", ex);
            return new UserPreferences();
        }
    }

    public void Save()
    {
        try
        {
            var preferencesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "userpreferences.json");
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(preferencesPath, json);
        }
        catch (Exception ex)
        {
            // Log error but don't fail the operation
            ApplicationLogger.LogException("Error saving user preferences", ex);
        }
    }
}
