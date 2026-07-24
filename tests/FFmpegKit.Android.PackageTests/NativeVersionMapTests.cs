using System.Text.RegularExpressions;

namespace FFmpegKit.Android.PackageTests;

/// <summary>
/// Guards the relationship between the two version schemes in play: the FFmpeg version a build
/// contains, which package versions are based on, and the FFmpegKit release that packages it,
/// which names the .aar to download. They do not track each other, so nothing but this mapping
/// connects them.
/// </summary>
public class NativeVersionMapTests
{
    private static string RepoRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "global.json")))
            {
                directory = directory.Parent;
            }

            Assert.True(directory is not null, "Could not locate the repository root.");
            return directory!.FullName;
        }
    }

    private static IReadOnlyList<(string FFmpeg, string FFmpegKit)> Mapping =>
        File.ReadAllLines(Path.Combine(RepoRoot, "build", "native-versions.tsv"))
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .Select(line => line.Split('\t', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(parts => parts.Length >= 2)
            .Select(parts => (parts[0], parts[1]))
            .ToList();

    private static string ReadProperty(string name)
    {
        var props = File.ReadAllText(Path.Combine(RepoRoot, "Directory.Build.props"));
        var match = Regex.Match(props, $@"<{Regex.Escape(name)}>([^<]+)</{Regex.Escape(name)}>");

        Assert.True(match.Success, $"Directory.Build.props does not define {name}.");
        return match.Groups[1].Value.Trim();
    }

    [Fact]
    public void Mapping_lists_every_supported_build()
    {
        // Append-only: lines that have shipped stay in the mapping so their tags remain
        // releasable. New rows are expected over time, so this is a floor, not an exact count.
        Assert.True(Mapping.Count >= 3, $"native-versions.tsv lists {Mapping.Count} rows; the 6.x/7.x/8.x lines must not be removed.");
    }

    [Fact]
    public void Every_mapping_row_has_a_checksum_baseline()
    {
        // FetchJars.sh refuses to download a line that has no committed SHA-256 baseline, so a
        // native-versions.tsv row without one is unreleasable. build/update-checksums.sh records
        // a baseline in one command; this test is the reminder to run it.
        var smartException = ReadProperty("SmartExceptionVersion");
        var variants = new[] { "audio", "full", "full-gpl", "https", "https-gpl", "min", "min-gpl", "video" };

        foreach (var (_, ffmpegKit) in Mapping)
        {
            var path = Path.Combine(RepoRoot, "build", "checksums", $"{ffmpegKit}.sha256");
            Assert.True(File.Exists(path), $"Missing checksum baseline {path}; run build/update-checksums.sh {ffmpegKit}.");

            var entries = File.ReadAllLines(path)
                .Where(line => line.Trim().Length > 0)
                .Select(line =>
                {
                    var match = Regex.Match(line, "^([0-9a-f]{64})  (\\S+)$");
                    Assert.True(match.Success, $"{path}: malformed line '{line}'.");
                    return match.Groups[2].Value;
                })
                .ToHashSet();

            foreach (var variant in variants)
            {
                Assert.Contains($"ffmpeg-kit-{variant}-{ffmpegKit}.aar", entries);
            }

            Assert.Contains($"smart-exception-common-{smartException}.jar", entries);
            Assert.Contains($"smart-exception-java-{smartException}.jar", entries);
        }
    }

    [Fact]
    public void Mapping_entries_are_versions_and_unique()
    {
        foreach (var (ffmpeg, ffmpegKit) in Mapping)
        {
            Assert.Matches(@"^\d+\.\d+\.\d+$", ffmpeg);
            Assert.Matches(@"^\d+\.\d+\.\d+$", ffmpegKit);
        }

        // A duplicate on either side would make the lookup ambiguous in one direction.
        Assert.Equal(Mapping.Count, Mapping.Select(m => m.FFmpeg).Distinct().Count());
        Assert.Equal(Mapping.Count, Mapping.Select(m => m.FFmpegKit).Distinct().Count());
    }

    [Fact]
    public void Repository_defaults_are_a_row_in_the_mapping()
    {
        // Directory.Build.props hardcodes one pair as the branch default. If someone bumps one
        // half without the other, the build downloads one .aar and describes a different FFmpeg.
        var ffmpeg = ReadProperty("FFmpegVersion");
        var ffmpegKit = ReadProperty("FFmpegKitNativeVersion");

        Assert.Contains((ffmpeg, ffmpegKit), Mapping);
    }

    [Fact]
    public void Package_versions_are_based_on_the_ffmpeg_version()
    {
        // The scheme is <ffmpeg version>.<binding revision>, so a package version must start with
        // an FFmpeg version from the mapping - never an FFmpegKit one.
        var ffmpegVersions = Mapping.Select(m => m.FFmpeg).ToHashSet();
        var ffmpegKitOnly = Mapping.Select(m => m.FFmpegKit).Where(v => !ffmpegVersions.Contains(v)).ToList();

        foreach (var row in Packages.Variants)
        {
            var variant = (string)row[0];
            using var package = Packages.OpenPackage(variant);
            var version = Packages.ReadNuspec(package, variant)
                .Descendants().First(e => e.Name.LocalName == "version").Value.Trim();

            var baseVersion = string.Join('.', version.Split('-')[0].Split('.').Take(3));

            Assert.False(
                ffmpegKitOnly.Contains(baseVersion),
                $"{Packages.PackageId(variant)} is versioned {version}, which is an FFmpegKit " +
                "release number rather than the FFmpeg version it contains.");
        }
    }
}
