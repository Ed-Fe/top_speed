namespace TopSpeed.Protocol
{
    // Edit protocol versioning values here.
    public static class ProtocolVersionInfo
    {
        // Packet envelope version (header byte).
        public const byte PacketVersion = 0x1F;

        // Current release version (year.month.day.revision).
        public const ushort CurrentYear = 2026;
        public const byte CurrentMonth = 3;
        public const byte CurrentDay = 11;
        public const byte CurrentRevision = 2;

        // Client supported protocol range (explicit values by design).
        public const ushort ClientMinYear = 2026;
        public const byte ClientMinMonth = 3;
        public const byte ClientMinDay = 2;
        public const byte ClientMinRevision = 1;
        public const ushort ClientMaxYear = 2026;
        public const byte ClientMaxMonth = 3;
        public const byte ClientMaxDay = 11;
        public const byte ClientMaxRevision = 2;

        // Server supported protocol range (explicit values by design).
        public const ushort ServerMinYear = 2026;
        public const byte ServerMinMonth = 3;
        public const byte ServerMinDay = 2;
        public const byte ServerMinRevision = 1;
        public const ushort ServerMaxYear = 2026;
        public const byte ServerMaxMonth = 3;
        public const byte ServerMaxDay = 11;
        public const byte ServerMaxRevision = 2;
    }
}
