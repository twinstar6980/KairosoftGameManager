#pragma warning disable 0,
// ReSharper disable MemberHidesStaticFromOuterClass

using KairosoftGameManager;
using KairosoftGameManager.Utility;
using Windows.ApplicationModel;
using Microsoft.UI;

namespace KairosoftGameManager {

	public record SettingData {
		public Integer                                Version                      = default!;
		public ElementTheme                           ThemeMode                    = default!;
		public Dictionary<String, String>             StoragePickerHistoryLocation = default!;
		public String                                 RepositoryDirectory          = default!;
		public String                                 ProgramFileOfIl2CppDumper    = default!;
		public SortedDictionary<String, List<String>> TestedGame                   = default!;
	}

	public record SettingState {
		public ElementTheme? ThemeMode = default!;
	}

	public class SettingProvider {

		#region structor

		public SettingData Data;

		public SettingState State;

		// ----------------

		public SettingProvider (
		) {
			this.Data = SettingProvider.CreateDefaultData();
			this.State = SettingProvider.CreateDefaultState();
		}

		#endregion

		#region action

		public async Task Reset (
		) {
			this.Data = SettingProvider.CreateDefaultData();
			this.State = SettingProvider.CreateDefaultState();
			return;
		}

		public async Task Apply (
		) {
			// ThemeMode
			if (this.State.ThemeMode != this.Data.ThemeMode && App.MainWindowIsInitialized) {
				App.MainWindow.Content.As<FrameworkElement>().RequestedTheme = this.Data.ThemeMode;
				App.MainWindow.AppWindow.TitleBar.ButtonForegroundColor = this.Data.ThemeMode switch {
					ElementTheme.Default => null,
					ElementTheme.Light   => Colors.Black,
					ElementTheme.Dark    => Colors.White,
					_                    => throw new (),
				};
				await ControlHelper.IterateDialog(async (it) => {
					it.RequestedTheme = App.MainWindow.Content.As<FrameworkElement>().RequestedTheme;
				});
				this.State.ThemeMode = this.Data.ThemeMode;
			}
			return;
		}

		#endregion

		#region storage

		public String File {
			get {
				return StorageHelper.ToWindowsStyle($"{App.SharedDirectory}/Setting.json");
			}
		}

		// ----------------

		public async Task Load (
			String? file = null
		) {
			file ??= this.File;
			this.Data = await JsonHelper.DeserializeFile<SettingData>(file);
			GF.AssertTest(this.Data.Version == Package.Current.Id.Version.Major);
			return;
		}

		public async Task Save (
			String? file  = null,
			Boolean apply = true
		) {
			file ??= this.File;
			if (apply) {
				await this.Apply();
			}
			await JsonHelper.SerializeFile<SettingData>(file, this.Data);
			return;
		}

		#endregion

		#region utility

		private static SettingData CreateDefaultData (
		) {
			return new () {
				Version = Package.Current.Id.Version.Major,
				ThemeMode = ElementTheme.Default,
				StoragePickerHistoryLocation = [],
				RepositoryDirectory = "C:/Program Files (x86)/Steam",
				ProgramFileOfIl2CppDumper = "",
				TestedGame = GameUtility.TestedGame,
			};
		}

		private static SettingState CreateDefaultState (
		) {
			return new () {
				ThemeMode = null,
			};
		}

		#endregion

	}

}
