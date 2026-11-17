using BinaryKits.Zpl.Viewer.DownloadedFonts;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BinaryKits.Zpl.Viewer
{
    public class PrinterStorage : IPrinterStorage, IDisposable
    {
        private const int maxLinksPerBase = 5;

        private readonly object sync = new();
        private readonly Dictionary<string, byte[]> files = new(StringComparer.Ordinal);
        private readonly Dictionary<string, StoredFont> fonts = new(StringComparer.Ordinal);

        // Insertion order of the bases matters for wildcard lookup order
        private readonly List<KeyValuePair<string, List<string>>> fontLinks = [];

        private bool disposed;

        public void AddFile(char storageDevice, string fileName, byte[] data)
        {
            string key = GetKey(storageDevice, fileName);

            lock (this.sync)
            {
                ObjectDisposedException.ThrowIf(this.disposed, this);

                this.RemoveFont(key);
                this.files[key] = data;
            }
        }

        public byte[] GetFile(char storageDevice, string fileName)
        {
            string key = GetKey(storageDevice, fileName);

            lock (this.sync)
            {
                return this.files.TryGetValue(key, out byte[] data) && data != null ? data : [];
            }
        }

        public void AddFont(char storageDevice, string fileName, ZplDownloadedFont font, bool ownsFont = true)
        {
            ArgumentNullException.ThrowIfNull(font);

            string key = GetKey(storageDevice, fileName);

            lock (this.sync)
            {
                ObjectDisposedException.ThrowIf(this.disposed, this);

                this.files.Remove(key);

                if (this.fonts.TryGetValue(key, out StoredFont previous) && !ReferenceEquals(previous.Font, font))
                {
                    previous.DisposeIfOwned();
                }

                this.fonts[key] = new StoredFont(font, ownsFont);
            }
        }

        public bool TryGetFont(char storageDevice, string fileName, out ZplDownloadedFont font)
        {
            string key = GetKey(storageDevice, fileName);

            lock (this.sync)
            {
                bool found = this.fonts.TryGetValue(key, out StoredFont stored);
                font = stored.Font;
                return found;
            }
        }

        public void SetFontLink(string extensionFont, string baseFont, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(extensionFont) || string.IsNullOrWhiteSpace(baseFont))
            {
                return;
            }

            string extension = extensionFont.Trim().ToUpperInvariant();
            string basePattern = baseFont.Trim().ToUpperInvariant();

            lock (this.sync)
            {
                int index = this.fontLinks.FindIndex(o => o.Key == basePattern);
                List<string> links = index >= 0 ? this.fontLinks[index].Value : [];

                links.Remove(extension);

                if (enabled)
                {
                    links.Add(extension);

                    if (links.Count > maxLinksPerBase)
                    {
                        links.RemoveRange(0, links.Count - maxLinksPerBase);
                    }
                }

                if (index < 0 && links.Count > 0)
                {
                    this.fontLinks.Add(new KeyValuePair<string, List<string>>(basePattern, links));
                }
                else if (index >= 0 && links.Count == 0)
                {
                    this.fontLinks.RemoveAt(index);
                }
            }
        }

        public IReadOnlyList<string> GetFontLinks(string baseFont)
        {
            if (string.IsNullOrWhiteSpace(baseFont))
            {
                return [];
            }

            string baseKey = baseFont.Trim().ToUpperInvariant();
            List<string> result = [];

            lock (this.sync)
            {
                foreach (KeyValuePair<string, List<string>> entry in this.fontLinks)
                {
                    if (entry.Key == baseKey)
                    {
                        AddDistinct(result, entry.Value);
                    }
                }

                foreach (KeyValuePair<string, List<string>> entry in this.fontLinks)
                {
                    if (entry.Key != baseKey && IsWildcard(entry.Key) && WildcardMatches(entry.Key, baseKey))
                    {
                        AddDistinct(result, entry.Value);
                    }
                }
            }

            result.Remove(baseKey);
            return result;
        }

        public void Dispose()
        {
            lock (this.sync)
            {
                if (this.disposed)
                {
                    return;
                }

                this.disposed = true;

                foreach (StoredFont stored in this.fonts.Values)
                {
                    stored.DisposeIfOwned();
                }

                this.fonts.Clear();
                this.files.Clear();
                this.fontLinks.Clear();
            }

            GC.SuppressFinalize(this);
        }

        private void RemoveFont(string key)
        {
            if (this.fonts.Remove(key, out StoredFont previous))
            {
                previous.DisposeIfOwned();
            }
        }

        private static string GetKey(char storageDevice, string fileName)
        {
            return $"{char.ToUpperInvariant(storageDevice)}:{(fileName ?? string.Empty).Trim().ToUpperInvariant()}";
        }

        private static void AddDistinct(List<string> target, List<string> source)
        {
            foreach (string item in source)
            {
                if (!target.Contains(item))
                {
                    target.Add(item);
                }
            }
        }

        private static bool IsWildcard(string pattern)
        {
            return pattern.IndexOf('*') >= 0 || pattern.IndexOf('?') >= 0;
        }

        private static bool WildcardMatches(string pattern, string value)
        {
            string regex = "^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
            return Regex.IsMatch(value, regex, RegexOptions.CultureInvariant);
        }

        private readonly record struct StoredFont(ZplDownloadedFont Font, bool Owned)
        {
            public void DisposeIfOwned()
            {
                if (this.Owned)
                {
                    this.Font.Dispose();
                }
            }
        }
    }
}
