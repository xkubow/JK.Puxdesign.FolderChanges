using System.Collections.Concurrent;
using JK.Puxdesign.FolderChanges.Models;

namespace JK.Puxdesign.FolderChanges.Store;

public class FolderStateStore : IFolderStateStore
{
    private readonly ConcurrentDictionary<string, FolderState> _states = new();

    public FolderState? GetState(string folderPath)
    {
        return _states.TryGetValue(folderPath, out var state) ? state : null;
    }

    public void UpdateState(string folderPath, FolderState state)
    {
        _states[folderPath] = state;
    }
}
