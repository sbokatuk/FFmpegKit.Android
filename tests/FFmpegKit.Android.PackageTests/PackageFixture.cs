using System.IO.Compression;
using System.Xml.Linq;

namespace FFmpegKit.Android.PackageTests;

/// <summary>
/// Locates the packed .nupkg files and exposes the variants under test.
/// </summary>
public static class Packages
{
    /// <summary>Every FFmpegKit variant this repository builds.</summary>
    public static readonly string[] AllVariants =
    [
        "Audio", "Full", "FullGpl", "Https", "HttpsGpl", "Min", "MinGpl", "Video",
    ];

    /// <summary>Target frameworks each package must carry a binding assembly for.</summary>
    public static readonly string[] ExpectedTargetFrameworks =
    [
        "net8.0-android34.0", "net9.0-android35.0", "net10.0-android36.0",
    ];

    public static string ArtifactsDirectory { get; } = ResolveArtifactsDirectory();

    /// <summary>
    /// Variants expected to be present. Defaults to all eight; narrow it with
    /// FFMPEGKIT_VARIANTS=Video when iterating locally on a single variant.
    /// </summary>
    public static IEnumerable<object[]> Variants =>
        (Environment.GetEnvironmentVariable("FFMPEGKIT_VARIANTS") is { Length: > 0 } filter
            ? filter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : AllVariants)
        .Select(v => new object[] { v });

    public static string PackageId(string variant) => $"Xamarin.FFmpegKit.{variant}.Android";

    /// <summary>
    /// Upstream ships the -gpl variants under GPL-3.0 and the rest under LGPL-3.0. Getting this
    /// wrong would misrepresent the obligations a consumer takes on, so it is asserted per variant.
    /// </summary>
    public static bool IsGpl(string variant) => variant.EndsWith("Gpl", StringComparison.Ordinal);

    public static string NativeLicense(string variant) => IsGpl(variant) ? "GPL-3.0-only" : "LGPL-3.0-only";

    public static string LicenseExpression(string variant) => $"MIT AND {NativeLicense(variant)}";

    public static string AssemblyName(string variant) => $"FFmpegKit.{variant}.Android";

    /// <summary>FullGpl -> full-gpl, HttpsGpl -> https-gpl, Video -> video, ...</summary>
    public static string AarName(string variant) => variant.ToLowerInvariant().Replace("gpl", "-gpl");

    public static string FindPackage(string variant, string extension = ".nupkg")
    {
        var id = PackageId(variant);
        var matches = Directory.Exists(ArtifactsDirectory)
            ? Directory.GetFiles(ArtifactsDirectory, $"{id}.*{extension}")
                .Where(f => !f.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase))
                .ToArray()
            : [];

        Assert.True(
            matches.Length > 0,
            $"No {id}*{extension} found in '{ArtifactsDirectory}'. " +
            "Run FFmpegKit.Android/BuildNugets.sh (or the CI pack step) first.");

        // A rebuilt working copy can leave several versions behind; test the newest.
        return matches.OrderByDescending(File.GetLastWriteTimeUtc).First();
    }

    public static ZipArchive OpenPackage(string variant, string extension = ".nupkg") =>
        ZipFile.OpenRead(FindPackage(variant, extension));

    public static XDocument ReadNuspec(ZipArchive package, string variant)
    {
        var entry = package.GetEntry($"{PackageId(variant)}.nuspec");
        Assert.NotNull(entry);

        using var stream = entry!.Open();
        return XDocument.Load(stream);
    }

    /// <summary>Reads a package entry fully into memory so it can be seeked.</summary>
    public static MemoryStream ReadEntry(ZipArchive package, string entryName)
    {
        var entry = package.GetEntry(entryName);
        Assert.True(entry is not null, $"Package has no entry '{entryName}'.");

        var buffer = new MemoryStream();
        using (var stream = entry!.Open())
        {
            stream.CopyTo(buffer);
        }

        buffer.Position = 0;
        return buffer;
    }

    private static string ResolveArtifactsDirectory()
    {
        if (Environment.GetEnvironmentVariable("FFMPEGKIT_ARTIFACTS") is { Length: > 0 } configured)
        {
            return Path.GetFullPath(configured);
        }

        // Walk up to the repository root (the directory holding global.json).
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "global.json")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(directory?.FullName ?? AppContext.BaseDirectory, "artifacts");
    }
}
