using System;
using System.Globalization;
using System.IO;
using TopSpeed.Server.Config;
using TopSpeed.Server.Logging;

namespace TopSpeed.Server
{
    internal static partial class Program
    {
        private static LogLevel ParseLogLevels(string[] args)
        {
            var value = GetArgumentValue(args, "--log");
            if (string.IsNullOrWhiteSpace(value))
                return LogLevel.Error | LogLevel.Warning | LogLevel.Info;

            var levels = LogLevel.None;
            var parts = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var token = part.Trim().ToLowerInvariant();
                switch (token)
                {
                    case "error":
                        levels |= LogLevel.Error;
                        break;
                    case "warning":
                        levels |= LogLevel.Warning;
                        break;
                    case "info":
                        levels |= LogLevel.Info;
                        break;
                    case "debug":
                        levels |= LogLevel.Debug;
                        break;
                    case "all":
                        levels = LogLevel.All;
                        break;
                }
            }

            return levels == LogLevel.None
                ? LogLevel.Error | LogLevel.Warning | LogLevel.Info
                : levels;
        }

        private static bool IsHelpRequested(string[] args)
        {
            foreach (var arg in args)
            {
                if (string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static void ShowHelp()
        {
            ConsoleSink.WriteLine("TopSpeed.Server usage:");
            ConsoleSink.WriteLine("  TopSpeed.Server [options]");
            ConsoleSink.WriteLine(string.Empty);
            ConsoleSink.WriteLine("Options:");
            ConsoleSink.WriteLine("  --port <number>         Server port (1-65535).");
            ConsoleSink.WriteLine("  --max-players <number>  Max connected players (1-255).");
            ConsoleSink.WriteLine("  --motd <text>           Message of the day.");
            ConsoleSink.WriteLine("  --log <levels>          Comma-separated levels: error,warning,info,debug,all.");
            ConsoleSink.WriteLine("  --log-file <path>       Output log file path (e.g. log.txt).");
            ConsoleSink.WriteLine("  --check-update          Check for a newer server version and exit.");
            ConsoleSink.WriteLine("  --update                Download and apply the latest server update, then exit.");
            ConsoleSink.WriteLine("  -h, --help              Show this help.");
        }

        private static string FormatLogLevels(LogLevel levels)
        {
            if (levels == LogLevel.None)
                return "none";
            if (levels == LogLevel.All)
                return "all";

            var parts = new System.Collections.Generic.List<string>();
            if ((levels & LogLevel.Error) != 0)
                parts.Add("error");
            if ((levels & LogLevel.Warning) != 0)
                parts.Add("warning");
            if ((levels & LogLevel.Info) != 0)
                parts.Add("info");
            if ((levels & LogLevel.Debug) != 0)
                parts.Add("debug");
            return parts.Count == 0 ? "none" : string.Join(",", parts);
        }

        private static string? GetArgumentValue(string[] args, string key)
        {
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (string.Equals(arg, key, StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length)
                        return args[i + 1];
                    return null;
                }

                if (arg.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
                    return arg.Substring(key.Length + 1);
            }

            return null;
        }

        private static string BuildLogFilePath(string configuredPath)
        {
            var trimmed = configuredPath.Trim().Trim('"');
            if (Path.IsPathRooted(trimmed))
                return trimmed;

            return Path.GetFullPath(trimmed, AppContext.BaseDirectory);
        }

        private static void ApplyArgumentOverrides(ServerSettings settings, string[] args, Logger logger)
        {
            if (TryGetIntArg(args, "--port", out var port))
            {
                if (port >= 1 && port <= 65535)
                    settings.Port = port;
                else
                    logger.Warning("Invalid --port value. Using configured port.");
            }

            if (TryGetIntArg(args, "--max-players", out var maxPlayers))
            {
                if (maxPlayers >= 1 && maxPlayers <= byte.MaxValue)
                    settings.MaxPlayers = maxPlayers;
                else
                    logger.Warning("Invalid --max-players value. Using configured max players.");
            }

            var motd = GetArgumentValue(args, "--motd");
            if (!string.IsNullOrWhiteSpace(motd))
                settings.Motd = motd.Trim();
        }

        private static bool TryGetIntArg(string[] args, string key, out int value)
        {
            value = 0;
            var raw = GetArgumentValue(args, key);
            if (string.IsNullOrWhiteSpace(raw))
                return false;
            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }
    }
}
