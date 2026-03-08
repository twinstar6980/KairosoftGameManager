#pragma warning disable 0,
// ReSharper disable MemberHidesStaticFromOuterClass

using KairosoftGameManager;
using KairosoftGameManager.Utility;
using Microsoft.UI;

namespace KairosoftGameManager {

	public record SettingData {
		public String                     Version                      { get; set; } = default!;
		public ElementTheme               ThemeMode                    { get; set; } = default!;
		public Dictionary<String, String> StoragePickerHistoryLocation { get; set; } = default!;
		public List<String>               RepositoryDirectory          { get; set; } = default!;
		public ExternalToolSetting        ExternalTool                 { get; set; } = default!;
	}

	public record SettingState {
		public ElementTheme? ThemeMode                  { get; set; } = default!;
		public List<String>  CurrentRepositoryDirectory { get; set; } = default!;
	}

	public class SettingProvider {

		#region constructor

		public SettingData Data { get; private set; }

		public SettingState State { get; private set; }

		// ----------------

		public SettingProvider(
		) {
			this.Data = SettingProvider.CreateDefaultData();
			this.State = SettingProvider.CreateDefaultState();
			return;
		}

		#endregion

		#region action

		public async Task Reset(
		) {
			this.Data = SettingProvider.CreateDefaultData();
			this.State = SettingProvider.CreateDefaultState();
			return;
		}

		public async Task Apply(
		) {
			// ThemeMode
			if (this.State.ThemeMode != this.Data.ThemeMode && App.Instance.MainWindowIsInitialized) {
				App.Instance.MainWindow.Content.As<FrameworkElement>().RequestedTheme = this.Data.ThemeMode;
				App.Instance.MainWindow.AppWindow.TitleBar.ButtonForegroundColor = this.Data.ThemeMode switch {
					ElementTheme.Default => null,
					ElementTheme.Light   => Colors.Black,
					ElementTheme.Dark    => Colors.White,
					_                    => throw new UnreachableException(),
				};
				await ControlHelper.IterateDialog(async (it) => {
					it.RequestedTheme = App.Instance.MainWindow.Content.As<FrameworkElement>().RequestedTheme;
				});
				this.State.ThemeMode = this.Data.ThemeMode;
			}
			return;
		}

		#endregion

		#region storage

		public String File {
			get {
				return $"{App.Instance.SharedDirectory}/setting.json";
			}
		}

		// ----------------

		public async Task Load(
			String? file = null
		) {
			file ??= this.File;
			this.Data = (await JsonHelper.DeserializeFile<SettingData>(file)).SelfAlso((it) => AssertTest(it.Version == ApplicationInformation.VersionMainly));
			return;
		}

		public async Task Save(
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

		private static SettingData CreateDefaultData(
		) {
			return new () {
				Version = ApplicationInformation.VersionMainly,
				ThemeMode = ElementTheme.Default,
				StoragePickerHistoryLocation = [],
				RepositoryDirectory = [
					"C:/Program Files (x86)/Steam",
				],
				ExternalTool = new () {
					Il2cppdumperPath = "Il2CppDumper",
					ZipalignPath = "zipalign",
					ApksignerPath = "apksigner",
					ApkCertificateFile = "",
					ApkCertificatePassword = "",
				},
			};
		}

		private static SettingState CreateDefaultState(
		) {
			return new () {
				ThemeMode = null,
				CurrentRepositoryDirectory = [],
			};
		}

		#endregion

	}

}
