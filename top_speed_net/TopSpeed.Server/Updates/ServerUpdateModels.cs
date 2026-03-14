using System;
using System.Collections.Generic;

namespace TopSpeed.Server.Updates
{
    internal sealed class ServerUpdateInfo
    {
        public string VersionText { get; set; } = string.Empty;
        public ServerVersion Version { get; set; }
        public IReadOnlyList<string> ServerChanges { get; set; } = Array.Empty<string>();
        public string DownloadUrl { get; set; } = string.Empty;
        public long AssetSizeBytes { get; set; }
    }

    internal sealed class ServerUpdateCheckResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public ServerUpdateInfo? Update { get; set; }
    }

    internal sealed class ServerDownloadProgress
    {
        public long DownloadedBytes { get; set; }
        public long TotalBytes { get; set; }
        public int Percent { get; set; }
    }

    internal sealed class ServerDownloadResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string ZipPath { get; set; } = string.Empty;
        public long TotalBytes { get; set; }
    }
}
