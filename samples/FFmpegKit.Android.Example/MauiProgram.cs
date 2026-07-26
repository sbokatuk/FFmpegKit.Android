using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace FFmpegKit.Android.Example;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
#if NET10_0_OR_GREATER
			// MediaElement 10.0.0 made the Android foreground-service opt-in a required argument.
			// This sample previews a local file while in the foreground, and opting in would also
			// require FOREGROUND_SERVICE_MEDIA_PLAYBACK in the manifest.
			.UseMauiCommunityToolkitMediaElement(isAndroidForegroundServiceEnabled: false)
#else
			.UseMauiCommunityToolkitMediaElement()
#endif
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
