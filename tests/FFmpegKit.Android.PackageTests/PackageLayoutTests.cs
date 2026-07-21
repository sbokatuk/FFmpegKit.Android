using System.IO.Compression;
using System.Text.RegularExpressions;

namespace FFmpegKit.Android.PackageTests;

/// <summary>
/// Asserts the shape of the produced NuGet packages. These run against the packed .nupkg rather
/// than the build output, so they catch packaging regressions the compiler cannot see.
/// </summary>
public class PackageLayoutTests
{
    [Theory]
    [MemberData(nameof(Packages.Variants), MemberType = typeof(Packages))]
    public void Package_carries_a_binding_assembly_for_every_target_framework(string variant)
    {
        using var package = Packages.OpenPackage(variant);

        foreach (var tfm in Packages.ExpectedTargetFrameworks)
        {
            var expected = $"lib/{tfm}/{Packages.AssemblyName(variant)}.dll";
            Assert.True(
                package.GetEntry(expected) is not null,
                $"{Packages.PackageId(variant)} is missing '{expected}'.");
        }
    }

    [Theory]
    [MemberData(nameof(Packages.Variants), MemberType = typeof(Packages))]
    public void Package_carries_the_native_aar_for_every_target_framework(string variant)
    {
        using var package = Packages.OpenPackage(variant);

        foreach (var tfm in Packages.ExpectedTargetFrameworks)
        {
            // Two .aar files live here: the native FFmpegKit one and the generated project .aar
            // that carries the embedded smart-exception jars. Only the former is checked here.
            var candidates = package.Entries
                .Where(e =>
                    e.FullName.StartsWith($"lib/{tfm}/", StringComparison.Ordinal) &&
                    Path.GetFileName(e.FullName).StartsWith("ffmpeg-kit-", StringComparison.Ordinal))
                .ToList();

            var aar = Assert.Single(candidates);

            // The variant must match exactly: shipping the audio .aar in the video package would
            // still restore and install, and silently lack the expected codecs. The pattern is
            // anchored so 'full' cannot be satisfied by 'ffmpeg-kit-full-gpl-8.1.7.aar'.
            var fileName = Path.GetFileName(aar.FullName);
            Assert.Matches($@"^ffmpeg-kit-{Regex.Escape(Packages.AarName(variant))}-[0-9][0-9.]*\.aar$", fileName);

            // The native payload is tens of megabytes; anything tiny means an empty/placeholder aar.
            Assert.True(aar.Length > 1_000_000, $"'{aar.FullName}' is only {aar.Length} bytes.");
        }
    }

    [Theory]
    [MemberData(nameof(Packages.Variants), MemberType = typeof(Packages))]
    public void Package_embeds_the_smart_exception_classes_needed_at_runtime(string variant)
    {
        using var package = Packages.OpenPackage(variant);

        foreach (var tfm in Packages.ExpectedTargetFrameworks)
        {
            // FFmpegKitConfig's static initialiser calls com.arthenica.smartexception.java.Exceptions.
            // It is not in the .aar and not declared in its .pom, so if these jars stop being embedded
            // the package still builds and installs, then throws NoClassDefFoundError on the first
            // FFmpeg call. Only an on-device run catches that otherwise.
            using var projectAar = new ZipArchive(
                Packages.ReadEntry(package, $"lib/{tfm}/{Packages.AssemblyName(variant)}.aar"));

            var embeddedClasses = projectAar.Entries
                .Where(e => e.FullName.EndsWith(".jar", StringComparison.OrdinalIgnoreCase))
                .SelectMany(e =>
                {
                    var buffer = new MemoryStream();
                    using (var stream = e.Open())
                    {
                        stream.CopyTo(buffer);
                    }

                    buffer.Position = 0;
                    using var jar = new ZipArchive(buffer);
                    return jar.Entries.Select(entry => entry.FullName).ToList();
                })
                .ToList();

            Assert.Contains("com/arthenica/smartexception/java/Exceptions.class", embeddedClasses);
        }
    }

    [Theory]
    [MemberData(nameof(Packages.Variants), MemberType = typeof(Packages))]
    public void Package_declares_the_expected_nuspec_metadata(string variant)
    {
        using var package = Packages.OpenPackage(variant);
        var nuspec = Packages.ReadNuspec(package, variant);

        string Value(string name) => nuspec.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == name)?.Value.Trim() ?? string.Empty;

        Assert.Equal(Packages.PackageId(variant), Value("id"));
        Assert.NotEmpty(Value("version"));
        Assert.Equal("MIT", Value("license"));
        Assert.Equal("icon.png", Value("icon"));
        Assert.Equal("README.md", Value("readme"));
        Assert.Contains("FFmpegKit", Value("description"), StringComparison.Ordinal);

        var dependencyGroups = nuspec.Descendants()
            .Where(e => e.Name.LocalName == "group")
            .Select(e => e.Attribute("targetFramework")?.Value)
            .ToList();

        Assert.Equal(Packages.ExpectedTargetFrameworks.OrderBy(t => t), dependencyGroups.OrderBy(t => t));
    }

    [Theory]
    [MemberData(nameof(Packages.Variants), MemberType = typeof(Packages))]
    public void Package_ships_the_icon_and_readme_it_references(string variant)
    {
        using var package = Packages.OpenPackage(variant);

        Assert.True(package.GetEntry("icon.png") is not null, "icon.png is referenced but not packed.");
        Assert.True(package.GetEntry("README.md") is not null, "README.md is referenced but not packed.");
    }

    [Theory]
    [MemberData(nameof(Packages.Variants), MemberType = typeof(Packages))]
    public void Symbol_package_is_produced(string variant)
    {
        using var symbols = Packages.OpenPackage(variant, ".snupkg");

        foreach (var tfm in Packages.ExpectedTargetFrameworks)
        {
            var expected = $"lib/{tfm}/{Packages.AssemblyName(variant)}.pdb";
            Assert.True(
                symbols.GetEntry(expected) is not null,
                $"Symbol package for {Packages.PackageId(variant)} is missing '{expected}'.");
        }
    }

    [Fact]
    public void Every_variant_is_packed_with_the_same_version()
    {
        var versions = Packages.Variants
            .Select(row => (string)row[0])
            .Select(variant =>
            {
                using var package = Packages.OpenPackage(variant);
                var nuspec = Packages.ReadNuspec(package, variant);
                return nuspec.Descendants().First(e => e.Name.LocalName == "version").Value.Trim();
            })
            .Distinct()
            .ToList();

        Assert.Single(versions);
    }
}
