using System.Globalization;
using System.Threading;
using Ffmpegkit.Droid;
// This assembly's own root namespace is 'FFmpegKit', which would otherwise shadow the bound type.
using FFmpeg = Ffmpegkit.Droid.FFmpegKit;

namespace FFmpegKit.Android.DeviceTests;

/// <summary>A single on-device check. Throws to fail.</summary>
/// <param name="Name">Human readable name, reported to logcat.</param>
/// <param name="Execute">Runs the check. Receives a writable working directory.</param>
public sealed record SmokeTest(string Name, Action<string> Execute);

/// <summary>
/// End-to-end checks that only mean anything on a real device: they load the native FFmpeg
/// libraries out of the .aar over JNI and run actual FFmpeg commands.
/// </summary>
public static class SmokeTests
{
    public static SmokeTest[] All =>
    [
        new("native library reports its build", ReportsItsBuild),
        new("ffmpeg -version succeeds", VersionCommandSucceeds),
        new("encodes raw frames to mp4", EncodesRawFramesToMp4),
        new("ffprobe reads back the encoded file", FFprobeReadsBackTheEncodedFile),
        new("failing command reports a non-success return code", FailingCommandIsReportedAsFailure),
        new("awaits an async execute", AsyncExecuteCompletes),
        new("awaits an async ffprobe", AsyncProbeReturnsMediaInformation),
        new("converts java enums to managed ones", ManagedEnumConversionsWork),
        new("delivers log output to a delegate", LogDelegateReceivesOutput),
        new("cancels a running command", CancellationStopsACommand),
        new("reports typed media information", TypedMediaInformationIsParsed),
        new("parses media values regardless of locale", TypedValuesIgnoreAmbientCulture),
        new("reports progress while encoding", ProgressIsReported),
        new("reports the bundled ffmpeg version", ReportsBundledFFmpegVersion),
    ];

    private static void TypedMediaInformationIsParsed(string workingDirectory)
    {
        var output = Path.Combine(workingDirectory, "output.mp4");
        Assert(File.Exists(output), "The encode check must run before this one.");

        var information = FFprobeKit.GetMediaInformationAsync(output).GetAwaiter().GetResult().MediaInformation;
        Assert(information is not null, "FFprobe returned no media information.");

        var duration = information!.DurationOrNull;
        Assert(duration is not null, $"Duration '{information.Duration}' did not parse.");
        Assert(duration!.Value > TimeSpan.Zero, $"Duration parsed as {duration}.");

        Assert(information.SizeBytes is > 0, $"SizeBytes was {information.SizeBytes?.ToString() ?? "<null>"}.");

        var video = information.Streams.FirstOrDefault(s => s.IsVideo);
        Assert(video is not null, "No stream reported IsVideo.");

        // The encode step scales to 64x64, so these are known values rather than merely non-null.
        Assert(video!.PixelWidth == 64, $"PixelWidth was {video.PixelWidth?.ToString() ?? "<null>"}.");
        Assert(video.PixelHeight == 64, $"PixelHeight was {video.PixelHeight?.ToString() ?? "<null>"}.");
        Assert(video.AverageFrameRateFps is > 0, $"AverageFrameRateFps was {video.AverageFrameRateFps?.ToString() ?? "<null>"}.");

        Report($"duration={duration} {video.PixelWidth}x{video.PixelHeight} @{video.AverageFrameRateFps:0.##}fps size={information.SizeBytes}");
    }

