#pragma warning disable 0,

using KairosoftGameManager;
using KairosoftGameManager.Utility;
using Microsoft.UI.Xaml.Media;

namespace KairosoftGameManager {

	public partial class App : Application {

		#region singleton

		public static App Instance { get; private set; } = default!;

		#endregion

		#region life

		public StoragePath PackageDirectory { get; private set; }

		public StoragePath ProgramFile { get; private set; }

		public StoragePath SharedDirectory { get; private set; }

		public StoragePath CacheDirectory { get; private set; }

		public SettingProvider Setting { get; }

		public View.MainWindow MainWindow { get; private set; }

		public Boolean MainWindowIsInitialized {
			get {
				// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
				return this.MainWindow != null;
			}
		}

		// ----------------

		public App(
		) {
			// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
			AssertTest(App.Instance == null);
			App.Instance = this;
			this.PackageDirectory = null!;
			this.ProgramFile = null!;
			this.SharedDirectory = null!;
			this.CacheDirectory = null!;
			this.Setting = new ();
			this.MainWindow = null!;
			this.InitializeComponent();
			return;
		}

		// ----------------

		protected override async void OnLaunched(
			LaunchActivatedEventArgs args
		) {
			try {
				this.PackageDirectory = await StorageHelper.Query(StorageQueryType.ApplicationPackage);
				this.ProgramFile = this.PackageDirectory.Join("Application.exe");
				this.SharedDirectory = await StorageHelper.Query(StorageQueryType.ApplicationPackagedShared);
				this.CacheDirectory = await StorageHelper.Query(StorageQueryType.ApplicationPackagedCache);
				await ApplicationExceptionManager.Instance.Initialize(this);
				await ApplicationExceptionManager.Instance.Listen(async (exception) => {
					await this.HandleException(exception, this.MainWindow);
					return;
				});
				try {
					await this.Setting.Load();
				}
				catch (Exception) {
					// ignored
				}
				await this.Setting.Save(apply: false);
				this.MainWindow = new ();
				WindowHelper.SetSize(this.MainWindow, 720, 640);
				await this.Setting.Apply();
				WindowHelper.Activate(this.MainWindow);
			}
			catch (Exception e) {
				_ = this.HandleExceptionFatal(e);
			}
			return;
		}

		#endregion

		#region utility

		private async Task HandleException(
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
								Text = ConvertHelper.GenerateExceptionMessage(exception),
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

		private async Task HandleExceptionFatal(
			Exception exception
		) {
			try {
				var window = new Window() {
					SystemBackdrop = new MicaBackdrop(),
					Content = new Grid(),
				};
				window.Closed += async (_, _) => {
					// if the user close the window externally, the dialog task will not complete, so put the step to close MainWindow in the Closed event
					if (this.MainWindowIsInitialized) {
						WindowHelper.Close(this.MainWindow);
					}
					return;
				};
				WindowHelper.SetIcon(window, $"{this.PackageDirectory}/Asset/Logo.ico");
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
