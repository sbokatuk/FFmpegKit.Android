using CommunityToolkit.Maui.Views;

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
	}

	async void OnConvertClicked(object sender, EventArgs e)
	{
		if (_inputPath is null || ConversionPicker.SelectedIndex < 0)
			return;

		var option = ConversionOptions[ConversionPicker.SelectedIndex];
		var inputPath = _inputPath;

		ConvertBtn.IsEnabled = false;
		Spinner.IsVisible = true;
		Spinner.IsRunning = true;
		StatusLabel.Text = "Converting...";

		AfterPlayer.Stop();
		AfterPlayer.Source = null;

		try
		{
			var (success, message, outputPath) = await Task.Run(() => RunConversion(option, inputPath));
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
			Spinner.IsRunning = false;
			Spinner.IsVisible = false;
			ConvertBtn.IsEnabled = true;
		}
	}

	static (bool Success, string Message, string OutputPath) RunConversion(ConversionOption option, string inputPath)
	{
		var outputPath = Path.Combine(FileSystem.CacheDirectory, option.OutputFileName);

		if (File.Exists(outputPath))
			File.Delete(outputPath);

		var command = option.BuildCommand(inputPath, outputPath);
		var session = Ffmpegkit.Droid.FFmpegKit.Execute(command);

		if (Ffmpegkit.Droid.ReturnCode.IsSuccess(session.ReturnCode))
		{
			var outputSize = new FileInfo(outputPath).Length;
			return (true, $"Success! Converted video written to:\n{outputPath}\n({outputSize:N0} bytes)", outputPath);
		}

		var logs = session.FailStackTrace ?? session.AllLogsAsString;
		return (false, $"Conversion failed (return code {session.ReturnCode}).\n{logs}", outputPath);
	}
}
