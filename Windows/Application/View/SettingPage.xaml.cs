#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;
using KairosoftGameManager.Utility;
using Windows.ApplicationModel;
using CommunityToolkit.WinUI.Controls;

namespace KairosoftGameManager.View {

	public sealed partial class SettingPage : Page {

		#region life

		public SettingPage (
		) {
			this.InitializeComponent();
			this.Controller = new () { View = this };
			this.Controller.Initialize();
		}

		// ----------------

		private SettingPageController Controller { get; }

		#endregion

	}

	public class SettingPageController : CustomController {

		#region data

		public SettingPage View { get; init; } = default!;

		#endregion

		#region initialize

		public void Initialize (
		) {
			return;
		}

		#endregion

		#region information

		public String uVersion_Text {
			get {
				return $"Version {Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}";
			}
		}

		// ----------------

		public async void uSource_Click (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.As<Button>();
			_ = await Windows.System.Launcher.LaunchUriAsync(new ("https://github.com/twinstar6980/KairosoftGameManager"));
			return;
		}

		#endregion

		#region setting

		public async void uSettingFile_Click (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.As<SettingsCard>();
			await StorageHelper.RevealDirectory(StorageHelper.Parent(App.Setting.File).AsNotNull());
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
				return (Size)App.Setting.Data.ThemeMode;
			}
		}

		public async void uThemeMode_SelectionChanged (
			Object                    sender,
			SelectionChangedEventArgs args
		) {
			var senders = sender.As<ComboBox>();
			App.Setting.Data.ThemeMode = (ElementTheme)senders.SelectedIndex;
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
			await App.Setting.Save();
			this.NotifyPropertyChanged([
				nameof(this.uRepositoryDirectory_Text),
			]);
			return;
		}

		public String uRepositoryDirectory_Text {
			get {
				return App.Setting.Data.RepositoryDirectory;
			}
		}

		// ----------------

		public async void uProgramFileOfIl2CppDumper_LostFocus (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.As<TextBox>();
			App.Setting.Data.ProgramFileOfIl2CppDumper = StorageHelper.Regularize(senders.Text);
			await App.Setting.Save();
			this.NotifyPropertyChanged([
				nameof(this.uProgramFileOfIl2CppDumper_Text),
			]);
			return;
		}

		public String uProgramFileOfIl2CppDumper_Text {
			get {
				return App.Setting.Data.ProgramFileOfIl2CppDumper;
			}
		}

		// ----------------

		public String uTestedGameText_Text {
			get {
				return String.Join('\n', App.Setting.Data.TestedGame.Select((value) => ($"{value.Key} - {String.Join(' ', value.Value)}")));
			}
		}

		public async void uTestedGameReset_Click (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.As<Button>();
			App.Setting.Data.TestedGame = GameUtility.TestedGame;
			await App.Setting.Save();
			this.NotifyPropertyChanged([
				nameof(this.uTestedGameText_Text),
			]);
			return;
		}

		#endregion

	}

}
