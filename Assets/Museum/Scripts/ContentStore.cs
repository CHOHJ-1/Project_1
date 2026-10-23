using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Museum
{
    // Plain C#: can be tested independently of the Unity Editor.
    public sealed class ContentStore
    {
        public const string MarkerText = "museum-platform-2";
        public const long MaxFileBytes = 32L * 1024 * 1024;
        public const long MaxTotalBytes = 256L * 1024 * 1024;
        public const int MaxItems = 200;
        public const int MaxDimension = 8192;
        private readonly string root;
        public ContentStore(string root) { this.root = root; Directory.CreateDirectory(root); }

        public sealed class Collection
        {
            public string Folder;
            public string[] Ids;
            public string Fingerprint;
        }
        public Collection Inspect(string folder)
        {
            if (File.ReadAllText(Path.Combine(folder, "content-source.txt")).Trim() != MarkerText)
                throw new InvalidDataException("Missing or incorrect content-source.txt marker.");
            string icons = Path.Combine(folder, "Icons");
            string descriptions = Path.Combine(folder, "Descriptions");
            var left = ReadFiles(icons);
            var right = ReadFiles(descriptions);
            if (!left.Keys.OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(right.Keys.OrderBy(x => x, StringComparer.Ordinal)))
                throw new InvalidDataException("Icons and Descriptions must have identical PNG filenames.");
            if (left.Count > MaxItems) throw new InvalidDataException("Too many artifacts (maximum 200).");
            if (left.Count == 0 && !File.Exists(Path.Combine(folder, "allow-empty.txt")))
                throw new InvalidDataException("Empty collection blocked. Add allow-empty.txt to explicitly clear the collection.");
            string[] ids = left.Keys.OrderBy(x => x, StringComparer.Ordinal).ToArray();
            long total = 0;
            var digest = new StringBuilder();
            foreach (string id in ids)
            {
                foreach (string path in new[] { left[id], right[id] })
                {
                    long length = new FileInfo(path).Length;
                    total += length;
                    if (length < 33 || length > MaxFileBytes || total > MaxTotalBytes)
                        throw new InvalidDataException("PNG file or collection exceeds size limits.");
                    ValidateHeader(path);
                    digest.Append(id).Append(':').Append(HashFile(path)).Append(';');
                }
            }
            return new Collection { Folder = folder, Ids = ids, Fingerprint = HashBytes(Encoding.UTF8.GetBytes(digest.ToString())) };
        }
        private static Dictionary<string, string> ReadFiles(string directory)
        {
            // Lowercase .png and portable IDs avoid case-sensitive filesystem differences.
            var files = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string path in Directory.GetFiles(directory))
            {
                if (!string.Equals(Path.GetExtension(path), ".png", StringComparison.OrdinalIgnoreCase)) continue;
                string id = Path.GetFileNameWithoutExtension(path);
                if (Path.GetExtension(path) != ".png" || id.Length == 0 || id.Length > 80 ||
                    id.Any(c => !(c >= 'a' && c <= 'z') && !(c >= '0' && c <= '9') && c != '-' && c != '_'))
                    throw new InvalidDataException("Use lowercase a-z, 0-9, - or _ filenames and .png extension.");
                files.Add(id, path);
            }
            return files;
        }
        public static void ValidateHeader(string path)
        {
            byte[] header = new byte[24];
            using (var stream = File.OpenRead(path))
                if (stream.Read(header, 0, header.Length) != header.Length) throw new InvalidDataException("Truncated PNG.");
            byte[] signature = { 137, 80, 78, 71, 13, 10, 26, 10 };
            if (!header.Take(8).SequenceEqual(signature) || header[12] != 73 || header[13] != 72 || header[14] != 68 || header[15] != 82)
                throw new InvalidDataException("Invalid PNG header.");
            uint width = ReadUInt(header, 16), height = ReadUInt(header, 20);
            if (width == 0 || height == 0 || width > MaxDimension || height > MaxDimension || (ulong)width * height > 16777216)
                throw new InvalidDataException("PNG exceeds 8192 pixels per side or 16 megapixels.");
        }
        private static uint ReadUInt(byte[] bytes, int offset)
        {
            return ((uint)bytes[offset] << 24) | ((uint)bytes[offset + 1] << 16) | ((uint)bytes[offset + 2] << 8) | bytes[offset + 3];
        }
        private static string HashBytes(byte[] bytes)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "");
        }
        private static string HashFile(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
        }
        public Collection Load()
        {
            string pointer = Path.Combine(root, "active.txt");
            if (!File.Exists(pointer)) return null;
            string name = File.ReadAllText(pointer).Trim();
            if (!name.StartsWith("generation-", StringComparison.Ordinal) || name != Path.GetFileName(name))
                throw new InvalidDataException("Invalid local collection pointer.");
            return Inspect(Path.Combine(root, name));
        }
        public Collection Stage(Collection source)
        {
            string staging = Path.Combine(root, "generation-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(Path.Combine(staging, "Icons"));
                Directory.CreateDirectory(Path.Combine(staging, "Descriptions"));
                File.WriteAllText(Path.Combine(staging, "content-source.txt"), MarkerText);
                if (source.Ids.Length == 0) File.WriteAllText(Path.Combine(staging, "allow-empty.txt"), "");
                foreach (string id in source.Ids)
                    foreach (string category in new[] { "Icons", "Descriptions" })
                        File.Copy(Path.Combine(source.Folder, category, id + ".png"), Path.Combine(staging, category, id + ".png"));
                Collection copied = Inspect(staging);
                if (copied.Fingerprint != source.Fingerprint) throw new IOException("USB files changed during copy.");
                return copied;
            }
            catch
            {
                if (Directory.Exists(staging)) Directory.Delete(staging, true);
                throw;
            }
        }
        public void Commit(Collection source, Collection staged)
        {
            if (Inspect(staged.Folder).Fingerprint != source.Fingerprint || Inspect(source.Folder).Fingerprint != source.Fingerprint)
                throw new IOException("USB files changed during import; retry after copying finishes.");
            string pointer = Path.Combine(root, "active.txt");
            string temporary = Path.Combine(root, "active.tmp");
            File.WriteAllText(temporary, Path.GetFileName(staged.Folder));
            // Same-volume atomic replacement. No destructive fallback if the filesystem cannot support it.
            if (File.Exists(pointer)) File.Replace(temporary, pointer, Path.Combine(root, "previous.txt"));
            else File.Move(temporary, pointer);
            PruneOldGenerations();
        }
        public void Discard(Collection staged)
        {
            if (staged != null && Directory.Exists(staged.Folder)) Directory.Delete(staged.Folder, true);
        }
        private void PruneOldGenerations()
        {
            // Retain active + previous snapshots. Cleanup failure must not invalidate a successful commit.
            try
            {
                var retained = new HashSet<string>(StringComparer.Ordinal);
                foreach (string pointer in new[] { "active.txt", "previous.txt" })
                {
                    string path = Path.Combine(root, pointer);
                    if (File.Exists(path)) retained.Add(File.ReadAllText(path).Trim());
                }
                foreach (string folder in Directory.GetDirectories(root, "generation-*"))
                    if (!retained.Contains(Path.GetFileName(folder))) Directory.Delete(folder, true);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        public Collection Import(Collection source, Action<string> validateImage)
        {
            Collection staged = Stage(source);
            bool committed = false;
            try
            {
                foreach (string id in staged.Ids)
                    foreach (string category in new[] { "Icons", "Descriptions" })
                        validateImage(Path.Combine(staged.Folder, category, id + ".png"));
                Commit(source, staged);
                committed = true;
                return staged;
            }
            finally { if (!committed) Discard(staged); }
        }
    }
}
