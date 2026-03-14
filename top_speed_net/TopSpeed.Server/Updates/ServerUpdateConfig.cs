using System;
using System.Runtime.InteropServices;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Updates
{
    internal sealed class ServerUpdateConfig
    {
        private const string RepoOwner = "diamondStar35";
        private const string RepoName = "top_speed";

        public ServerUpdateConfig(
            string infoUrl,
            string latestReleaseApiUrl,
            string assetTemplate)
        {
            InfoUrl = infoUrl ?? throw new ArgumentNullException(nameof(infoUrl));
            LatestReleaseApiUrl = latestReleaseApiUrl ?? throw new ArgumentNullException(nameof(latestReleaseApiUrl));
            AssetTemplate = assetTemplate ?? throw new ArgumentNullException(nameof(assetTemplate));
        }

        public string InfoUrl { get; }
        public string LatestReleaseApiUrl { get; }
        public string AssetTemplate { get; }

        public static ServerUpdateConfig Default { get; } = new ServerUpdateConfig(
            $"https://raw.githubusercontent.com/{RepoOwner}/{RepoName}/main/info.json",
            $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest",
            "TopSpeed.Server-{rid}-Release-v-{version}.zip");

        public static ServerVersion CurrentVersion =>
            new ServerVersion(
                ReleaseVersionInfo.ServerYear,
                ReleaseVersionInfo.ServerMonth,
                ReleaseVersionInfo.ServerDay,
                ReleaseVersionInfo.ServerRevision);

        public static string CurrentRid => DetectRid();

        public string BuildExpectedAssetName(string version, string rid)
        {
            return AssetTemplate
                .Replace("{version}", version ?? string.Empty)
                .Replace("{rid}", rid ?? string.Empty);
        }

        private static string DetectRid()
        {
            // Use the runtime identifier that the current process was compiled for.
            var rid = RuntimeInformation.RuntimeIdentifier;

            // Normalize the two RIDs whose release zip names differ from the publish RID.
            if (string.Equals(rid, "linux-arm", StringComparison.OrdinalIgnoreCase))
                return "linux-arm32";
            if (string.Equals(rid, "linux-x86", StringComparison.OrdinalIgnoreCase))
                return "linux-x86-fdd";

            return rid;
        }
    }
}
