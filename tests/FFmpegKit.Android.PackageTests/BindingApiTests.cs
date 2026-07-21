namespace FFmpegKit.Android.PackageTests;

/// <summary>
/// Asserts that the binding assembly inside each package actually exposes the FFmpegKit API.
/// A binding that fails to generate still compiles and packs cleanly — it just produces an
/// almost-empty assembly — so the package layout alone is not enough to prove the build worked.
/// </summary>
public class BindingApiTests
{
    /// <summary>Types every FFmpegKit variant binds, regardless of which codecs it ships.</summary>
    private static readonly string[] CoreTypes =
    [
        "Ffmpegkit.Droid.FFmpegKit",
        "Ffmpegkit.Droid.FFmpegKitConfig",
        "Ffmpegkit.Droid.FFmpegSession",
        "Ffmpegkit.Droid.FFprobeKit",
        "Ffmpegkit.Droid.FFprobeSession",
        "Ffmpegkit.Droid.MediaInformation",
        "Ffmpegkit.Droid.MediaInformationSession",
        "Ffmpegkit.Droid.ReturnCode",
        "Ffmpegkit.Droid.Statistics",
        "Ffmpegkit.Droid.StreamInformation",
        "Ffmpegkit.Droid.Abi",
        "Ffmpegkit.Droid.Level",
        "Ffmpegkit.Droid.Log",
        "Ffmpegkit.Droid.SessionState",
    ];

    public static IEnumerable<object[]> VariantsAndFrameworks =>
        from variant in Packages.Variants.Select(row => (string)row[0])
        from tfm in Packages.ExpectedTargetFrameworks
        select new object[] { variant, tfm };

    private static AssemblyApi OpenBinding(string variant, string tfm)
    {
        using var package = Packages.OpenPackage(variant);
        var assembly = Packages.ReadEntry(package, $"lib/{tfm}/{Packages.AssemblyName(variant)}.dll");
        return new AssemblyApi(assembly);
    }

    [Theory]
    [MemberData(nameof(VariantsAndFrameworks))]
    public void Binding_exposes_the_core_ffmpegkit_types(string variant, string tfm)
    {
        using var api = OpenBinding(variant, tfm);

        var missing = CoreTypes.Except(api.PublicTypes).ToList();

        Assert.True(
            missing.Count == 0,
            $"{Packages.PackageId(variant)} ({tfm}) is missing bound types: {string.Join(", ", missing)}. " +
            $"The assembly exposes {api.PublicTypes.Count} public types in total.");
    }

    [Theory]
    [MemberData(nameof(VariantsAndFrameworks))]
    public void Binding_is_not_an_empty_shell(string variant, string tfm)
    {
        using var api = OpenBinding(variant, tfm);

        // Guards a real failure mode: an unpinned Android API level makes the binding generator
        // produce a valid but essentially empty assembly, which still packs and installs fine.
        Assert.True(
            api.PublicTypes.Count >= 30,
            $"{Packages.PackageId(variant)} ({tfm}) exposes only {api.PublicTypes.Count} public types; " +
            "the binding generator likely did not run.");
    }

    [Theory]
    [MemberData(nameof(VariantsAndFrameworks))]
    public void FFmpegKit_exposes_the_command_entry_points(string variant, string tfm)
    {
        using var api = OpenBinding(variant, tfm);

        var methods = api.MethodsOf("Ffmpegkit.Droid.FFmpegKit");

        Assert.Contains("Execute", methods);
        Assert.Contains("ExecuteAsync", methods);
        Assert.Contains("ExecuteWithArguments", methods);
    }

    [Theory]
    [MemberData(nameof(VariantsAndFrameworks))]
    public void ReturnCode_exposes_the_success_check_used_to_interpret_a_session(string variant, string tfm)
    {
        using var api = OpenBinding(variant, tfm);

        Assert.Contains("IsValueSuccess", api.PropertiesOf("Ffmpegkit.Droid.ReturnCode"));
        Assert.Contains("Value", api.PropertiesOf("Ffmpegkit.Droid.ReturnCode"));
    }

    [Theory]
    [MemberData(nameof(VariantsAndFrameworks))]
    public void Abi_getName_is_renamed_by_the_metadata_transform(string variant, string tfm)
    {
        using var api = OpenBinding(variant, tfm);

        var properties = api.PropertiesOf("Ffmpegkit.Droid.Abi");

        // Transforms/Metadata.xml renames Abi.getName to GetAbiName, which the generator then
        // surfaces as the AbiName property. Without it the name collides with Java.Lang.Enum.Name.
        Assert.Contains("AbiName", properties);
        Assert.DoesNotContain("Name", properties);
    }
}
