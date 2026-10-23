using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Museum
{
    public sealed class UsbContentSync : MonoBehaviour
    {
        [SerializeField] private ArtifactScreen screen;
        [Tooltip("Optional MuseumContent folder for Editor testing. Leave empty for USB discovery.")]
        [SerializeField] private string editorSourceFolder = "";
        [SerializeField, Min(2)] private float pollSeconds = 3;
        [Tooltip("Maximum total decoded icon pixels, to bound GPU memory use.")]
        [SerializeField] private int maxIconPixels = 16000000;
        private ContentStore store;
        private string activeFingerprint;
        private string observedFingerprint;
        private string observedFolder;
        private string lastError;

        private IEnumerator Start()
        {
#if UNITY_EDITOR
            if (!string.IsNullOrWhiteSpace(editorSourceFolder))
                editorSourceFolder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", editorSourceFolder));
#endif
            store = new ContentStore(Path.Combine(Application.persistentDataPath, "MuseumPlatform2"));
            try
            {
                var saved = store.Load();
                if (saved != null) { screen.ShowCollection(saved.Folder, saved.Ids); activeFingerprint = saved.Fingerprint; }
                screen.SetStatus(saved == null ? "Insert content USB to begin." : "Saved content loaded. USB may be removed.");
            }
            catch (Exception e) { Report(e); }
            while (true)
            {
                // Discovery, hashing and disk reads run off the UI thread.
                Task<ContentStore.Collection> scan = Task.Run(() => Scan());
                while (!scan.IsCompleted) yield return null;
                if (scan.IsFaulted) { observedFingerprint = null; Report(scan.Exception.GetBaseException()); }
                else
                {
                    var source = scan.Result;
                    if (source == null) { observedFingerprint = null; observedFolder = null; }
                    else if (source.Fingerprint == activeFingerprint)
                        screen.SetStatus("Content up to date. USB may be removed.");
                    else if (source.Fingerprint != observedFingerprint || source.Folder != observedFolder)
                    {
                        observedFingerprint = source.Fingerprint;
                        observedFolder = source.Folder;
                        screen.SetStatus("Content detected. Waiting for files to settle...");
                    }
                    else
                    {
                        yield return ImportCollection(source);
                    }
                }
                yield return new WaitForSecondsRealtime(Mathf.Max(2, pollSeconds));
            }
        }
        private IEnumerator ImportCollection(ContentStore.Collection source)
        {
            screen.SetStatus("Updating content. Keep USB connected...");
            Task<ContentStore.Collection> copy = Task.Run(() => store.Stage(source));
            while (!copy.IsCompleted) yield return null;
            if (copy.IsFaulted) { observedFingerprint = null; Report(copy.Exception.GetBaseException()); yield break; }
            ContentStore.Collection staged = copy.Result;
            Exception failure = null;
            bool committed = false;
            // Iterator finally also runs if the component is disabled/destroyed mid-validation.
            try
            {
                long iconPixels = 0;
                foreach (string id in staged.Ids)
                {
                    foreach (string category in new[] { "Icons", "Descriptions" })
                    {
                        try
                        {
                            string path = Path.Combine(staged.Folder, category, id + ".png");
                            Texture2D texture = ArtifactScreen.LoadPng(path);
                            if (category == "Icons") iconPixels += (long)texture.width * texture.height;
                            DestroyImmediate(texture);
                            if (iconPixels > maxIconPixels) throw new InvalidDataException("Icon images exceed total pixel budget; reduce icon resolution.");
                        }
                        catch (Exception e) { failure = e; }
                        if (failure != null) break;
                        yield return null;
                    }
                    if (failure != null) break;
                }
                if (failure == null)
                {
                    // Final source recheck and pointer switch are deliberately indivisible to this coroutine.
                    try { store.Commit(source, staged); committed = true; }
                    catch (Exception e) { failure = e; }
                }
                if (committed)
                {
                    try
                    {
                        screen.ShowCollection(staged.Folder, staged.Ids);
                        activeFingerprint = staged.Fingerprint;
                        screen.SetStatus("Update complete. USB may be removed.");
                    }
                    catch (Exception e) { failure = e; }
                }
            }
            finally
            {
                if (!committed) store.Discard(staged);
            }
            if (failure != null) { observedFingerprint = null; Report(failure); }
        }
        private ContentStore.Collection Scan()
        {
            string[] sources = FindSources().Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (sources.Length > 1) throw new IOException("Multiple content USB sources found. Connect only one.");
            return sources.Length == 0 ? null : store.Inspect(sources[0]);
        }
        private IEnumerable<string> FindSources()
        {
#if UNITY_EDITOR
            if (!string.IsNullOrWhiteSpace(editorSourceFolder))
            {
                yield return Path.GetFullPath(editorSourceFolder);
                yield break;
            }
#endif
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                bool ready;
                try { ready = drive.IsReady && (drive.DriveType == DriveType.Removable ||
                    (Path.DirectorySeparatorChar == '\\' && drive.DriveType == DriveType.Fixed)); }
                catch (IOException) { continue; }
                catch (UnauthorizedAccessException) { continue; }
                if (!ready) continue;
                string candidate = Path.Combine(drive.RootDirectory.FullName, "MuseumContent");
                if (File.Exists(Path.Combine(candidate, "content-source.txt"))) yield return candidate;
            }
            // Common Linux mount locations, useful for development. Target delivery is Windows x64.
            foreach (string mountRoot in new[] { "/media", "/run/media", "/mnt" })
            {
                if (!Directory.Exists(mountRoot)) continue;
                foreach (string first in Directory.GetDirectories(mountRoot))
                {
                    string direct = Path.Combine(first, "MuseumContent");
                    if (File.Exists(Path.Combine(direct, "content-source.txt"))) yield return direct;
                    foreach (string second in Directory.GetDirectories(first))
                    {
                        string nested = Path.Combine(second, "MuseumContent");
                        if (File.Exists(Path.Combine(nested, "content-source.txt"))) yield return nested;
                    }
                }
            }
        }
        private void Report(Exception e)
        {
            if (lastError != e.Message) Debug.LogWarning("Content sync: " + e.Message);
            lastError = e.Message;
            screen.SetStatus("Update blocked: " + e.Message + " Existing content retained.");
        }
    }
}