    private static void TypedValuesIgnoreAmbientCulture(string workingDirectory)
    {
        var output = Path.Combine(workingDirectory, "output.mp4");
        Assert(File.Exists(output), "The encode check must run before this one.");

        var information = FFprobeKit.GetMediaInformationAsync(output).GetAwaiter().GetResult().MediaInformation;
        Assert(information is not null, "FFprobe returned no media information.");

        var invariant = information!.DurationOrNull;
        var previous = CultureInfo.CurrentCulture;

        try
        {
            // The whole point of the typed accessors. Under de-DE the dot in "12.345000" reads as
            // a group separator and double.Parse returns 12,345,000; under fr-FR it throws.
            foreach (var culture in new[] { "de-DE", "fr-FR" })
            {
                CultureInfo.CurrentCulture = new CultureInfo(culture);

                var parsed = information.DurationOrNull;
                Assert(
                    parsed == invariant,
                    $"Duration parsed as {parsed} under {culture} but {invariant} under {previous.Name}.");
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }

        Report($"duration stable across locales: {invariant}");
    }

    private static void ProgressIsReported(string workingDirectory)
    {
        var input = Path.Combine(workingDirectory, "progress.raw");
        var output = Path.Combine(workingDirectory, "progress.mp4");
        WriteRawFrames(input, frameCount: 600);
        File.Delete(output);

        var samples = new List<FFmpegProgress>();
        var progress = new Progress<FFmpegProgress>(p =>
        {
            lock (samples) { samples.Add(p); }
        });

        // 600 frames at 30fps is 20 seconds of material, so percent is computable.
        var total = TimeSpan.FromSeconds(600 / 30.0);
        var session = FFmpeg.ExecuteAsync(
            $"-y -f rawvideo -pixel_format rgb24 -video_size {FrameWidth}x{FrameHeight} " +
            $"-framerate 30 -i \"{input}\" -c:v mpeg4 \"{output}\"",
            progress,
            total).GetAwaiter().GetResult();

        AssertSuccess(session, "progress encode");

        // Progress arrives on an FFmpegKit thread and Progress<T> posts it, so allow it to drain.
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            lock (samples) { if (samples.Count > 0) break; }
            Thread.Sleep(50);
        }

        FFmpegProgress[] captured;
        lock (samples) { captured = samples.ToArray(); }

        Assert(captured.Length > 0, "No progress was reported.");
        Assert(captured.All(p => p.Percent is >= 0 and <= 1), "A percent fell outside 0..1.");
        Assert(captured.Any(p => p.Position > TimeSpan.Zero), "Position never advanced.");

        var last = captured[^1];
        Report($"{captured.Length} samples, last: {last.Percent:P0} at {last.Position}, frame {last.VideoFrameNumber}, speed {last.Speed:0.##}x");
    }

    private static void ReportsBundledFFmpegVersion(string workingDirectory)
    {
        // Cross-checks the version the release notes extract from libavcodec.so at build time
        // against what the library reports at runtime.
        var version = FFmpegKitConfig.FFmpegVersion;

        Assert(!string.IsNullOrWhiteSpace(version), "FFmpegKitConfig.FFmpegVersion was empty.");
        Report($"ffmpeg={version} ffmpegkit={FFmpegKitConfig.Version} lts={FFmpegKitConfig.IsLTSBuild}");
    }

    private static void AsyncExecuteCompletes(string workingDirectory)
    {
        var input = Path.Combine(workingDirectory, "input.raw");
        var output = Path.Combine(workingDirectory, "async.mp4");
        WriteRawFrames(input);
        File.Delete(output);

        var session = FFmpeg.ExecuteAsync(BuildEncodeCommand(input, output)).GetAwaiter().GetResult();

        AssertSuccess(session, "async encode");
        Assert(session.Succeeded(), "Succeeded() disagreed with the return code.");
        Assert(File.Exists(output), $"'{output}' was not produced.");
    }

    private static void AsyncProbeReturnsMediaInformation(string workingDirectory)
    {
        var output = Path.Combine(workingDirectory, "async.mp4");
        Assert(File.Exists(output), "The async encode check must run before this one.");

        var session = FFprobeKit.GetMediaInformationAsync(output).GetAwaiter().GetResult();

        Assert(session.MediaInformation is not null, "Async FFprobe returned no media information.");
        Report($"async probe format={session.MediaInformation!.Format}");
    }

