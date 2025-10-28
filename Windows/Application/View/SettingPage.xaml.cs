#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;
using KairosoftGameManager.Utility;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel;
using CommunityToolkit.WinUI.Controls;

namespace KairosoftGameManager.View {

	public sealed partial class SettingPage : Page {

		#region life

		private SettingPageController Controller { get; }

		// ----------------

		public SettingPage (
		) {
			this.InitializeComponent();
			this.Controller = new () { View = this };
			this.Controller.InitializeView();
			return;
		}

		// ----------------

		protected override void OnNavigatedTo (
			NavigationEventArgs args
		) {
			ControlHelper.PostTask(this, async () => {
				await this.Controller.UpdateView();
			}).SelfLet(ExceptionHelper.WrapTask);
			base.OnNavigatedTo(args);
			return;
		}

		#endregion

	}

	public partial class SettingPageController : CustomController {

		#region data

		public SettingPage View { get; init; } = default!;

		#endregion

		#region life

		public void InitializeView (
		) {
			return;
		}

		public async Task UpdateView (
		) {
			return;
		}

		#endregion

		#region information

		public String uVersion_Text {
			get {
				return $"Version {ApplicationInformation.Version}";
			}
		}

		public String uCopyrightLabel_Text {
			get {
				return $"\u00A9 {ApplicationInformation.Year} {ApplicationInformation.Developer}. All rights reserved.";
			}
		}

		// ----------------

		public async void uSource_Click (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.As<Button>();
			await Windows.System.Launcher.LaunchUriAsync(new ("https://github.com/twinstar6980/KairosoftGameManager"));
			return;
		}

		#endregion

		#region setting

		public async void uSettingFile_Click (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.As<SettingsCard>();
			await StorageHelper.Reveal(StorageHelper.Parent(App.Setting.File).AsNotNull());
			return;
		}

		// ----------------

		public List<String> uThemeMode_ItemsSource {
			get {
				return Enum.GetValues<ElementTheme>().Select(ConvertHelper.MakeThemeToString).ToList();
			}
		}

		public Size uThemeMode_SelectedIndex {
			get {
				return App.Setting.Data.ThemeMode.CastPrimitive<Size>();
			}
		}

		public async void uThemeMode_SelectionChanged (
			Object                    sender,
			SelectionChangedEventArgs args
		) {
			var senders = sender.As<ComboBox>();
			App.Setting.Data.ThemeMode = senders.SelectedIndex.CastPrimitive<ElementTheme>();
			await App.Setting.Save();
			return;
		}

		// ----------------

		public async void uRepositoryDirectory_LostFocus (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.As<TextBox>();
			App.Setting.Data.RepositoryDirectory = StorageHelper.Regularize(senders.Text);
			this.NotifyPropertyChanged([
				nameof(this.uRepositoryDirectory_Text),
			]);
			await App.Setting.Save();
			return;
		}

		public String uRepositoryDirectory_Text {
			get {
				return App.Setting.Data.RepositoryDirectory;
			}
		}

		public async void uRepositoryDirectoryPick_Click (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.As<Button>();
			var value = await StorageHelper.PickLoadDirectory(App.MainWindow, "@RepositoryDirectory");
			if (value != null) {
				App.Setting.Data.RepositoryDirectory = value;
				this.NotifyPropertyChanged([
					nameof(this.uRepositoryDirectory_Text),
				]);
				await App.Setting.Save();
			}
			return;
		}

		// ----------------

		public async void uExternalToolOfIl2cppdumperPath_LostFocus (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.As<TextBox>();
			App.Setting.Data.ExternalTool.Il2cppdumperPath = StorageHelper.Regularize(senders.Text);
			this.NotifyPropertyChanged([
				nameof(this.uExternalToolOfIl2cppdumperPath_Text),
			]);
			await App.Setting.Save();
			return;
		}

		public String uExternalToolOfIl2cppdumperPath_Text {
			get {
				return App.Setting.Data.ExternalTool.Il2cppdumperPath;
			}
		}

