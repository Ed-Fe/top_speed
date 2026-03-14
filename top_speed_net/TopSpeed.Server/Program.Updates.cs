using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using TopSpeed.Server.Logging;
using TopSpeed.Server.Updates;

namespace TopSpeed.Server
{
    internal static partial class Program
    {
        private static int? TryHandleUpdateCommand(string[] args)
        {
            if (HasArgument(args, "--apply-update"))
                return RunApplyUpdate(args);
            if (HasArgument(args, "--check-update"))
                return RunCheckUpdate(args);
            if (HasArgument(args, "--update"))
                return RunUpdate(args);
            return null;
        }

        private const int DefaultAutoUpdateIntervalMinutes = 60;
        private const int MinAutoUpdateIntervalMinutes = 5;

        private static void StartBackgroundUpdateCheck(string[] args, CancellationTokenSource stopSource)
        {
            var config = BuildUpdateConfig(args);
            var service = new ServerUpdateService(config);
            var current = ServerUpdateConfig.CurrentVersion;
            var rid = ServerUpdateConfig.CurrentRid;
            var autoUpdate = HasArgument(args, "--auto-update");
            var intervalMinutes = GetAutoUpdateIntervalMinutes(args);

            Task.Run(async () =>
            {
                var shouldStop = await CheckAndMaybeApplyUpdateAsync(
                    service, current, rid, autoUpdate, stopSource.Token).ConfigureAwait(false);
                if (shouldStop || !autoUpdate)
                {
                    if (shouldStop)
                        stopSource.Cancel();
                    return;
                }

                var period = TimeSpan.FromMinutes(intervalMinutes);
                ConsoleSink.WriteLine($"[Update] Auto-update checks enabled every {intervalMinutes} minute(s).");

                using var timer = new PeriodicTimer(period);
                while (await timer.WaitForNextTickAsync(stopSource.Token).ConfigureAwait(false))
                {
                    shouldStop = await CheckAndMaybeApplyUpdateAsync(
                        service, current, rid, autoUpdate, stopSource.Token).ConfigureAwait(false);
                    if (shouldStop)
                    {
                        stopSource.Cancel();
                        return;
                    }
                }
            }, stopSource.Token);
        }

        private static int GetAutoUpdateIntervalMinutes(string[] args)
        {
            if (!TryGetIntArg(args, "--update-interval-minutes", out var minutes))
                return DefaultAutoUpdateIntervalMinutes;

            if (minutes < MinAutoUpdateIntervalMinutes)
            {
                ConsoleSink.WriteLine(
                    $"[Update] --update-interval-minutes too low ({minutes}). " +
                    $"Using {MinAutoUpdateIntervalMinutes}.");
                return MinAutoUpdateIntervalMinutes;
            }

            return minutes;
        }

