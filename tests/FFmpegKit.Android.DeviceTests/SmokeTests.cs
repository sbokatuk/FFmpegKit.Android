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
    ];

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
            FFmpegKitConfig.EnableLogCallback((ILogCallback)null!);
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
