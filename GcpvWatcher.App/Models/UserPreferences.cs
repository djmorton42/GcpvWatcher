using System.Text.Json;

namespace GcpvWatcher.App.Models;

public class UserPreferences
{
    public string WatchDirectory { get; set; } = string.Empty;
    public string FinishLynxDirectory { get; set; } = string.Empty;

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
            Console.WriteLine($"Error loading user preferences: {ex.Message}");
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
            Console.WriteLine($"Error saving user preferences: {ex.Message}");
        }
    }
}