		public async void uExternalToolOfIl2cppdumperPathPick_Click (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.As<Button>();
			var value = await StorageHelper.PickLoadFile(App.MainWindow, "@ExternalToolOfIl2cppdumperPath");
			if (value != null) {
				App.Setting.Data.ExternalTool.Il2cppdumperPath = value;
				this.NotifyPropertyChanged([
					nameof(this.uExternalToolOfIl2cppdumperPath_Text),
				]);
				await App.Setting.Save();
			}
			return;
		}

		// ----------------

		public async void uExternalToolOfZipalignPath_LostFocus (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.As<TextBox>();
			App.Setting.Data.ExternalTool.ZipalignPath = StorageHelper.Regularize(senders.Text);
			this.NotifyPropertyChanged([
				nameof(this.uExternalToolOfZipalignPath_Text),
			]);
			await App.Setting.Save();
			return;
		}

		public String uExternalToolOfZipalignPath_Text {
			get {
				return App.Setting.Data.ExternalTool.ZipalignPath;
			}
		}

		public async void uExternalToolOfZipalignPathPick_Click (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.As<Button>();
			var value = await StorageHelper.PickLoadFile(App.MainWindow, "@ExternalToolOfZipalignPath");
			if (value != null) {
				App.Setting.Data.ExternalTool.ZipalignPath = value;
				this.NotifyPropertyChanged([
					nameof(this.uExternalToolOfZipalignPath_Text),
				]);
				await App.Setting.Save();
			}
			return;
		}

		// ----------------

		public async void uExternalToolOfApksignerPath_LostFocus (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.As<TextBox>();
			App.Setting.Data.ExternalTool.ApksignerPath = StorageHelper.Regularize(senders.Text);
			this.NotifyPropertyChanged([
				nameof(this.uExternalToolOfApksignerPath_Text),
			]);
			await App.Setting.Save();
			return;
		}

		public String uExternalToolOfApksignerPath_Text {
			get {
				return App.Setting.Data.ExternalTool.ApksignerPath;
			}
		}

		public async void uExternalToolOfApksignerPathPick_Click (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.As<Button>();
			var value = await StorageHelper.PickLoadFile(App.MainWindow, "@ExternalToolOfApksignerPath");
			if (value != null) {
				App.Setting.Data.ExternalTool.ApksignerPath = value;
				this.NotifyPropertyChanged([
					nameof(this.uExternalToolOfApksignerPath_Text),
				]);
				await App.Setting.Save();
			}
			return;
		}

		// ----------------

		public async void uExternalToolOfApkCertificateFile_LostFocus (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.As<TextBox>();
			App.Setting.Data.ExternalTool.ApkCertificateFile = StorageHelper.Regularize(senders.Text);
			this.NotifyPropertyChanged([
				nameof(this.uExternalToolOfApkCertificateFile_Text),
			]);
			await App.Setting.Save();
			return;
		}

		public String uExternalToolOfApkCertificateFile_Text {
			get {
				return App.Setting.Data.ExternalTool.ApkCertificateFile;
			}
		}

		public async void uExternalToolOfApkCertificateFilePick_Click (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.As<Button>();
			var value = await StorageHelper.PickLoadFile(App.MainWindow, "@ExternalToolOfApkCertificateFile");
			if (value != null) {
				App.Setting.Data.ExternalTool.ApkCertificateFile = value;
				this.NotifyPropertyChanged([
					nameof(this.uExternalToolOfApkCertificateFile_Text),
				]);
				await App.Setting.Save();
			}
			return;
		}

		// ----------------

		public async void uExternalToolOfApkCertificatePassword_LostFocus (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.As<TextBox>();
			App.Setting.Data.ExternalTool.ApkCertificatePassword = StorageHelper.Regularize(senders.Text);
			this.NotifyPropertyChanged([
				nameof(this.uExternalToolOfApkCertificatePassword_Text),
			]);
			await App.Setting.Save();
			return;
		}

		public String uExternalToolOfApkCertificatePassword_Text {
			get {
				return App.Setting.Data.ExternalTool.ApkCertificatePassword;
			}
		}

		#endregion

	}

}
