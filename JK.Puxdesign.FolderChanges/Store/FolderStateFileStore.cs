using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using JK.Puxdesign.FolderChanges.Configurations;
using JK.Puxdesign.FolderChanges.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JK.Puxdesign.FolderChanges.Store;

public class FolderStateFileStore : IFolderStateFileStore
{
    private readonly ILogger<FolderStateFileStore> _logger;
    private readonly FolderSettings _settings;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public FolderStateFileStore(ILogger<FolderStateFileStore> logger, IOptions<FolderSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<FolderState?> LoadStateAsync(string folderPath)
    {
        var statePath = Path.Combine(folderPath, _settings.FileStorageName);
        if (!File.Exists(statePath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(statePath);
            return JsonSerializer.Deserialize<FolderState>(json, SerializerOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading state from {StatePath}", statePath);
            return null;
        }
    }

    public async Task SaveStateAsync(string folderPath, FolderState state)
    {
        var statePath = Path.Combine(folderPath, _settings.FileStorageName);
        try
        {
            var json = JsonSerializer.Serialize(state, SerializerOptions);
            await File.WriteAllTextAsync(statePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving state to {StatePath}", statePath);
        }
    }
}
