using System;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Updates
{
    internal sealed class UpdateConfig
    {
        private const string RepoOwner = "diamondStar35";
        private const string RepoName = "top_speed";

        public UpdateConfig(
            string infoUrl,
            string latestReleaseApiUrl,
            string assetTemplate,
            string updaterExeName,
            string gameExeName)
        {
            InfoUrl = infoUrl ?? throw new ArgumentNullException(nameof(infoUrl));
            LatestReleaseApiUrl = latestReleaseApiUrl ?? throw new ArgumentNullException(nameof(latestReleaseApiUrl));
            AssetTemplate = assetTemplate ?? throw new ArgumentNullException(nameof(assetTemplate));
            UpdaterExeName = updaterExeName ?? throw new ArgumentNullException(nameof(updaterExeName));
            GameExeName = gameExeName ?? throw new ArgumentNullException(nameof(gameExeName));
        }

        public string InfoUrl { get; }
        public string LatestReleaseApiUrl { get; }
        public string AssetTemplate { get; }
        public string UpdaterExeName { get; }
        public string GameExeName { get; }

        public static UpdateConfig Default { get; } = new UpdateConfig(
            $"https://raw.githubusercontent.com/{RepoOwner}/{RepoName}/main/info.json",
            $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest",
            "TopSpeed-Release-v-{version}.zip",
            "Updater.exe",
            "TopSpeed.exe");

        public static GameVersion CurrentVersion =>
            new GameVersion(
                ReleaseVersionInfo.ClientYear,
                ReleaseVersionInfo.ClientMonth,
                ReleaseVersionInfo.ClientDay,
                ReleaseVersionInfo.ClientRevision);

        public string BuildExpectedAssetName(string version)
        {
            return AssetTemplate.Replace("{version}", version ?? string.Empty);
        }
    }
}
