#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;
using KairosoftGameManager.Utility;
using Windows.ApplicationModel;
using Microsoft.UI.Xaml.Media;

namespace KairosoftGameManager {

	public partial class App : Application {

		#region instance

		public static App Instance { get; private set; } = default!;

		public static View.MainWindow MainWindow { get; private set; } = default!;

		public static String ProgramFile { get; private set; } = default!;

		#endregion

		#region life

		public App (
		) {
			App.Instance = this;
			this.InitializeComponent();
		}

		// ----------------

		protected override async void OnLaunched (
			LaunchActivatedEventArgs args
		) {
			this.UnhandledException += this.OnUnhandledException;
			var window = default(Window);
			try {
				App.ProgramFile = StorageHelper.ToWindowsStyle(StorageHelper.Parent(Environment.GetCommandLineArgs()[0]) + "/KairosoftGameManager.exe");
				await Setting.Initialize();
				window = new View.MainWindow();
				WindowHelper.Size(window, 720, 640);
				App.MainWindow = window.AsClass<View.MainWindow>();
			}
			catch (Exception e) {
				window = new () {
					ExtendsContentIntoTitleBar = true,
					SystemBackdrop = new MicaBackdrop(),
					Content = new Control.Box() {
						Padding = new (16),
						Children = {
							new TextBlock() {
								HorizontalAlignment = HorizontalAlignment.Center,
								VerticalAlignment = VerticalAlignment.Center,
								TextWrapping = TextWrapping.Wrap,
								Text = e.ToString(),
							},
						},
					},
				};
			}
			WindowHelper.Track(window);
			WindowHelper.Title(window, Package.Current.DisplayName);
			WindowHelper.Icon(window, $"{StorageHelper.Parent(App.ProgramFile)}/Asset/Logo.ico");
			WindowHelper.Theme(window, Setting.Data.ThemeMode);
			WindowHelper.Activate(window);
			return;
		}

		// ----------------

		protected void OnUnhandledException (
			Object                                        sender,
			Microsoft.UI.Xaml.UnhandledExceptionEventArgs args
		) {
			// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
			if (App.MainWindow is not null) {
				args.Handled = true;
				try {
					_ = ControlHelper.ShowDialogSimple(App.MainWindow.Content, "Unhandled Exception", args.Exception.ToString());
				}
				catch (Exception) {
					// ignored
				}
			}
			return;
		}

		#endregion

	}

}
