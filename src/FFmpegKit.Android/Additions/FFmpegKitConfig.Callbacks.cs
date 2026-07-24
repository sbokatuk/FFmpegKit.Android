using System;

// The generated binding compiles with Nullable disabled and carries no annotations; these
// hand-written additions are annotation-correct, so nullable-enabled consumers get real
// nullability information for at least the surface they touch most.
#nullable enable

namespace Ffmpegkit.Droid
{
	public partial class FFmpegKitConfig
	{
		/// <summary>Routes FFmpeg log output to a delegate.</summary>
		/// <remarks>
		/// Overload of <see cref="EnableLogCallback(ILogCallback)"/> that takes a delegate, so a
		/// lambda can be used instead of a class implementing the interface. The callback runs on
		/// an FFmpegKit worker thread — marshal to the UI thread before touching UI.
		/// <para>
		/// The delegate is held by FFmpegKit until it is replaced, so anything it captures stays
		/// alive too — avoid capturing an Activity. Clear it with
		/// <see cref="DisableLogCallback"/>.
		/// </para>
		/// </remarks>
		public static void EnableLogCallback (Action<Log> logCallback)
		{
			if (logCallback is null)
				throw new ArgumentNullException (nameof (logCallback));

			EnableLogCallback (new ActionLogCallback (logCallback));
		}

		/// <summary>Routes FFmpeg progress statistics to a delegate.</summary>
		/// <remarks>
		/// Overload of <see cref="EnableStatisticsCallback(IStatisticsCallback)"/> taking a
		/// delegate. Fires frequently during a transcode; see
		/// <see cref="EnableLogCallback(Action{Log})"/> for the threading and lifetime caveats.
		/// </remarks>
		public static void EnableStatisticsCallback (Action<Statistics> statisticsCallback)
		{
			if (statisticsCallback is null)
				throw new ArgumentNullException (nameof (statisticsCallback));

			EnableStatisticsCallback (new ActionStatisticsCallback (statisticsCallback));
		}

		/// <summary>Stops routing FFmpeg log lines to a previously enabled callback.</summary>
		/// <remarks>
		/// The delegate overload above deliberately rejects null; the native way to clear the
		/// hook is passing a null interface, which nullable-enabled callers could only write as
		/// <c>EnableLogCallback ((ILogCallback) null!)</c>. This says the same thing by name.
		/// </remarks>
		public static void DisableLogCallback () => EnableLogCallback ((ILogCallback) null!);

		/// <summary>Stops routing FFmpeg statistics samples to a previously enabled callback.</summary>
		/// <remarks>See <see cref="DisableLogCallback"/>.</remarks>
		public static void DisableStatisticsCallback () => EnableStatisticsCallback ((IStatisticsCallback) null!);
	}
}
