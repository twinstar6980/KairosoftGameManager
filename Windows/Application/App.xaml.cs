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
			var window = default(Window);
			try {
				ExceptionHelper.Initialize(this, this.HandleException);
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
				App.MainWindow = new ();
				WindowHelper.SetSize(App.MainWindow, 720, 640);
				await App.Setting.Apply();
				window = App.MainWindow;
			}
			catch (Exception e) {
				window = new Window() {
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
								Text = ExceptionHelper.GenerateMessage(e),
							},
						},
					},
				}.SelfAlso((it) => {
					WindowHelper.SetTitleBar(it, true, null, false);
					it.Closed += (_, _) => {
						if (App.MainWindowIsInitialized) {
							WindowHelper.Close(App.MainWindow);
						}
						return;
					};
				});
			}
			WindowHelper.SetIcon(window, $"{App.PackageDirectory}/Asset/Logo.ico");
			WindowHelper.SetTitle(window, Package.Current.DisplayName);
			WindowHelper.Activate(window);
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
							Text = ExceptionHelper.GenerateMessage(exception),
						}, null);
					}
					catch (Exception) {
						// ignored
					}
				});
			}
			return;
		}

		#endregion

	}

}
