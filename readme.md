# JK.Puxdesign.FolderChanges

## Project Description
This project is a Blazor-based application designed to monitor and report changes in specified local folders. It tracks new, modified, and deleted files and folders, maintaining a version history of file changes by comparing file fingerprints.

## Technical Choices

### Blazor vs. MVC/ASP.NET Core
*   **Unified Language (C#)**: Using Blazor allows for a single development language (C#) across both the backend logic and the frontend UI. This reduces context switching and eliminates the need for extensive JavaScript for interactivity.
*   **Simplified State Management**: State is maintained on the server within the user's circuit, making it easier to handle complex UI flows without complex client-side state management libraries.

### Transient vs. Scoped Registrations
*   **Transient**: Most services in this project (`IFolderService`, `IFolderRepository`, `IFolderStateFileStore`) are registered as **Transient**.
    *   **Reasoning**: In Blazor Server, `Scoped` services live for the entire duration of a user's circuit (connection). For stateless or request-based logic, `Transient` ensures that a fresh instance is created every time it's needed, preventing unintended side effects or stale data from being carried over within a long-lived circuit.
*   **Singleton**: `IFolderStateStore` is registered as a **Singleton** because it acts as a global in-memory cache for folder states across all users and requests.

### File Fingerprinting (MD5 vs. SHA256)
*   The application currently uses **MD5** to generate file fingerprints (hashes).
*   **MD5** was chosen because it is significantly faster than SHA256, which is crucial when processing many files. In the context of detecting file changes (not for security/cryptography), the minimal risk of collisions is an acceptable trade-off for better performance. **SHA256** could be used if higher collision resistance were required, but at a performance cost.

### Folder Access
*   The Backend (BE) server must have appropriate OS-level permissions (Read/Write) to the folders it monitors.
*   The application uses standard `System.IO` APIs to scan the file system, so the service account running the application must be granted access to any target directory.

### Configuration
*   **Configurable Limits**: Operational limits, such as maximum file size and maximum file count per folder, are managed via `appsettings.json`. This allows for easy adjustments without code changes.

## Suggestions for Improvement

*   **Parallel Processing**: Implement parallelism (e.g., using `Parallel.ForEachAsync` or `Task.WhenAll`) when calculating hashes for multiple files in a folder to significantly reduce processing time.
*   **Decoupled Architecture**: Separate the Frontend (FE) from the Backend (BE) service. Moving to a Web API (BE) and a standalone SPA (FE) would allow for better scalability, independent deployments, and the ability to serve multiple clients.
*   **Real-time Monitoring**: Integrate `FileSystemWatcher` to react to folder changes in real-time instead of relying on manual triggers or polling.
*   **Background Processing**: For large folders, offload the processing to a background worker or queue (using technologies like **Microsoft Orleans** or **Hangfire**) to avoid blocking the UI thread during long-running operations.
*   **Monitoring & Observability**: Integrate **OpenTelemetry** to collect and export metrics (e.g., folder processing duration, change counts, and error rates). This would provide deep visibility into the application's performance and facilitate proactive monitoring using industry-standard tools like Prometheus or Application Insights.
