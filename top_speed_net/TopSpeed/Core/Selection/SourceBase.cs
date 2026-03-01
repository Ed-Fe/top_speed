using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TopSpeed.Core
{
    internal abstract class SourceBase<TInfo>
    {
        private readonly Dictionary<string, (DateTime LastWriteUtc, TInfo Value)> _cache =
            new Dictionary<string, (DateTime LastWriteUtc, TInfo Value)>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _issues = new List<string>();
        private readonly string _rootFolder;
        private readonly string _pattern;

        protected SourceBase(string rootFolder, string pattern)
        {
            _rootFolder = rootFolder;
            _pattern = pattern;
        }

        public IEnumerable<string> GetFiles()
        {
            return GetInfo().Select(GetKey);
        }

        public IReadOnlyList<TInfo> GetInfo()
        {
            _issues.Clear();
            var files = Scan.Find(_rootFolder, _pattern);
            if (files.Count == 0)
            {
                _cache.Clear();
                return Array.Empty<TInfo>();
            }

            var items = new List<TInfo>(files.Count);
            var known = new HashSet<string>(files, StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];
                if (Scan.TryCached(file, _cache, Parse, out var info))
                    items.Add(info);
            }

            Scan.Prune(_cache, known);
            return items
                .OrderBy(GetDisplay, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public IReadOnlyList<string> ConsumeIssues()
        {
            if (_issues.Count == 0)
                return Array.Empty<string>();

            var copy = _issues.ToArray();
            _issues.Clear();
            return copy;
        }

        protected abstract string GetKey(TInfo info);
        protected abstract string GetDisplay(TInfo info);
        protected abstract (bool Success, TInfo Value) ParseCore(string file);

        protected void AddFileIssue(string file)
        {
            _issues.Add($"File: {Path.GetFileName(file)}");
        }

        protected void AddIssue(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
            _issues.Add(message);
        }

        private (bool Success, TInfo Value) Parse(string file)
        {
            try
            {
                return ParseCore(file);
            }
            catch (Exception ex)
            {
                AddFileIssue(file);
                AddIssue(ex.Message);
                return (false, default!);
            }
        }
    }
}
