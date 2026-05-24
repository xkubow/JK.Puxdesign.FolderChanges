using JK.Puxdesign.FolderChanges.Configurations;
using JK.Puxdesign.FolderChanges.Models;
using JK.Puxdesign.FolderChanges.Store;
using Microsoft.Extensions.Options;

namespace JK.Puxdesign.FolderChanges.Repositories;

public class FolderRepository : IFolderRepository
{
    private readonly IFolderStateStore _memoryStore;
    private readonly IFolderStateFileStore _fileStore;
    private readonly FolderSettings _settings;

    public FolderRepository(IFolderStateStore memoryStore, IFolderStateFileStore fileStore, IOptions<FolderSettings> settings)
    {
        _memoryStore = memoryStore;
        _fileStore = fileStore;
        _settings = settings.Value;
    }

    public async Task<FolderState> GetStateAsync(string folderPath)
    {
        var memoryState = _memoryStore.GetState(folderPath);
        if (memoryState != null)
        {
            return memoryState;
        }

        FolderState? state = null;
        if (_settings.UseFileStorage)
        {
            state = await _fileStore.LoadStateAsync(folderPath);
        }

        state ??= new FolderState();
        _memoryStore.UpdateState(folderPath, state);
        return state;
    }

    public async Task SaveStateAsync(string folderPath, FolderState state)
    {
        _memoryStore.UpdateState(folderPath, state);

        if (_settings.UseFileStorage)
        {
            await _fileStore.SaveStateAsync(folderPath, state);
        }
    }
}
