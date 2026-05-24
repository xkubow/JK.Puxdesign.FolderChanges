using JK.Puxdesign.FolderChanges.Models;

namespace JK.Puxdesign.FolderChanges.Store;

public interface IFolderStateFileStore
{
    Task<FolderState?> LoadStateAsync(string folderPath);
    Task SaveStateAsync(string folderPath, FolderState state);
}
