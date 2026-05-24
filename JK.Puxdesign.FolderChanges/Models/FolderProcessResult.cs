namespace JK.Puxdesign.FolderChanges.Models;

public class FolderProcessResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<FileDetail> NewFiles { get; set; } = new();
    public List<FileDetail> ChangedFiles { get; set; } = new();
    public List<FileDetail> DeletedFiles { get; set; } = new();
    public List<string> DeletedFolders { get; set; } = new();
}