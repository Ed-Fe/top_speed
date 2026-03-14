using System;
using System.IO;
using System.Threading;
using TopSpeed.Protocol;
using TopSpeed.Server.Config;
using TopSpeed.Server.Logging;
using TopSpeed.Server.Network;

namespace TopSpeed.Server
{
    internal static partial class Program
    {
        private static int Main(string[] args)
        {
            if (IsHelpRequested(args))
            {
                ShowHelp();
                return 0;
            }

            var updateExitCode = TryHandleUpdateCommand(args);
            if (updateExitCode.HasValue)
                return updateExitCode.Value;

            using var timerResolution = new WindowsTimerResolution(1);

            var loggingEnabled = args.Length > 0;
            var levels = loggingEnabled ? ParseLogLevels(args) : LogLevel.None;
            var configuredLogFile = GetArgumentValue(args, "--log-file");
            var logFile = loggingEnabled && !string.IsNullOrWhiteSpace(configuredLogFile)
                ? BuildLogFilePath(configuredLogFile!)
                : null;
            using var logger = new Logger(levels, logFile, writeToConsole: loggingEnabled);
            var serverRelease = $"{ReleaseVersionInfo.ServerYear}.{ReleaseVersionInfo.ServerMonth}.{ReleaseVersionInfo.ServerDay} (r{ReleaseVersionInfo.ServerRevision})";
            if (loggingEnabled)
            {
                logger.InfoAlways($"Logging enabled. Levels: {FormatLogLevels(levels)}. File: {(string.IsNullOrWhiteSpace(logFile) ? "none" : logFile)}.");
                logger.InfoAlways($"Server release: {serverRelease}.");
                logger.InfoAlways($"Protocol current: {ProtocolProfile.Current}. Supported: {ProtocolProfile.ServerSupported}.");
                logger.Info("TopSpeed Server starting.");
            }
            else
            {
                ConsoleSink.WriteLine("TopSpeed Server starting...");
                ConsoleSink.WriteLine($"Server release: {serverRelease}");
                ConsoleSink.WriteLine($"Protocol version: {ProtocolProfile.Current}");
            }

            var settingsPath = Path.Combine(AppContext.BaseDirectory, "settings.json");
            var store = new ServerSettingsStore(settingsPath);
            var settings = store.LoadOrCreate(logger);
            ApplyArgumentOverrides(settings, args, logger);
            store.Save(settings, logger);

            var config = new RaceServerConfig
            {
                Port = settings.Port,
                DiscoveryPort = settings.DiscoveryPort,
                MaxPlayers = settings.MaxPlayers,
                Motd = settings.Motd
            };
            if (loggingEnabled)
                logger.Info($"Server configuration: port={config.Port}, discoveryPort={config.DiscoveryPort}, maxPlayers={config.MaxPlayers}.");

            using var server = new RaceServer(config, logger);
            using var discovery = new ServerDiscoveryService(server, config, logger);
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            server.Start();
            discovery.Start();
            if (!loggingEnabled)
                ConsoleSink.WriteLine("Server started. Press Ctrl+C to stop.");
            StartBackgroundUpdateCheck();
            RunLoop(server, cts.Token);
            discovery.Stop();
            server.Stop();
            if (loggingEnabled)
                logger.Info("TopSpeed Server stopped.");
            else
                ConsoleSink.WriteLine("Server stopped.");
            return 0;
        }
    }
}
