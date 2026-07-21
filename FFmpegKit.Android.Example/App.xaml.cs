namespace FFmpegKit.Android.Example;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState) =>
		new(new AppShell());
}
