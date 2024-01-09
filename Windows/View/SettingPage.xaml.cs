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
			var senders = sender.AsClass<Button>();
			_ = await Windows.System.Launcher.LaunchUriAsync(new ("https://github.com/twinkles-twinstar/KairosoftGameManager"));
			return;
		}

		#endregion

		#region setting

		public async void uSettingFile_Click (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.AsClass<SettingsCard>();
			await StorageHelper.RevealDirectory(StorageHelper.Parent(Setting.File).AsNotNull());
			return;
		}

		// ----------------

		public List<String> uThemeMode_ItemsSource {
			get {
				return Enum.GetValues<ElementTheme>().Select(ConvertHelper.ThemeToString).ToList();
			}
		}

		public Size uThemeMode_SelectedIndex {
			get {
				return (Size)Setting.Data.ThemeMode;
			}
		}

		public async void uThemeMode_SelectionChanged (
			Object                    sender,
			SelectionChangedEventArgs args
		) {
			var senders = sender.AsClass<ComboBox>();
			Setting.Data.ThemeMode = (ElementTheme)senders.SelectedIndex;
			await Setting.Save();
			return;
		}

		// ----------------

		public String uRepositoryDirectory_Text {
			get {
				return Setting.Data.RepositoryDirectory;
			}
		}

		public async void uRepositoryDirectory_LostFocus (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.AsClass<TextBox>();
			Setting.Data.RepositoryDirectory = StorageHelper.Regularize(senders.Text);
			await Setting.Save();
			this.NotifyPropertyChanged(
				nameof(this.uRepositoryDirectory_Text)
			);
			return;
		}

		// ----------------

		public String uProgramFileOfIl2CppDumper_Text {
			get {
				return Setting.Data.ProgramFileOfIl2CppDumper;
			}
		}

		public async void uProgramFileOfIl2CppDumper_LostFocus (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.AsClass<TextBox>();
			Setting.Data.ProgramFileOfIl2CppDumper = StorageHelper.Regularize(senders.Text);
			await Setting.Save();
			this.NotifyPropertyChanged(
				nameof(this.uProgramFileOfIl2CppDumper_Text)
			);
			return;
		}

		// ----------------

		public String uTestedGameText_Text {
			get {
				return String.Join('\n', Setting.Data.TestedGame.Select((value) => ($"{value.Key} - {String.Join(' ', value.Value)}")));
			}
		}

		public async void uTestedGameReset_Click (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.AsClass<Button>();
			Setting.Data.TestedGame = GameUtility.TestedGame;
			await Setting.Save();
			this.NotifyPropertyChanged(
				nameof(this.uTestedGameText_Text)
			);
			return;
		}

		#endregion

	}

}
