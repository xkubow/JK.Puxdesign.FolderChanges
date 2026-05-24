using JK.Puxdesign.FolderChanges.Models;

namespace JK.Puxdesign.FolderChanges.Store;

public interface IFolderStateStore
{
    FolderState? GetState(string folderPath);
    void UpdateState(string folderPath, FolderState state);
}