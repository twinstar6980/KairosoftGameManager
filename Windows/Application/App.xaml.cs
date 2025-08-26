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
		}

		// ----------------

		protected override async void OnLaunched (
			LaunchActivatedEventArgs args
		) {
			this.UnhandledException += this.OnUnhandledException;
			TaskScheduler.UnobservedTaskException += this.OnUnobservedTaskException;
			var window = default(Window);
			try {
				App.PackageDirectory = StorageHelper.Parent(Environment.GetCommandLineArgs()[0]).AsNotNull();
				App.ProgramFile = $"{App.PackageDirectory}/Application.exe";
				App.SharedDirectory = StorageHelper.Regularize(Windows.Storage.ApplicationData.Current.LocalFolder.Path);
				App.CacheDirectory = $"{App.SharedDirectory}/Cache";
				try {
					await App.Setting.Load();
				}
				catch (Exception) {
					await App.Setting.Reset();
				}
				await App.Setting.Save();
				window = new View.MainWindow();
				WindowHelper.Size(window, 720, 640);
				App.MainWindow = window.As<View.MainWindow>();
				await App.Setting.Apply();
			}
			catch (Exception e) {
				window = new () {
					ExtendsContentIntoTitleBar = true,
					SystemBackdrop = new MicaBackdrop(),
					Content = new Control.Box() {
						RequestedTheme = ElementTheme.Default,
						Padding = new (16),
						Children = {
							new TextBlock() {
								HorizontalAlignment = HorizontalAlignment.Center,
								VerticalAlignment = VerticalAlignment.Center,
								IsTextSelectionEnabled = true,
								TextWrapping = TextWrapping.Wrap,
								Text = GF.GenerateExceptionMessage(e),
							},
						},
					},
				};
			}
			WindowHelper.Title(window, Package.Current.DisplayName);
			WindowHelper.Icon(window, $"{App.PackageDirectory}/Asset/Logo.ico");
			WindowHelper.Activate(window);
			return;
		}

		// ----------------

		private void OnUnhandledException (
			Object                                        sender,
			Microsoft.UI.Xaml.UnhandledExceptionEventArgs args
		) {
			args.Handled = true;
			this.HandleException(args.Exception);
			return;
		}

		private void OnUnobservedTaskException (
			Object?                          sender,
			UnobservedTaskExceptionEventArgs args
		) {
			args.SetObserved();
			this.HandleException(args.Exception);
			return;
		}

		#endregion

		#region utility

		private void HandleException (
			Exception exception
		) {
			if (App.MainWindowIsInitialized) {
				App.MainWindow.DispatcherQueue.EnqueueAsync(() => {
					try {
						_ = ControlHelper.ShowDialogAsAutomatic(App.MainWindow.Content, "Unhandled Exception", new TextBlock() {
							HorizontalAlignment = HorizontalAlignment.Stretch,
							VerticalAlignment = VerticalAlignment.Stretch,
							IsTextSelectionEnabled = true,
							TextWrapping = TextWrapping.Wrap,
							Text = GF.GenerateExceptionMessage(exception),
						}, null);
					}
					catch (Exception) {
						// ignored
					}
				});
			}
			return;
		}

		public Task WithTaskExceptionHandler (
			Task task
		) {
			return task.ContinueWith((it) => {
				if (task.Exception?.InnerException != null) {
					this.HandleException(task.Exception.InnerException);
				}
				return;
			});
		}

		#endregion

	}

}
