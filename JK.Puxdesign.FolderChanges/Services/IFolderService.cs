using JK.Puxdesign.FolderChanges.Models;

namespace JK.Puxdesign.FolderChanges.Services;

public interface IFolderService
{
    Task<FolderProcessResult> ProcessFolderAsync(string folderPath);
}