        private static async Task<bool> CheckAndMaybeApplyUpdateAsync(
            ServerUpdateService service,
            ServerVersion current,
            string rid,
            bool autoUpdate,
            CancellationToken cancellationToken)
        {
            ServerUpdateCheckResult result;
            try
            {
                result = await service.CheckAsync(current, rid, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ConsoleSink.WriteLine($"[Update] Update check failed: {ex.Message}");
                return false;
            }

            if (!result.IsSuccess || result.Update == null)
                return false;

            if (!autoUpdate)
            {
                ConsoleSink.WriteLine(
                    $"[Update] A new server version {result.Update.Version} is available. " +
                    "Run with --update to upgrade.");
                return false;
            }

            ConsoleSink.WriteLine($"[Update] New version {result.Update.Version} found. Downloading...");

            ServerDownloadResult downloadResult;
            var lastProgressBucket = -1;
            try
            {
                downloadResult = await service.DownloadAsync(
                    result.Update,
                    AppContext.BaseDirectory,
                    progress =>
                    {
                        var bucket = progress.Percent / 25;
                        if (bucket <= lastProgressBucket)
                            return;
                        lastProgressBucket = bucket;
                        ConsoleSink.WriteLine($"[Update] Downloaded {progress.Percent}%");
                    },
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ConsoleSink.WriteLine($"[Update] Auto-update download failed: {ex.Message}");
                return false;
            }

            if (!downloadResult.IsSuccess)
            {
                ConsoleSink.WriteLine($"[Update] Auto-update download failed: {downloadResult.ErrorMessage}");
                return false;
            }

            try
            {
                LaunchApplyUpdateProcess(downloadResult.ZipPath, Process.GetCurrentProcess().Id);
            }
            catch (Exception ex)
            {
                ConsoleSink.WriteLine($"[Update] Failed to launch updater: {ex.Message}");
                return false;
            }

            ConsoleSink.WriteLine("[Update] Update downloaded. Stopping server to apply update.");
            return true;
        }

        private static int RunCheckUpdate(string[] args)
        {
            var config = BuildUpdateConfig(args);
            var service = new ServerUpdateService(config);
            var current = ServerUpdateConfig.CurrentVersion;
            var rid = ServerUpdateConfig.CurrentRid;

            ConsoleSink.WriteLine($"Checking for updates (current: {current}, RID: {rid})...");

            ServerUpdateCheckResult result;
            try
            {
                result = service.CheckAsync(current, rid, CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                ConsoleSink.WriteLine($"Update check failed: {ex.Message}");
                return 1;
            }

            if (!result.IsSuccess)
            {
                ConsoleSink.WriteLine($"Update check failed: {result.ErrorMessage}");
                return 1;
            }

            if (result.Update == null)
            {
                ConsoleSink.WriteLine("The server is up to date.");
                return 0;
            }

            var update = result.Update;
            ConsoleSink.WriteLine($"New version available: {update.Version}");
            if (update.ServerChanges != null && update.ServerChanges.Count > 0)
            {
                ConsoleSink.WriteLine("What's new:");
                for (var i = 0; i < update.ServerChanges.Count; i++)
                {
                    var change = update.ServerChanges[i];
                    if (!string.IsNullOrWhiteSpace(change))
                        ConsoleSink.WriteLine($"  * {change.Trim()}");
                }
            }

            // Exit code 10 signals "update available" to scripts/service managers.
            return 10;
        }

        private static int RunUpdate(string[] args)
        {
            var config = BuildUpdateConfig(args);
            var service = new ServerUpdateService(config);
            var current = ServerUpdateConfig.CurrentVersion;
            var rid = ServerUpdateConfig.CurrentRid;

            ConsoleSink.WriteLine($"Checking for updates (current: {current}, RID: {rid})...");

            ServerUpdateCheckResult checkResult;
            try
            {
                checkResult = service.CheckAsync(current, rid, CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                ConsoleSink.WriteLine($"Update check failed: {ex.Message}");
                return 1;
            }

            if (!checkResult.IsSuccess)
            {
                ConsoleSink.WriteLine($"Update check failed: {checkResult.ErrorMessage}");
                return 1;
            }

            if (checkResult.Update == null)
            {
                ConsoleSink.WriteLine("The server is already up to date.");
                return 0;
            }

            var update = checkResult.Update;
            ConsoleSink.WriteLine($"Downloading update {update.Version}...");

            var targetDir = AppContext.BaseDirectory;
            var lastPercent = -1;

            ServerDownloadResult downloadResult;
            try
            {
                downloadResult = service.DownloadAsync(
                    update,
                    targetDir,
                    progress =>
                    {
                        if (progress.Percent != lastPercent && progress.Percent % 10 == 0)
                        {
                            lastPercent = progress.Percent;
                            ConsoleSink.WriteLine($"  Downloaded {progress.Percent}%");
                        }
                    },
                    CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                ConsoleSink.WriteLine($"Download failed: {ex.Message}");
                return 1;
            }

            if (!downloadResult.IsSuccess)
            {
                ConsoleSink.WriteLine($"Download failed: {downloadResult.ErrorMessage}");
                return 1;
            }

            ConsoleSink.WriteLine("Download complete. Launching updater...");
            var currentPid = Process.GetCurrentProcess().Id;
            try
            {
                LaunchApplyUpdateProcess(downloadResult.ZipPath, currentPid);
            }
            catch (Exception ex)
            {
                ConsoleSink.WriteLine($"Failed to launch updater: {ex.Message}");
                return 1;
            }

            // Give the child process a moment to register before we exit.
            Thread.Sleep(200);
            ConsoleSink.WriteLine("The server will stop now. Restart it after the update completes.");
            return 0;
        }

        private static int RunApplyUpdate(string[] args)
        {
            // Usage (internal): --apply-update <zip_path> <pid>
            var zipPath = GetArgumentValue(args, "--apply-update");
            if (string.IsNullOrWhiteSpace(zipPath))
            {
                ConsoleSink.WriteLine("--apply-update requires a zip path argument.");
                return 1;
            }

            var pidStr = GetNextPositionalArgAfter(args, "--apply-update");
            if (!int.TryParse(pidStr, out var pid) || pid <= 0)
            {
                ConsoleSink.WriteLine("--apply-update requires a valid process ID after the zip path.");
                return 1;
            }

            ConsoleSink.WriteLine($"Waiting for server process {pid} to exit...");
            WaitForProcessExit(pid);

            var targetDir = AppContext.BaseDirectory;
            ConsoleSink.WriteLine($"Applying update from: {zipPath}");
            try
            {
                ExtractZip(zipPath, targetDir);
                File.Delete(zipPath);
                ConsoleSink.WriteLine("Update applied successfully. Restart the server to run the new version.");
            }
            catch (Exception ex)
            {
                ConsoleSink.WriteLine($"Update failed: {ex.Message}");
                return 1;
            }

            return 0;
        }

        private static void LaunchApplyUpdateProcess(string zipPath, int pid)
        {
            var exe = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrWhiteSpace(exe))
                exe = Path.Combine(AppContext.BaseDirectory, "TopSpeed.Server");

            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                Arguments = $"--apply-update \"{zipPath}\" {pid}",
                WorkingDirectory = AppContext.BaseDirectory,
                UseShellExecute = false,
                CreateNoWindow = false
            });
        }

        private static void WaitForProcessExit(int pid)
        {
            try
            {
                var process = Process.GetProcessById(pid);
                process.WaitForExit();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
        }

        private static void ExtractZip(string zipPath, string targetDir)
        {
            zipPath = Path.GetFullPath(zipPath);
            targetDir = Path.GetFullPath(targetDir);

            if (!File.Exists(zipPath))
                throw new FileNotFoundException("Update zip was not found.", zipPath);
            if (!Directory.Exists(targetDir))
                throw new DirectoryNotFoundException($"Target directory was not found: {targetDir}");

            using var archive = ZipFile.OpenRead(zipPath);
            for (var i = 0; i < archive.Entries.Count; i++)
            {
                var entry = archive.Entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.FullName))
                    continue;
                if (string.IsNullOrEmpty(entry.Name))
                    continue;

                var destination = Path.GetFullPath(Path.Combine(targetDir, entry.FullName));
                if (!destination.StartsWith(targetDir, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Unsafe entry path: {entry.FullName}");

                var parent = Path.GetDirectoryName(destination);
                if (!string.IsNullOrWhiteSpace(parent))
                    Directory.CreateDirectory(parent);

                entry.ExtractToFile(destination, overwrite: true);
            }
        }

        private static bool HasArgument(string[] args, string key)
        {
            foreach (var arg in args)
            {
                if (string.Equals(arg, key, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static ServerUpdateConfig BuildUpdateConfig(string[] args)
        {
            var owner = GetArgumentValue(args, "--update-owner");
            var repo = GetArgumentValue(args, "--update-repo");
            var infoRef = GetArgumentValue(args, "--update-info-ref");
            var releaseTag = GetArgumentValue(args, "--update-release-tag");

            return ServerUpdateConfig.FromRepository(owner, repo, infoRef, releaseTag);
        }

        /// <summary>
        /// Returns the argument that follows the value of <paramref name="key"/>.
        /// For "--apply-update /path/to/zip.zip 12345", calling with "--apply-update" returns "12345" (the PID),
        /// because "/path/to/zip.zip" is the value of the key and "12345" is the next positional argument after it.
        /// </summary>
        private static string? GetNextPositionalArgAfter(string[] args, string key)
        {
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (!string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
                    continue;
                // args[i+1] is the value (zip path); args[i+2] is the next positional arg (pid).
                if (i + 2 < args.Length)
                    return args[i + 2];
            }

            return null;
        }
    }
}