    private static void ManagedEnumConversionsWork(string workingDirectory)
    {
        var session = FFmpeg.Execute("-version");

        var state = session.State.ToManaged();
        Assert(
            state == FFmpegKitSessionState.Completed,
            $"Expected a Completed session state, got {state}.");

        // The point of the conversion: this is a switch, which a Java.Lang.Enum cannot be used in.
        var described = state switch
        {
            FFmpegKitSessionState.Completed => "completed",
            FFmpegKitSessionState.Failed => "failed",
            _ => "other",
        };

        Assert(described == "completed", $"switch produced '{described}'.");
        Assert(Level.AvLogInfo.ToManaged() == FFmpegKitLogLevel.Info, "Level.AvLogInfo did not map to Info.");
        Report($"state={state} level={Level.AvLogWarning.ToManaged()}");
    }

    private static void LogDelegateReceivesOutput(string workingDirectory)
    {
        var lines = 0;

        FFmpegKitConfig.EnableLogCallback(_ => Interlocked.Increment(ref lines));
        try
        {
            FFmpeg.Execute("-version");

            // Log callbacks arrive on FFmpegKit's own thread and can lag the Execute call that
            // produced them, so give them a moment rather than clearing the callback immediately
            // and racing the delivery.
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (Volatile.Read(ref lines) == 0 && DateTime.UtcNow < deadline)
            {
                Thread.Sleep(50);
            }
        }
        finally
        {
            FFmpegKitConfig.DisableLogCallback();
        }

        Assert(Volatile.Read(ref lines) > 0, "The log delegate never fired.");
        Report($"received {Volatile.Read(ref lines)} log lines");
    }

    private static void CancellationStopsACommand(string workingDirectory)
    {
        var input = Path.Combine(workingDirectory, "long.raw");
        var output = Path.Combine(workingDirectory, "cancelled.mp4");
        WriteRawFrames(input, frameCount: 4000);
        File.Delete(output);

        using var cancellation = new CancellationTokenSource();

        // Upscaling several thousand frames keeps FFmpeg busy long enough to cancel mid-run.
        var task = FFmpeg.ExecuteAsync(
            $"-y -f rawvideo -pixel_format rgb24 -video_size {FrameWidth}x{FrameHeight} " +
            $"-framerate 30 -i \"{input}\" -vf scale=1280:720 -c:v mpeg4 \"{output}\"",
            cancellation.Token);

        cancellation.CancelAfter(TimeSpan.FromMilliseconds(300));

        // Cancelling must complete the task rather than hang or throw...
        Assert(task.Wait(TimeSpan.FromSeconds(60)), "The cancelled command never completed.");

        // ...and must actually have stopped FFmpeg. Several thousand frames upscaled to 720p
        // cannot finish in 300ms on any device this runs on, so a success code here would mean
        // the token was ignored rather than that the work simply beat the timer.
        var returnCode = task.Result.ReturnCode;
        Assert(
            returnCode is not null && returnCode.IsValueCancel,
            $"Expected a cancelled return code, got {returnCode?.Value.ToString() ?? "<null>"}.");

        Report($"cancelled session returned {returnCode!.Value}");
    }

    private static string BuildEncodeCommand(string input, string output) =>
        $"-y -f rawvideo -pixel_format rgb24 -video_size {FrameWidth}x{FrameHeight} " +
        $"-framerate 10 -i \"{input}\" -vf scale=64:64 -c:v mpeg4 \"{output}\"";

    private static void ReportsItsBuild(string workingDirectory)
    {
        // Reaching this at all proves libffmpegkit.so loaded and the JNI bridge works.
        var packageName = Packages.PackageName;
        Assert(!string.IsNullOrWhiteSpace(packageName), "Packages.PackageName was empty.");

        var libraries = Packages.ExternalLibraries;
        Assert(libraries is { Count: > 0 }, "Packages.ExternalLibraries was empty.");

        Report($"package={packageName} externalLibraries={string.Join(",", libraries!)}");
    }

