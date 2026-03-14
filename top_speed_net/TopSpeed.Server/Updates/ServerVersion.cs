using System;

namespace TopSpeed.Server.Updates
{
    internal readonly struct ServerVersion : IComparable<ServerVersion>, IEquatable<ServerVersion>
    {
        public ServerVersion(int year, int month, int day, int revision)
        {
            Year = year;
            Month = month;
            Day = day;
            Revision = revision;
        }

        public int Year { get; }
        public int Month { get; }
        public int Day { get; }
        public int Revision { get; }

        public static bool TryParse(string? value, out ServerVersion version)
        {
            version = default;
            if (value == null)
                return false;
            var text = value.Trim();
            if (text.Length == 0)
                return false;

            var parts = text.Split('.');
            if (parts.Length != 4)
                return false;
            if (!int.TryParse(parts[0], out var year))
                return false;
            if (!int.TryParse(parts[1], out var month))
                return false;
            if (!int.TryParse(parts[2], out var day))
                return false;
            if (!int.TryParse(parts[3], out var revision))
                return false;
            if (year < 2000 || year > 9999)
                return false;
            if (month < 1 || month > 12)
                return false;
            if (day < 1 || day > 31)
                return false;
            if (revision < 1 || revision > 255)
                return false;

            version = new ServerVersion(year, month, day, revision);
            return true;
        }

        public int CompareTo(ServerVersion other)
        {
            var cmp = Year.CompareTo(other.Year);
            if (cmp != 0)
                return cmp;
            cmp = Month.CompareTo(other.Month);
            if (cmp != 0)
                return cmp;
            cmp = Day.CompareTo(other.Day);
            if (cmp != 0)
                return cmp;
            return Revision.CompareTo(other.Revision);
        }

        public bool Equals(ServerVersion other)
        {
            return Year == other.Year && Month == other.Month && Day == other.Day && Revision == other.Revision;
        }

        public override bool Equals(object? obj)
        {
            return obj is ServerVersion other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Year;
                hash = (hash * 397) ^ Month;
                hash = (hash * 397) ^ Day;
                hash = (hash * 397) ^ Revision;
                return hash;
            }
        }

        public override string ToString()
        {
            return $"{Year}.{Month}.{Day} (r{Revision})";
        }

        public string ToMachineString()
        {
            return $"{Year}.{Month}.{Day}.{Revision}";
        }
    }
}
