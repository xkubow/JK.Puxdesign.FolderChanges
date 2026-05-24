using System.Collections.Generic;

namespace JK.Puxdesign.FolderChanges.Models;

public class FolderState
{
    public List<FileDetail> Files { get; set; } = new();
    public List<string> Folders { get; set; } = new();
}
