using JK.Puxdesign.FolderChanges.Models;

namespace JK.Puxdesign.FolderChanges.Repositories;

public interface IFolderRepository
{
    Task<FolderState> GetStateAsync(string folderPath);
    Task SaveStateAsync(string folderPath, FolderState state);
}
