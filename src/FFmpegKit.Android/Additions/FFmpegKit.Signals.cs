#nullable enable

namespace Ffmpegkit.Droid
{
	// Mono on Android restarts the threads it stopped for a garbage collection with SIGXCPU, and
	// FFmpeg's term_init installs its own SIGXCPU handler the first time a command runs. From then
	// on the restart never reaches Mono: the next collection leaves every managed thread suspended
	// for good, which an app sees as an ANR, and FFmpeg reads the same signal as a request to stop.
	// Nothing fails until a collection happens to run, so a test app that allocates little passes
	// while a real one freezes.
	//
	// FFmpegKitConfig.ignoreSignal is upstream's opt-out, and it has to come before the first
	// command. Static constructors on the types that can start one do that without the consumer
	// having to know. A module initializer would be simpler, but it would load the native libraries
	// as soon as this assembly loads, which breaks apps that gate FFmpegKit at runtime on devices
	// it cannot run on. FFprobe installs no handlers, so FFprobeKit needs none of this.
	static class MonoRestartSignal
	{
		internal static void KeepFromFFmpeg ()
		{
			try {
				FFmpegKitConfig.IgnoreSignal (Signal.Sigxcpu);
			} catch (Java.Lang.Throwable) {
				// The native library did not load, say on a 32-bit device. Whatever the app calls
				// next reports that as it always has; thrown from here it would instead become a
				// TypeInitializationException on every later use of the type.
			}
		}
	}

	public partial class FFmpegKit
	{
		static FFmpegKit () => MonoRestartSignal.KeepFromFFmpeg ();
	}

	public partial class FFmpegKitConfig
	{
		static FFmpegKitConfig () => MonoRestartSignal.KeepFromFFmpeg ();
	}

	public partial class AsyncFFmpegExecuteTask
	{
		static AsyncFFmpegExecuteTask () => MonoRestartSignal.KeepFromFFmpeg ();
	}
}
