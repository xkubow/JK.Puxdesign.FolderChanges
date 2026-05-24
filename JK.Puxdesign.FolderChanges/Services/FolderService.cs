using System.Security.Cryptography;
using JK.Puxdesign.FolderChanges.Configurations;
using JK.Puxdesign.FolderChanges.Models;
using JK.Puxdesign.FolderChanges.Repositories;
using Microsoft.Extensions.Options;

namespace JK.Puxdesign.FolderChanges.Services;

public class FolderService : IFolderService
{
    private readonly ILogger<FolderService> _logger;
    private readonly IFolderRepository _repository;
    private readonly FolderSettings _settings;

    public FolderService(ILogger<FolderService> logger, IFolderRepository repository, IOptions<FolderSettings> settings)
    {
        _logger = logger;
        _repository = repository;
        _settings = settings.Value;
    }

    public async Task<FolderProcessResult> ProcessFolderAsync(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            return new FolderProcessResult { Message = "Folder path cannot be empty." };

        try
        {
            if (!Directory.Exists(folderPath))
            {
                _logger.LogWarning("Folder process failed: '{FolderPath}' does not exist", folderPath);
                return new FolderProcessResult { Message = $"Folder '{folderPath}' does not exist." };
            }

            var previousState = await _repository.GetStateAsync(folderPath);
            var files = Directory.GetFiles(folderPath);

            if (files.Length > _settings.MaxFileCount)
            {
                _logger.LogWarning("Folder process failed: too many files ({Count}) in '{FolderPath}'", files.Length, folderPath);
                return new FolderProcessResult { Message = $"Folder contains too many files ({files.Length}). Maximum allowed is {_settings.MaxFileCount}." };
            }

            var (processingEntries, errorMessage) = await ProcessFilesInternalAsync(files, folderPath, previousState);
            if (errorMessage != null)
                return new FolderProcessResult { Message = errorMessage };

            var currentFolders = GetCurrentFolders(folderPath);
            var (result, currentFiles) = AnalyzeChanges(processingEntries, previousState, currentFolders);

            await SaveStateAsync(folderPath, currentFiles, currentFolders);

            result.Success = true;
            result.Message = FormatResultMessage(result);
            LogProcessingSuccess(folderPath, result);

            return result;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied to folder: '{FolderPath}'", folderPath);
            return new FolderProcessResult { Message = $"Access denied to folder: {folderPath}" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing folder: '{FolderPath}'", folderPath);
            return new FolderProcessResult { Message = $"Error processing folder: {ex.Message}" };
        }
    }

    private async Task<(IEnumerable<FileProcessingEntry> Entries, string? ErrorMessage)> ProcessFilesInternalAsync(
        string[] files, string folderPath, FolderState previousState)
    {
        var previousFilesDict = previousState.Files.ToDictionary(f => f.Name);
        var processingEntries = new List<FileProcessingEntry>();

        foreach (var filePath in files)
        {
            var entry = await ProcessFileInternalAsync(filePath, previousFilesDict);
            processingEntries.Add(entry);

            if (entry.Status == FileProcessingStatus.TooLarge)
            {
                _logger.LogWarning("Folder process failed: file '{FileName}' in '{FolderPath}' exceeds size limit", Path.GetFileName(filePath), folderPath);
                return (processingEntries, entry.ErrorMessage);
            }
        }

        return (processingEntries, null);
    }

    private (FolderProcessResult Result, List<FileDetail> CurrentFiles) AnalyzeChanges(
        IEnumerable<FileProcessingEntry> processingEntries,
        FolderState previousState,
        List<string> currentFolders)
    {
        var result = new FolderProcessResult();
        var currentFiles = new List<FileDetail>();

        foreach (var entry in processingEntries)
        {
            if (entry.Detail == null) continue;

            currentFiles.Add(entry.Detail);

            if (entry.Status == FileProcessingStatus.New)
            {
                result.NewFiles.Add(entry.Detail);
            }
            else if (entry.Status == FileProcessingStatus.Changed)
            {
                result.ChangedFiles.Add(entry.Detail);
            }
        }

        var currentFileNames = currentFiles.Select(f => f.Name).ToHashSet();
        foreach (var prev in previousState.Files.Where(f => !currentFileNames.Contains(f.Name)))
        {
            result.DeletedFiles.Add(new FileDetail
            {
                Name = prev.Name,
                Fingerprint = prev.Fingerprint,
                Version = prev.Version
            });
        }

        result.DeletedFolders = previousState.Folders
            .Where(f => !currentFolders.Contains(f))
            .ToList();

        return (result, currentFiles);
    }

    private List<string> GetCurrentFolders(string folderPath) => Directory.GetDirectories(folderPath)
            .Select(Path.GetFileName)
            .Where(n => !string.IsNullOrEmpty(n))
            .Cast<string>()
            .ToList();

    private async Task SaveStateAsync(string folderPath, List<FileDetail> currentFiles, List<string> currentFolders)
    {
        var newState = new FolderState
        {
            Files = currentFiles,
            Folders = currentFolders
        };
        await _repository.SaveStateAsync(folderPath, newState);
    }

    private string FormatResultMessage(FolderProcessResult result)
    {
        int totalChanges = result.NewFiles.Count + result.ChangedFiles.Count + result.DeletedFiles.Count;
        var message = $"Successfully processed. Found {totalChanges} changes.";

        if (result.DeletedFiles.Any() || result.DeletedFolders.Any())
        {
            message += $" Found {result.DeletedFiles.Count} deleted files and {result.DeletedFolders.Count} deleted folders.";
        }
        return message;
    }

    private void LogProcessingSuccess(string folderPath, FolderProcessResult result)
    {
        _logger.LogInformation("Successfully processed folder '{FolderPath}': {New} new, {Changed} changed, {Deleted} deleted files",
            folderPath, result.NewFiles.Count, result.ChangedFiles.Count, result.DeletedFiles.Count);
    }

    private async Task<string> CalculateFingerprintAsync(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hashBytes = await MD5.HashDataAsync(stream);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    private async Task<FileProcessingEntry> ProcessFileInternalAsync( string filePath, IReadOnlyDictionary<string, FileDetail> previousFiles)
    {
        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Name == _settings.FileStorageName)
            return new FileProcessingEntry(FileProcessingStatus.Ignored);

        if (fileInfo.Length > _settings.MaxFileSizeBytes)
        {
            return new FileProcessingEntry(FileProcessingStatus.TooLarge, ErrorMessage: $"File '{fileInfo.Name}' exceeds the configured size limit ({_settings.MaxFileSizeBytes / (1024 * 1024)}MB).");
        }

        var fingerprint = await CalculateFingerprintAsync(filePath);
        previousFiles.TryGetValue(fileInfo.Name, out var previousFile);

        var status = FileProcessingStatus.New;
        int version = 1;

        if (previousFile != null)
        {
            version = previousFile.Version;
            if (previousFile.Fingerprint != fingerprint)
            {
                version++;
                status = FileProcessingStatus.Changed;
            }
            else
            {
                status = FileProcessingStatus.Unchanged;
            }
        }

        var detail = new FileDetail
        {
            Name = fileInfo.Name,
            Fingerprint = fingerprint,
            Version = version
        };

        return new FileProcessingEntry(status, detail);
    }

    private enum FileProcessingStatus { Ignored, New, Changed, Unchanged, TooLarge }
    private record FileProcessingEntry(FileProcessingStatus Status, FileDetail? Detail = null, string? ErrorMessage = null);
}