    private static void VersionCommandSucceeds(string workingDirectory)
    {
        var session = FFmpeg.Execute("-version");

        AssertSuccess(session, "-version");
        Assert(
            session.Output?.Contains("ffmpeg version", StringComparison.OrdinalIgnoreCase) == true,
            $"'-version' output did not look like FFmpeg: {session.Output}");
    }

    private static void EncodesRawFramesToMp4(string workingDirectory)
    {
        var input = Path.Combine(workingDirectory, "input.raw");
        var output = Path.Combine(workingDirectory, "output.mp4");

        WriteRawFrames(input);
        File.Delete(output);

        // rawvideo in, mpeg4 out: both are always present regardless of which FFmpegKit variant
        // is under test, and the scale filter exercises libavfilter/libswscale on the way through.
        var session = FFmpeg.Execute(
            $"-y -f rawvideo -pixel_format rgb24 -video_size {FrameWidth}x{FrameHeight} " +
            $"-framerate 10 -i \"{input}\" -vf scale=64:64 -c:v mpeg4 \"{output}\"");

        AssertSuccess(session, "encode");
        Assert(File.Exists(output), $"'{output}' was not produced.");

        var size = new FileInfo(output).Length;
        Assert(size > 0, $"'{output}' is empty.");
        Report($"encoded {size} bytes");
    }

    private static void FFprobeReadsBackTheEncodedFile(string workingDirectory)
    {
        var output = Path.Combine(workingDirectory, "output.mp4");
        Assert(File.Exists(output), "The encode check must run before this one.");

        var session = FFprobeKit.GetMediaInformation(output);
        var information = session.MediaInformation;

        Assert(information is not null, "FFprobe returned no media information.");

        var streams = information!.Streams;
        Assert(streams is { Count: > 0 }, "FFprobe reported no streams.");

        var video = streams!.FirstOrDefault(s => s.Type == "video");
        Assert(video is not null, $"No video stream found. Streams: {string.Join(",", streams.Select(s => s.Type))}");
        Report($"probed codec={video!.Codec} format={information.Format}");
    }

    private static void FailingCommandIsReportedAsFailure(string workingDirectory)
    {
        // A binding that mis-marshals return codes would make every command look successful,
        // which would quietly defeat every other check here.
        var session = FFmpeg.Execute($"-i \"{Path.Combine(workingDirectory, "does-not-exist.mp4")}\" -f null -");

        Assert(
            !session.ReturnCode.IsValueSuccess,
            "FFmpeg reported success for a command that should have failed.");
    }

    private const int FrameWidth = 32;
    private const int FrameHeight = 32;
    private const int FrameCount = 10;

    /// <summary>Writes a handful of rgb24 frames so the encode test needs no bundled media.</summary>
    private static void WriteRawFrames(string path, int frameCount = FrameCount)
    {
        var frame = new byte[FrameWidth * FrameHeight * 3];
        using var stream = File.Create(path);

        for (var i = 0; i < frameCount; i++)
        {
            for (var pixel = 0; pixel < frame.Length; pixel += 3)
            {
                frame[pixel] = (byte)(i * 25);
                frame[pixel + 1] = (byte)(pixel % 256);
                frame[pixel + 2] = (byte)((pixel + i) % 256);
            }

            stream.Write(frame);
        }
    }

    private static void AssertSuccess(AbstractSession session, string what)
    {
        Assert(
            session.ReturnCode is not null && session.ReturnCode.IsValueSuccess,
            $"'{what}' failed with return code {session.ReturnCode?.Value.ToString() ?? "<null>"}. " +
            $"Logs:\n{session.AllLogsAsString}");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new SmokeTestFailure(message);
        }
    }

    private static void Report(string message) => Reporter?.Invoke(message);

    /// <summary>Set by the host activity so checks can surface detail to logcat.</summary>
    public static Action<string>? Reporter { get; set; }
}

public sealed class SmokeTestFailure(string message) : Exception(message);
