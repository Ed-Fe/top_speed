using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TopSpeed.Server.Updates
{
    [DataContract]
    internal sealed class ServerInfoDoc
    {
        [DataMember(Name = "serverVersion")]
        public string? ServerVersion { get; set; }

        [DataMember(Name = "serverChanges")]
        public List<string>? ServerChanges { get; set; }
    }

    [DataContract]
    internal sealed class ServerReleaseDoc
    {
        [DataMember(Name = "assets")]
        public List<ServerReleaseAssetDoc>? Assets { get; set; }
    }

    [DataContract]
    internal sealed class ServerReleaseAssetDoc
    {
        [DataMember(Name = "name")]
        public string? Name { get; set; }

        [DataMember(Name = "browser_download_url")]
        public string? DownloadUrl { get; set; }

        [DataMember(Name = "size")]
        public long? Size { get; set; }
    }
}
