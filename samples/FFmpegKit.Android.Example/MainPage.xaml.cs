using CommunityToolkit.Maui.Views;
using Ffmpegkit.Droid;
// This app's own root namespace is 'FFmpegKit', which would otherwise shadow the bound type.
// An app whose namespace does not start with FFmpegKit can just write FFmpegKit.ExecuteAsync(...).
using FFmpeg = Ffmpegkit.Droid.FFmpegKit;

namespace FFmpegKit.Android.Example;

public partial class MainPage : ContentPage
{
	const string SampleAssetName = "sample.mp4";

	sealed record ConversionOption(string Name, string OutputFileName, Func<string, string, string> BuildCommand);

	static readonly ConversionOption[] ConversionOptions =
	[
		new("Resize to 160x120", "converted_resize.mp4",
			(input, output) => $"-y -i \"{input}\" -vf scale=160:120 -c:v mpeg4 -c:a aac \"{output}\""),
		new("Grayscale", "converted_grayscale.mp4",
			(input, output) => $"-y -i \"{input}\" -vf hue=s=0 -c:v mpeg4 -c:a aac \"{output}\""),
		new("Extract audio only (AAC)", "converted_audio.m4a",
			(input, output) => $"-y -i \"{input}\" -vn -c:a aac \"{output}\""),
	];

	string? _inputPath;
	TimeSpan? _sourceDuration;
	CancellationTokenSource? _cancellation;

	public MainPage()
	{
		InitializeComponent();
		ConversionPicker.ItemsSource = ConversionOptions.Select(o => o.Name).ToList();
		ConversionPicker.SelectedIndex = 0;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		if (_inputPath is not null)
			return;

		var inputPath = Path.Combine(FileSystem.CacheDirectory, SampleAssetName);

		using (var assetStream = await FileSystem.OpenAppPackageFileAsync(SampleAssetName))
		using (var fileStream = File.Create(inputPath))
		{
			await assetStream.CopyToAsync(fileStream);
		}

		_inputPath = inputPath;
		BeforePlayer.Source = MediaSource.FromFile(inputPath);

		// Typed accessor: MediaInformation.Duration is the raw FFprobe string, and parsing it
		// with the ambient culture is a silent bug on a comma-decimal locale.
		var probe = await FFprobeKit.GetMediaInformationAsync(inputPath);
		_sourceDuration = probe.MediaInformation?.DurationOrNull;

		var video = probe.MediaInformation?.Streams?.FirstOrDefault(s => s.IsVideo);
		StatusLabel.Text = video is null
			? "Tap the button to run an FFmpeg conversion."
			: $"Source: {video.PixelWidth}x{video.PixelHeight}, {_sourceDuration?.TotalSeconds:0.##}s, {video.Codec}.";
	}

	async void OnConvertClicked(object sender, EventArgs e)
	{
		if (_inputPath is null || ConversionPicker.SelectedIndex < 0)
			return;

		var option = ConversionOptions[ConversionPicker.SelectedIndex];
		var inputPath = _inputPath;

		ConvertBtn.IsEnabled = false;
		CancelBtn.IsEnabled = true;
		Spinner.IsVisible = true;
		Spinner.IsRunning = true;
		ConversionProgress.Progress = 0;
		ConversionProgress.IsVisible = true;
		ProgressLabel.IsVisible = true;
		ProgressLabel.Text = "Starting...";
		StatusLabel.Text = "Converting...";

		AfterPlayer.Stop();
		AfterPlayer.Source = null;

		_cancellation = new CancellationTokenSource();

		try
		{
			// Progress<T> marshals back to the thread that created it - the UI thread here -
			// so the handler can touch controls directly.
			var progress = new Progress<FFmpegProgress>(p =>
			{
				ConversionProgress.Progress = p.Percent ?? 0;
				ProgressLabel.Text = p.Percent is { } percent
					? $"{percent:P0} · {p.Position:mm\\:ss} · {p.Speed:0.#}x"
					: $"{p.Position:mm\\:ss} · {p.Speed:0.#}x";
			});

			var (success, message, outputPath) =
				await RunConversionAsync(option, inputPath, progress, _sourceDuration, _cancellation.Token);
			StatusLabel.Text = message;
			SemanticScreenReader.Announce(message);

			if (success)
				AfterPlayer.Source = MediaSource.FromFile(outputPath);
		}
		catch (Exception ex)
		{
			StatusLabel.Text = $"Unexpected error: {ex.Message}";
		}
		finally
		{
			_cancellation.Dispose();
			_cancellation = null;
			CancelBtn.IsEnabled = false;
			Spinner.IsRunning = false;
			Spinner.IsVisible = false;
			ConversionProgress.IsVisible = false;
			ProgressLabel.IsVisible = false;
			ConvertBtn.IsEnabled = true;
		}
	}

	void OnCancelClicked(object sender, EventArgs e)
	{
		// Cancellation is co-operative: FFmpeg stops as soon as it notices, and the awaited
		// session then completes with a cancelled return code rather than throwing.
		_cancellation?.Cancel();
		StatusLabel.Text = "Cancelling...";
	}

	static async Task<(bool Success, string Message, string OutputPath)> RunConversionAsync(
		ConversionOption option,
		string inputPath,
		IProgress<FFmpegProgress> progress,
		TimeSpan? sourceDuration,
		CancellationToken cancellationToken)
	{
		var outputPath = Path.Combine(FileSystem.CacheDirectory, option.OutputFileName);

		if (File.Exists(outputPath))
			File.Delete(outputPath);

		var command = option.BuildCommand(inputPath, outputPath);

		// Awaited directly: no Task.Run, because ExecuteAsync hands the work to FFmpegKit's own
		// executor rather than blocking a thread pool thread for the length of the transcode.
		// The duration is what lets FFmpegKit report a percentage rather than just a position.
		var session = await FFmpeg.ExecuteAsync(command, progress, sourceDuration, cancellationToken);

		if (session.Succeeded())
		{
			var outputSize = new FileInfo(outputPath).Length;
			return (true, $"Success! Converted video written to:\n{outputPath}\n({outputSize:N0} bytes)", outputPath);
		}

		if (session.ReturnCode is { IsValueCancel: true })
			return (false, "Conversion cancelled.", outputPath);

		var logs = session.FailStackTrace ?? session.AllLogsAsString;
		return (false, $"Conversion failed (return code {session.ReturnCode}).\n{logs}", outputPath);
	}
}
