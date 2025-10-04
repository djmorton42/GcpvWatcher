using System.IO;

namespace GcpvWatcher.App.Services;

public class FileOperationsService
{
    private const string LynxEvtFileName = "Lynx.evt";
    
    /// <summary>
    /// Checks if a Lynx.evt file exists in the specified directory
    /// </summary>
    /// <param name="directoryPath">The directory path to check</param>
    /// <returns>True if the file exists, false otherwise</returns>
    public bool LynxEvtFileExists(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            return false;
            
        var filePath = Path.Combine(directoryPath, LynxEvtFileName);
        return File.Exists(filePath);
    }
    
    /// <summary>
    /// Creates a new Lynx.evt file in the specified directory with the standard template
    /// </summary>
    /// <param name="directoryPath">The directory path where to create the file</param>
    /// <returns>The full path to the created file</returns>
    /// <exception cref="ArgumentException">Thrown when directoryPath is null or empty</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the directory does not exist</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when there's no permission to create the file</exception>
    public string CreateLynxEvtFile(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            throw new ArgumentException("Directory path cannot be null or empty.", nameof(directoryPath));
            
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Directory does not exist: {directoryPath}");
            
        var filePath = Path.Combine(directoryPath, LynxEvtFileName);
        
        // Create the template content
        var templateContent = GetLynxEvtTemplate();
        
        // Write the file
        File.WriteAllText(filePath, templateContent);
        
        return filePath;
    }
    
    /// <summary>
    /// Gets the standard Lynx.evt file template content
    /// </summary>
    /// <returns>The template content as a string</returns>
    private static string GetLynxEvtTemplate()
    {
        return string.Empty;
    }
}
