#pragma warning disable 0,
// ReSharper disable MemberHidesStaticFromOuterClass

using KairosoftGameManager;
using KairosoftGameManager.Utility;
using Windows.ApplicationModel;
using Newtonsoft.Json;

namespace KairosoftGameManager {

	public class Setting {

		#region data

		[JsonObject(ItemRequired = Required.AllowNull)]
		public class SettingData {
			public required Integer                                Version;
			public required ElementTheme                           ThemeMode;
			public required String                                 RepositoryDirectory;
			public required String                                 ProgramFileOfIl2CppDumper;
			public required SortedDictionary<String, List<String>> TestedGame;
		}

		#endregion

		#region loader

		public static SettingData Data = null!;

		public static readonly String File = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "Setting.json");

		// ----------------

		public static async Task Load (
		) {
			Setting.Data = await JsonHelper.DeserializeFile<SettingData>(Setting.File);
			return;
		}

		public static async Task Save (
		) {
			WindowHelper.Current.ForEach((item) => { WindowHelper.Theme(item, Setting.Data.ThemeMode); });
			await JsonHelper.SerializeFile<SettingData>(Setting.File, Setting.Data);
			return;
		}

		public static async Task Initialize (
		) {
			try {
				await Setting.Load();
				GF.AssertTest(Setting.Data.Version == Package.Current.Id.Version.Major);
			}
			catch (Exception) {
				Setting.Data = new () {
					Version = Package.Current.Id.Version.Major,
					ThemeMode = ElementTheme.Default,
					RepositoryDirectory = "C:/Program Files (x86)/Steam",
					ProgramFileOfIl2CppDumper = "",
					TestedGame = GameUtility.TestedGame,
				};
			}
			await Setting.Save();
			return;
		}

		#endregion

	}

}
