using Museum;
using System.IO;

string root = Path.Combine(Path.GetTempPath(), "museum-tests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
string source = Path.Combine(root, "usb", "MuseumContent");
Directory.CreateDirectory(Path.Combine(source, "Icons"));
Directory.CreateDirectory(Path.Combine(source, "Descriptions"));
File.WriteAllText(Path.Combine(source, "content-source.txt"), ContentStore.MarkerText);
string repository = Path.GetFullPath(args.Length > 0 ? args[0] : "../..");
byte[] png = File.ReadAllBytes(Path.Combine(repository, "USB-Example/MuseumContent/Icons/001.png"));
byte[] otherPng = File.ReadAllBytes(Path.Combine(repository, "USB-Example/MuseumContent/Icons/002.png"));
var store = new ContentStore(Path.Combine(root, "pc"));
int passed = 0;
void Check(string name, Action action) { action(); passed++; Console.WriteLine("PASS " + name); }
void Assert(bool ok) { if (!ok) throw new Exception("Assertion failed"); }
void Reject(Action action) { bool failed = false; try { action(); } catch (Exception) { failed = true; } Assert(failed); }
void Put(string id, byte[] bytes) { foreach (string category in new[] { "Icons", "Descriptions" }) File.WriteAllBytes(Path.Combine(source, category, id + ".png"), bytes); }
void Delete(string id) { foreach (string category in new[] { "Icons", "Descriptions" }) File.Delete(Path.Combine(source, category, id + ".png")); }
void Validate(string path) { ContentStore.ValidateHeader(path); }
try
{
    Check("first launch has no content", () => Assert(store.Load() == null));
    Check("unmarked folders rejected", () => {
        File.Delete(Path.Combine(source, "content-source.txt")); Reject(() => store.Inspect(source));
        File.WriteAllText(Path.Combine(source, "content-source.txt"), ContentStore.MarkerText);
    });
    Check("empty USB cannot silently clear content", () => Reject(() => store.Inspect(source)));
    Put("001", png);
    Check("initial import", () => { var data = store.Import(store.Inspect(source), Validate); Assert(data.Ids.SequenceEqual(new[] { "001" })); });
    string first = store.Load().Fingerprint;
    Check("same source has stable fingerprint", () => Assert(store.Inspect(source).Fingerprint == first));
    Check("restart reads PC copy after USB removal", () => {
        Directory.Move(source, source + "-offline");
        Assert(new ContentStore(Path.Combine(root, "pc")).Load().Fingerprint == first);
        Directory.Move(source + "-offline", source);
    });
    Check("addition", () => { Put("002", otherPng); store.Import(store.Inspect(source), Validate); Assert(store.Load().Ids.Length == 2); });
    Check("deletion reflected in new snapshot", () => { Delete("001"); store.Import(store.Inspect(source), Validate); Assert(store.Load().Ids.SequenceEqual(new[] { "002" })); });
    Check("same-name replacement", () => { string before = store.Load().Fingerprint; Put("002", png); store.Import(store.Inspect(source), Validate); Assert(store.Load().Fingerprint != before); });
    string good = store.Load().Fingerprint;
    Check("missing pair retains saved collection", () => {
        File.Delete(Path.Combine(source, "Descriptions", "002.png")); Reject(() => store.Inspect(source));
        Assert(store.Load().Fingerprint == good); Put("002", png);
    });
    Check("decoder failure never commits", () => {
        Reject(() => store.Import(store.Inspect(source), _ => throw new InvalidDataException("decoder failure")));
        Assert(store.Load().Fingerprint == good);
    });
    Check("USB removal during copy never commits", () => {
        Reject(() => store.Import(store.Inspect(source), _ => { Directory.Move(source, source + "-offline"); }));
        Directory.Move(source + "-offline", source); Assert(store.Load().Fingerprint == good);
    });
    Check("source change during copy never commits", () => {
        bool changed = false;
        Reject(() => store.Import(store.Inspect(source), _ => { if (!changed) { Put("002", otherPng); changed = true; } }));
        Assert(store.Load().Fingerprint == good); Put("002", png);
    });
    Check("bad PNG signature rejected", () => {
        Put("002", new byte[64]); Reject(() => store.Inspect(source)); Assert(store.Load().Fingerprint == good); Put("002", png);
    });
    Check("oversized dimensions rejected before decode", () => {
        var bytes = (byte[])png.Clone(); bytes[16] = 127; Put("002", bytes); Reject(() => store.Inspect(source)); Put("002", png);
    });
    Check("unsafe filenames rejected", () => { Put("Bad Name", png); Reject(() => store.Inspect(source)); Delete("Bad Name"); });
    Check("explicit empty collection supported", () => {
        Delete("002"); File.WriteAllText(Path.Combine(source, "allow-empty.txt"), "");
        store.Import(store.Inspect(source), Validate); Assert(store.Load().Ids.Length == 0);
    });
    Check("rollback pointer preserved", () => Assert(File.Exists(Path.Combine(root, "pc", "previous.txt"))));
    Console.WriteLine($"{passed} checks passed.");
    return 0;
}
finally { Directory.Delete(root, true); }
