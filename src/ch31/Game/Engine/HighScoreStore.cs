using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Retro.Engine;

/// <summary>
/// Reads and writes the high score table (and the audio settings) to the app's
/// private storage.
///
/// Two deliberate choices:
///  - A plain line-based text format instead of JSON serialisation. iOS builds
///    are AOT-compiled and trimmed; reflection-based serialisers are exactly the
///    thing that works in the simulator and then throws on a real device because
///    the type metadata was trimmed away. Eleven lines of parsing has no such
///    failure mode, and you can read the file over the wire while debugging.
///  - The save is atomic: write to a temporary file, flush it, then move it over
///    the real one. A phone killed mid-write then loses the new scores rather
///    than the whole table.
/// </summary>
public sealed class HighScoreStore
{
    private const int Version = 2;

    private readonly string _path;

    public HighScoreStore(string fileName)
    {
        var dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrEmpty(dir)) dir = Path.GetTempPath();
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, fileName);
    }

    public string Path_ => _path;

    public float MusicVolume { get; set; } = 0.7f;
    public float EffectVolume { get; set; } = 0.9f;

    public bool Load(HighScoreTable table)
    {
        try
        {
            if (!File.Exists(_path)) return false;
            var lines = File.ReadAllLines(_path);
            if (lines.Length == 0) return false;

            var head = lines[0].Split('|');
            if (head.Length < 2 || head[0] != "RETRO") return false;
            int version = int.Parse(head[1], CultureInfo.InvariantCulture);
            if (version < 1 || version > Version) return false;

            var entries = new List<ScoreEntry>();
            int checksum = 0;
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.StartsWith("SUM|", StringComparison.Ordinal))
                {
                    checksum = int.Parse(line[4..], CultureInfo.InvariantCulture);
                    continue;
                }
                if (line.StartsWith("VOL|", StringComparison.Ordinal))
                {
                    var v = line[4..].Split(',');
                    if (v.Length == 2)
                    {
                        MusicVolume = float.Parse(v[0], CultureInfo.InvariantCulture);
                        EffectVolume = float.Parse(v[1], CultureInfo.InvariantCulture);
                    }
                    continue;
                }
                var f = line.Split('|');
                if (f.Length < 3) continue;
                entries.Add(new ScoreEntry(f[0],
                    int.Parse(f[1], CultureInfo.InvariantCulture),
                    int.Parse(f[2], CultureInfo.InvariantCulture)));
            }

            // Version 1 files had no checksum; accept them and upgrade on next save.
            if (version >= 2 && checksum != Checksum(entries)) return false;

            table.Load(entries);
            return true;
        }
        catch (Exception)
        {
            // A corrupt or unreadable table must never stop the game booting.
            return false;
        }
    }

    public bool Save(HighScoreTable table)
    {
        var tmp = _path + ".tmp";
        try
        {
            var sb = new StringBuilder();
            sb.Append("RETRO|").Append(Version).Append('\n');
            sb.Append("VOL|").Append(MusicVolume.ToString("0.###", CultureInfo.InvariantCulture))
              .Append(',').Append(EffectVolume.ToString("0.###", CultureInfo.InvariantCulture)).Append('\n');
            foreach (var e in table.Entries)
                sb.Append(e.Name).Append('|').Append(e.Score).Append('|').Append(e.Stage).Append('\n');
            sb.Append("SUM|").Append(Checksum(table.Entries)).Append('\n');

            File.WriteAllText(tmp, sb.ToString(), Encoding.UTF8);
            File.Move(tmp, _path, overwrite: true);
            return true;
        }
        catch (Exception)
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { /* nothing useful to do */ }
            return false;
        }
    }

    /// <summary>
    /// Enough to catch a hand edit by someone who is not really trying. It is not
    /// security, and pretending otherwise on a device the player owns is wasted
    /// effort - the file lives in their sandbox and they can do as they please.
    /// </summary>
    private static int Checksum(IReadOnlyList<ScoreEntry> entries)
    {
        unchecked
        {
            int h = 17;
            foreach (var e in entries)
            {
                foreach (char c in e.Name) h = h * 31 + c;
                h = h * 31 + e.Score;
                h = h * 31 + e.Stage;
            }
            return h & 0x7FFFFFFF;
        }
    }
}
