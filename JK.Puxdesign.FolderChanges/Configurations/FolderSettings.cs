namespace JK.Puxdesign.FolderChanges.Configurations;

public class FolderSettings
{
    public bool UseFileStorage { get; set; } = true;
    public string FileStorageName { get; set; } = ".folder_state.json";
    public long MaxFileSizeBytes { get; set; } = 52428800; // 50MB default
    public int MaxFileCount { get; set; } = 100;
}
