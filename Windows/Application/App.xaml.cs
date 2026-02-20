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

		public static SettingProvider Setting { get; private set; } = default!;

		public static View.MainWindow MainWindow { get; private set; } = default!;

		public static String PackageDirectory { get; private set; } = default!;

		public static String ProgramFile { get; private set; } = default!;

		public static String SharedDirectory { get; private set; } = default!;

		public static String CacheDirectory { get; private set; } = default!;

		// ----------------

		public static Boolean MainWindowIsInitialized {
			get {
				// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
				return App.MainWindow != null;
			}
		}

		#endregion

		#region life

		public App (
		) {
			App.Instance = this;
			App.Setting = new ();
			this.InitializeComponent();
			return;
		}

		// ----------------

		protected override async void OnLaunched (
			LaunchActivatedEventArgs args
		) {
			try {
				ExceptionHelper.Initialize(this);
				ExceptionHelper.Listen(async (exception) => {
					_ = this.HandleException(exception, App.MainWindow);
					return;
				});
				App.PackageDirectory = StorageHelper.Regularize(Package.Current.InstalledPath);
				App.ProgramFile = $"{App.PackageDirectory}/Application.exe";
				App.SharedDirectory = StorageHelper.Regularize(Windows.Storage.ApplicationData.Current.LocalFolder.Path);
				App.CacheDirectory = $"{App.SharedDirectory}/cache";
				try {
					await App.Setting.Load();
				}
				catch (Exception) {
				}
				await App.Setting.Save(apply: false);
				App.MainWindow = new ();
				WindowHelper.SetSize(App.MainWindow, 720, 640);
				await App.Setting.Apply();
				WindowHelper.Activate(App.MainWindow);
			}
			catch (Exception e) {
				_ = this.HandleExceptionFatal(e);
			}
			return;
		}

		#endregion

		#region utility

		private async Task HandleException (
			Exception exception,
			Window?   window
		) {
			try {
				if (window != null) {
					await window.DispatcherQueue.EnqueueAsync(async () => {
						await ControlHelper.PostTask(window.Content.As<FrameworkElement>(), async () => {
							await ControlHelper.ShowDialogAsAutomatic(window.Content.As<FrameworkElement>(), "Unhandled Exception", new TextBlock() {
								HorizontalAlignment = HorizontalAlignment.Stretch,
								VerticalAlignment = VerticalAlignment.Stretch,
								Style = window.Content.As<FrameworkElement>().FindResource("BodyTextBlockStyle").As<Style>(),
								IsTextSelectionEnabled = true,
								TextWrapping = TextWrapping.Wrap,
								Text = ExceptionHelper.GenerateMessage(exception),
							}, null);
						});
					});
				}
			}
			catch (Exception) {
				// ignored
			}
			return;
		}

		private async Task HandleExceptionFatal (
			Exception exception
		) {
			try {
				var window = new Window() {
					SystemBackdrop = new MicaBackdrop(),
					Content = new Grid(),
				};
				window.Closed += async (_, _) => {
					// if the user close the window externally, the dialog task will not complete, so put the step to close MainWindow in the Closed event
					if (App.MainWindowIsInitialized) {
						WindowHelper.Close(App.MainWindow);
					}
					return;
				};
				WindowHelper.SetIcon(window, $"{App.PackageDirectory}/Asset/Logo.ico");
				WindowHelper.SetTitle(window, ApplicationInformation.Name);
				WindowHelper.SetTitleBar(window, true, null, false);
				WindowHelper.Activate(window);
				await this.HandleException(exception, window);
				WindowHelper.Close(window);
			}
			catch (Exception) {
				// ignored
			}
			return;
		}

		#endregion

	}

}
