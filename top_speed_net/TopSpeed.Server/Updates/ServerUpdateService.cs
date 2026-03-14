using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TopSpeed.Server.Updates
{
    internal sealed class ServerUpdateService
    {
        private readonly ServerUpdateConfig _config;
        private readonly HttpClient _http;

        public ServerUpdateService(ServerUpdateConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _http = new HttpClient();
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("TopSpeedServerUpdater/1.0");
            _http.Timeout = TimeSpan.FromSeconds(25);
        }

        public async Task<ServerUpdateCheckResult> CheckAsync(ServerVersion current, string rid, CancellationToken cancellationToken)
        {
            try
            {
                var info = await ReadInfoAsync(cancellationToken).ConfigureAwait(false);
                if (info == null)
                    return Fail("The update info file could not be read.");

                if (!ServerVersion.TryParse(info.ServerVersion, out var remoteVersion))
                    return Fail("The update info file has an invalid server version format.");

                if (remoteVersion.CompareTo(current) <= 0)
                    return new ServerUpdateCheckResult { IsSuccess = true };

                var release = await ReadLatestReleaseAsync(cancellationToken).ConfigureAwait(false);
                var expectedAsset = _config.BuildExpectedAssetName(info.ServerVersion ?? string.Empty, rid);
                var asset = FindAsset(release, expectedAsset);
                if (asset == null || string.IsNullOrWhiteSpace(asset.DownloadUrl))
                    return Fail($"Update package '{expectedAsset}' was not found in the latest release.");

                return new ServerUpdateCheckResult
                {
                    IsSuccess = true,
                    Update = new ServerUpdateInfo
                    {
                        VersionText = info.ServerVersion ?? string.Empty,
                        Version = remoteVersion,
                        ServerChanges = info.ServerChanges != null
                            ? (IReadOnlyList<string>)info.ServerChanges
                            : Array.Empty<string>(),
                        DownloadUrl = asset.DownloadUrl ?? string.Empty,
                        AssetSizeBytes = asset.Size ?? 0
                    }
                };
            }
            catch (TaskCanceledException)
            {
                return Fail("Update check timed out.");
            }
            catch (Exception ex)
            {
                return Fail($"Update check failed: {ex.Message}");
            }
        }

        public async Task<ServerDownloadResult> DownloadAsync(
            ServerUpdateInfo update,
            string targetDirectory,
            Action<ServerDownloadProgress> onProgress,
            CancellationToken cancellationToken)
        {
            if (update == null)
                throw new ArgumentNullException(nameof(update));
            if (string.IsNullOrWhiteSpace(targetDirectory))
                throw new ArgumentException("Target directory is required.", nameof(targetDirectory));

            var zipPath = Path.Combine(
                targetDirectory,
                _config.BuildExpectedAssetName(update.VersionText, ServerUpdateConfig.CurrentRid));

            try
            {
                using var response = await _http.GetAsync(
                    update.DownloadUrl,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                    return new ServerDownloadResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Download failed with status code {(int)response.StatusCode}.",
                        ZipPath = zipPath
                    };

                var totalBytes = response.Content.Headers.ContentLength ?? update.AssetSizeBytes;
                var downloaded = 0L;
                var lastPercent = -1;
                var buffer = new byte[81920];

                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                using var file = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
                while (true)
                {
                    var read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                    if (read <= 0)
                        break;
                    await file.WriteAsync(buffer, 0, read, cancellationToken).ConfigureAwait(false);
                    downloaded += read;

                    var percent = totalBytes > 0
                        ? (int)Math.Floor((downloaded * 100d) / totalBytes)
                        : 0;
                    if (percent > 100)
                        percent = 100;

                    if (percent != lastPercent || downloaded == totalBytes)
                    {
                        lastPercent = percent;
                        onProgress?.Invoke(new ServerDownloadProgress
                        {
                            DownloadedBytes = downloaded,
                            TotalBytes = totalBytes,
                            Percent = percent
                        });
                    }
                }

                onProgress?.Invoke(new ServerDownloadProgress
                {
                    DownloadedBytes = downloaded,
                    TotalBytes = totalBytes,
                    Percent = 100
                });

                return new ServerDownloadResult
                {
                    IsSuccess = true,
                    ZipPath = zipPath,
                    TotalBytes = totalBytes
                };
            }
            catch (TaskCanceledException)
            {
                return new ServerDownloadResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Download timed out or was canceled.",
                    ZipPath = zipPath
                };
            }
            catch (Exception ex)
            {
                return new ServerDownloadResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Download failed: {ex.Message}",
                    ZipPath = zipPath
                };
            }
        }

        private async Task<ServerInfoDoc?> ReadInfoAsync(CancellationToken cancellationToken)
        {
            using var response = await _http.GetAsync(_config.InfoUrl, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return null;
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            return ReadJson<ServerInfoDoc>(stream);
        }

        private async Task<ServerReleaseDoc?> ReadLatestReleaseAsync(CancellationToken cancellationToken)
        {
            using var response = await _http.GetAsync(_config.LatestReleaseApiUrl, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return null;
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            return ReadJson<ServerReleaseDoc>(stream);
        }

        private static T? ReadJson<T>(Stream stream) where T : class
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            return serializer.ReadObject(stream) as T;
        }

        private static ServerReleaseAssetDoc? FindAsset(ServerReleaseDoc? release, string expectedName)
        {
            if (release?.Assets == null || release.Assets.Count == 0)
                return null;

            for (var i = 0; i < release.Assets.Count; i++)
            {
                var asset = release.Assets[i];
                if (asset == null || string.IsNullOrWhiteSpace(asset.Name))
                    continue;
                if (!string.Equals((asset.Name ?? string.Empty).Trim(), expectedName, StringComparison.OrdinalIgnoreCase))
                    continue;
                return asset;
            }

            return null;
        }

        private static ServerUpdateCheckResult Fail(string message)
        {
            return new ServerUpdateCheckResult
            {
                IsSuccess = false,
                ErrorMessage = message ?? "Unknown update error."
            };
        }
    }
}
