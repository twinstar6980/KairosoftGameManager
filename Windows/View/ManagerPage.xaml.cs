#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;
using KairosoftGameManager.Utility;
using Microsoft.UI.Xaml.Media;

namespace KairosoftGameManager.View {

	public sealed partial class ManagerPage : Page {

		#region life

		public ManagerPage (
		) {
			this.InitializeComponent();
			this.Controller = new () { View = this };
			this.Controller.Initialize();
		}

		// ----------------

		private ManagerPageController Controller { get; }

		#endregion

	}

	public class ManagerPageController : CustomController {

		#region data

		public ManagerPage View { get; init; } = default!;

		#endregion

		#region initialize

		public void Initialize (
		) {
			var task = async () => {
				await ControlHelper.WaitUntilLoaded(this.View);
				await this.LoadRepository();
				return;
			};
			task();
			return;
		}

		// ----------------

		public async Task LoadRepository (
		) {
			this.uGameList_ItemsSource.Clear();
			this.NotifyPropertyChanged(
				nameof(this.uRepositoryDirectoryCount_Value)
			);
			var closeDialog = await ControlHelper.ShowDialogForWaiting(this.View, null, null);
			try {
				GF.AssertTest(await GameUtility.CheckRepositoryDirectory(Setting.Data.RepositoryDirectory));
				var gameConfigurationList = new List<GameConfiguration>();
				foreach (var gameIdentity in await GameUtility.ListGameInRepository(Setting.Data.RepositoryDirectory)) {
					var gameConfiguration = await GameUtility.LoadGameConfiguration(Setting.Data.RepositoryDirectory, gameIdentity);
					if (gameConfiguration is not null) {
						gameConfigurationList.Add(gameConfiguration);
					}
				}
				gameConfigurationList.Sort((left, right) => (String.CompareOrdinal(left.Name, right.Name)));
				foreach (var gameConfiguration in gameConfigurationList) {
					this.uGameList_ItemsSource.Add(new () { Host = this, Configuration = gameConfiguration });
				}
			}
			catch (Exception e) {
				App.MainWindow.PublishTip(InfoBarSeverity.Error, "Failed to load repository.", e.ToString());
			}
			await closeDialog();
			this.NotifyPropertyChanged(
				nameof(this.uRepositoryDirectoryCount_Value)
			);
			return;
		}

		public async Task ReloadGame (
			ManagerPageGameItemController gameController
		) {
			gameController.Configuration = (await GameUtility.LoadGameConfiguration(Setting.Data.RepositoryDirectory, gameController.Configuration.Identity)).AsNotNull();
			gameController.NotifyPropertyChanged(
				nameof(gameController.uIcon_Source),
				nameof(gameController.uName_Text),
				nameof(gameController.uIdentity_ToolTip),
				nameof(gameController.uIdentityBadge_Style),
				nameof(gameController.uIdentityText_Text),
				nameof(gameController.uVersion_ToolTip),
				nameof(gameController.uVersionBadge_Style),
				nameof(gameController.uVersionText_Text),
				nameof(gameController.uRecord_ToolTip),
				nameof(gameController.uRecordBadge_Style),
				nameof(gameController.uRecordText_Text),
				nameof(gameController.uProgram_ToolTip),
				nameof(gameController.uProgramBadge_Style),
				nameof(gameController.uProgramText_Text),
				nameof(gameController.uActionRestoreProgram_IsEnabled),
				nameof(gameController.uActionModifyProgram_IsEnabled),
				nameof(gameController.uActionEncryptRecord_IsEnabled),
				nameof(gameController.uActionDecryptRecord_IsEnabled),
				nameof(gameController.uActionImportRecord_IsEnabled),
				nameof(gameController.uActionExportRecord_IsEnabled)
			);
			return;
		}

		public async Task<Boolean?> ActionGame (
			ManagerPageGameItemController gameController,
			String                        action,
			Dictionary<String, Object>    temporaryStateMap
		) {
			var state = true as Boolean?;
			var shouldReload = false;
			var closeDialog = await ControlHelper.ShowDialogForWaiting(this.View, null, null);
			try {
				var cancelled = false;
				var game = gameController.Configuration;
				var temporaryState = temporaryStateMap.GetValueOrDefault(action);
				var confirmIfNotTested = async Task<Boolean> () => {
					if (GameUtility.IsTestedGame(Setting.Data.TestedGame, game.Identity, game.Version)) {
						return true;
					}
					var controlWarning = new TextBlock() {
						Text = "Action for this game may cause error, are you confirm?",
					};
					var controlTrust = new Button() {
						HorizontalAlignment = HorizontalAlignment.Stretch,
						Content = "Trust This Game",
					};
					controlTrust.Click += async (_, _) => {
						if (Setting.Data.TestedGame.TryGetValue(game.Identity, out var versionList)) {
							versionList.Add(game.Version);
						}
						else {
							Setting.Data.TestedGame.Add(game.Identity, [game.Version]);
						}
						await Setting.Save();
						shouldReload = true;
						controlTrust.IsEnabled = false;
						return;
					};
					var controlPanel = new StackPanel() {
						HorizontalAlignment = HorizontalAlignment.Stretch,
						Spacing = 8,
						Children = {
							controlWarning,
							controlTrust,
						},
					};
					return await ControlHelper.ShowDialogForPausing(this.View, "Untested Game", controlPanel);
				};
				switch (action) {
					case "ReloadGame": {
						shouldReload = true;
						break;
					}
					case "RevealGame": {
						await StorageHelper.RevealDirectory(game.Path);
						break;
					}
					case "LaunchGame": {
						await ProcessHelper.CreateProcess($"{game.Path}/{GameUtility.ExecutableFile}", [], false);
						break;
					}
					case "RestoreProgram": {
						if (!await confirmIfNotTested()) {
							cancelled = true;
							break;
						}
						if (!(game.Program == GameProgramState.Modified)) {
							cancelled = true;
							break;
						}
						StorageHelper.CopyFile(game.Path + $"/{GameUtility.BackupDirectory}_{game.Version}/{GameUtility.BackupProgramFile}", game.Path + $"/{GameUtility.ProgramFile}");
						StorageHelper.TrashFile(game.Path + $"/{GameUtility.BackupDirectory}_{game.Version}/{GameUtility.BackupProgramFile}");
						shouldReload = true;
						break;
					}
					case "ModifyProgram": {
						if (!await confirmIfNotTested()) {
							cancelled = true;
							break;
						}
						if (!(game.Program == GameProgramState.Original || game.Program == GameProgramState.Modified)) {
							cancelled = true;
							break;
						}
						var argumentDisableRecordEncryption = true;
						var argumentEnableDebugMode = true;
						if (temporaryState is not null) {
							var temporaryData = temporaryState.AsClass<Tuple<Boolean, Boolean>>();
							argumentDisableRecordEncryption = temporaryData.Item1;
							argumentEnableDebugMode = temporaryData.Item2;
						}
						else {
							var controlDisableRecordEncryption = new ToggleButton() {
								HorizontalAlignment = HorizontalAlignment.Stretch,
								Content = "Disable Record Encryption",
								IsChecked = argumentDisableRecordEncryption,
							};
							var controlEnableDebugMode = new ToggleButton() {
								HorizontalAlignment = HorizontalAlignment.Stretch,
								Content = "Enable Debug Mode",
								IsChecked = argumentEnableDebugMode,
							};
							controlDisableRecordEncryption.Click += (_, _) => {
								argumentDisableRecordEncryption = controlDisableRecordEncryption.IsChecked.AsNotNull();
								return;
							};
							controlEnableDebugMode.Click += (_, _) => {
								argumentEnableDebugMode = controlEnableDebugMode.IsChecked.AsNotNull();
								return;
							};
							var controlPanel = new StackPanel() {
								HorizontalAlignment = HorizontalAlignment.Stretch,
								Orientation = Orientation.Vertical,
								Spacing = 8,
								Children = {
									controlDisableRecordEncryption,
									controlEnableDebugMode,
								},
							};
							if (!await ControlHelper.ShowDialogForPausing(this.View, "Program Feature", controlPanel)) {
								cancelled = true;
								break;
							}
							temporaryStateMap.Add(action, new Tuple<Boolean, Boolean>(argumentDisableRecordEncryption, argumentEnableDebugMode));
						}
						await GameUtility.ModifyProgram(game.Path, argumentDisableRecordEncryption, argumentEnableDebugMode, Setting.Data.ProgramFileOfIl2CppDumper, game.Version, (_) => { });
						shouldReload = true;
						break;
					}
					case "EncryptRecord": {
						if (!await confirmIfNotTested()) {
							cancelled = true;
							break;
						}
						if (!(game.Record == GameRecordState.Decrypted)) {
							cancelled = true;
							break;
						}
						var recordDirectory = $"{game.Path}/{GameUtility.RecordBundleDirectory}/{game.User}";
						await GameUtility.EncryptRecord(recordDirectory, GameUtility.ConvertKeyFromUser(game.User), (_) => { });
						shouldReload = true;
						break;
					}
					case "DecryptRecord": {
						if (!await confirmIfNotTested()) {
							cancelled = true;
							break;
						}
						if (!(game.Record == GameRecordState.Original)) {
							cancelled = true;
							break;
						}
						var recordDirectory = $"{game.Path}/{GameUtility.RecordBundleDirectory}/{game.User}";
						await GameUtility.EncryptRecord(recordDirectory, GameUtility.ConvertKeyFromUser(game.User), (_) => { });
						shouldReload = true;
						break;
					}
					case "ImportRecord": {
						if (!await confirmIfNotTested()) {
							cancelled = true;
							break;
						}
						var shouldEncrypt = false;
						if (!(game.Record == GameRecordState.Original || game.Record == GameRecordState.Decrypted)) {
							if (temporaryState is not null) {
								var temporaryData = temporaryState.AsClass<Tuple<Boolean>>();
								shouldEncrypt = temporaryData.Item1;
							}
							else {
								var controlModeOriginal = new ToggleButton() {
									HorizontalAlignment = HorizontalAlignment.Stretch,
									Content = "Original",
									IsChecked = shouldEncrypt,
								};
								var controlModeDecrypted = new ToggleButton() {
									HorizontalAlignment = HorizontalAlignment.Stretch,
									Content = "Decrypted",
									IsChecked = !shouldEncrypt,
								};
								controlModeOriginal.Click += (_, _) => {
									shouldEncrypt = controlModeOriginal.IsChecked.AsNotNull();
									controlModeDecrypted.IsChecked = !shouldEncrypt;
									return;
								};
								controlModeDecrypted.Click += (_, _) => {
									shouldEncrypt = !controlModeDecrypted.IsChecked.AsNotNull();
									controlModeOriginal.IsChecked = shouldEncrypt;
									return;
								};
								var controlPanel = new StackPanel() {
									HorizontalAlignment = HorizontalAlignment.Stretch,
									Orientation = Orientation.Vertical,
									Spacing = 8,
									Children = {
										controlModeOriginal,
										controlModeDecrypted,
									},
								};
								if (!await ControlHelper.ShowDialogForPausing(this.View, "Encryption Mode", controlPanel)) {
									cancelled = true;
									break;
								}
								temporaryStateMap.Add(action, new Tuple<Boolean>(shouldEncrypt));
							}
						}
						else {
							shouldEncrypt = game.Record == GameRecordState.Original;
						}
						var archiveFile = await StorageHelper.PickFile(WindowHelper.Find(this.View), GameUtility.RecordArchiveFileExtension);
						if (archiveFile is null) {
							cancelled = true;
							break;
						}
						var recordDirectory = $"{game.Path}/{GameUtility.RecordBundleDirectory}/{game.User}";
						if (StorageHelper.ExistDirectory(recordDirectory)) {
							StorageHelper.TrashDirectory(recordDirectory);
						}
						var archiveConfigurationForLocal = new GameRecordArchiveConfiguration() { Platform = GamePlatform.Windows.ToString().ToLower(), Identity = game.Identity, Version = game.Version };
						await GameUtility.ImportRecordArchive(archiveFile, recordDirectory, !shouldEncrypt ? null : GameUtility.ConvertKeyFromUser(game.User), async (archiveConfiguration) => {
							if (archiveConfiguration != archiveConfigurationForLocal) {
								if (!await ControlHelper.ShowDialogForPausing(this.View, "Record Incompatible", $"This archive may not work with the current game.\nProvided: {GameUtility.MakeRecordArchiveConfigurationText(archiveConfiguration)}\nExpected: {GameUtility.MakeRecordArchiveConfigurationText(archiveConfigurationForLocal)}")) {
									cancelled = true;
									return false;
								}
							}
							return true;
						});
						shouldReload = true;
						break;
					}
					case "ExportRecord": {
						if (!await confirmIfNotTested()) {
							cancelled = true;
							break;
						}
						var shouldEncrypt = false;
						if (!(game.Record == GameRecordState.Original || game.Record == GameRecordState.Decrypted)) {
							cancelled = true;
							break;
						}
						else {
							shouldEncrypt = game.Record == GameRecordState.Original;
						}
						var archiveFile = await StorageHelper.SaveFile(WindowHelper.Find(this.View), new ("Kairosoft Game Record Archive", GameUtility.RecordArchiveFileExtension), game.Name);
						if (archiveFile is null) {
							cancelled = true;
							break;
						}
						var recordDirectory = $"{game.Path}/{GameUtility.RecordBundleDirectory}/{game.User}";
						var archiveConfigurationForLocal = new GameRecordArchiveConfiguration() { Platform = GamePlatform.Windows.ToString().ToLower(), Identity = game.Identity, Version = game.Version };
						await GameUtility.ExportRecordArchive(archiveFile, recordDirectory, !shouldEncrypt ? null : GameUtility.ConvertKeyFromUser(game.User), async (archiveConfiguration) => {
							archiveConfiguration.Platform = archiveConfigurationForLocal.Platform;
							archiveConfiguration.Identity = archiveConfigurationForLocal.Identity;
							archiveConfiguration.Version = archiveConfigurationForLocal.Version;
							return true;
						});
						break;
					}
					default: throw new ();
				}
				if (!cancelled) {
					App.MainWindow.PublishTip(InfoBarSeverity.Success, "Succeeded.", "");
				}
				else {
					App.MainWindow.PublishTip(InfoBarSeverity.Warning, "Cancelled.", "");
					state = null;
				}
			}
			catch (Exception e) {
				App.MainWindow.PublishTip(InfoBarSeverity.Error, "Failed.", e.ToString());
				state = false;
			}
			await closeDialog();
			if (shouldReload) {
				await this.ReloadGame(gameController);
			}
			return state;
		}

		#endregion

		#region repository

		public String uRepositoryDirectoryText_Text {
			get {
				return Setting.Data.RepositoryDirectory;
			}
		}

		public Size uRepositoryDirectoryCount_Value {
			get {
				return this.uGameList_ItemsSource.Count;
			}
		}

		public async void uRepositoryDirectoryAction_Click (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.AsClass<MenuFlyoutItem>();
			switch (senders.Tag.AsClass<String>()) {
				case "Reload": {
					await this.LoadRepository();
					break;
				}
				case "Reselect": {
					var directory = await StorageHelper.PickDirectory(WindowHelper.Find(this.View));
					if (directory is not null) {
						Setting.Data.RepositoryDirectory = directory;
						await Setting.Save();
						this.NotifyPropertyChanged(
							nameof(this.uRepositoryDirectoryText_Text)
						);
						await this.LoadRepository();
					}
					break;
				}
				default: throw new ();
			}
			return;
		}

		#endregion

		#region game

		public ObservableCollection<ManagerPageGameItemController> uGameList_ItemsSource { get; set; } = [];

		// ----------------

		public async void uGameAction_Click (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.AsClass<MenuFlyoutItem>();
			var action = senders.Tag.AsClass<String>();
			var actionTemporaryState = new Dictionary<String, Object>();
			var result = new List<Tuple<ManagerPageGameItemController, Boolean?>>();
			foreach (var item in this.View.uGameList.SelectedItems.Select(GF.AsClass<ManagerPageGameItemController>)) {
				var state = await this.ActionGame(item, action, actionTemporaryState);
				result.Add(new (item, state));
				if (state is not null && !state.AsNotNull()) {
					if (!await ControlHelper.ShowDialogForPausing(this.View, "Error Occurred", $"Failed to action for '{item.Configuration.Name}'.\nWhether to proceed with the remaining item?")) {
						break;
					}
				}
			}
			if (result.Count != this.View.uGameList.SelectedItems.Count || !result.All((value) => (value.Item2 is not null && value.Item2.AsNotNull()))) {
				var report = String.Join('\n', result.Select((value) => ($"{(value.Item2 is null ? "Cancelled" : !value.Item2.AsNotNull() ? "Failed" : "Succeeded")} - {value.Item1.Configuration.Name}")));
				await ControlHelper.ShowDialogSimple(this.View, "Result Report", report);
			}
			return;
		}

		#endregion

	}

	public class ManagerPageGameItemController : CustomController {

		#region data

		public ManagerPageController Host { get; init; } = default!;

		// ----------------

		public GameConfiguration Configuration { get; set; } = default!;

		#endregion

		#region view

		public ImageSource? uIcon_Source {
			get {
				return this.Configuration.Icon;
			}
		}

		// ----------------

		public String uName_Text {
			get {
				return this.Configuration.Name;
			}
		}

		public String uName_ToolTip {
			get {
				return $"{this.Configuration.Path}\nUser {this.Configuration.User}";
			}
		}

		// ----------------

		public String uIdentity_ToolTip {
			get {
				var tested = GameUtility.IsTestedGame(Setting.Data.TestedGame, this.Configuration.Identity, null);
				return $"Identity {(!tested ? "Untested" : "Tested")}";
			}
		}

		public Style uIdentityBadge_Style {
			get {
				var tested = GameUtility.IsTestedGame(Setting.Data.TestedGame, this.Configuration.Identity, null);
				return this.Host.View.FindResource(!tested ? "CautionIconInfoBadgeStyle" : "SuccessIconInfoBadgeStyle").AsClass<Style>();
			}
		}

		public String uIdentityText_Text {
			get {
				return this.Configuration.Identity;
			}
		}

		// ----------------

		public String uVersion_ToolTip {
			get {
				var tested = GameUtility.IsTestedGame(Setting.Data.TestedGame, this.Configuration.Identity, this.Configuration.Version);
				return $"Version {(!tested ? "Untested" : "Tested")}";
			}
		}

		public Style uVersionBadge_Style {
			get {
				var tested = GameUtility.IsTestedGame(Setting.Data.TestedGame, this.Configuration.Identity, this.Configuration.Version);
				return this.Host.View.FindResource(!tested ? "CautionIconInfoBadgeStyle" : "SuccessIconInfoBadgeStyle").AsClass<Style>();
			}
		}

		public String uVersionText_Text {
			get {
				return this.Configuration.Version;
			}
		}

		// ----------------

		public String uRecord_ToolTip {
			get {
				return $"Record {this.Configuration.Record}";
			}
		}

		public Style uRecordBadge_Style {
			get {
				return this.Host.View.FindResource(this.Configuration.Record switch {
					GameRecordState.None      => "InformationalIconInfoBadgeStyle",
					GameRecordState.Invalid   => "CriticalIconInfoBadgeStyle",
					GameRecordState.Original  => "CautionIconInfoBadgeStyle",
					GameRecordState.Decrypted => "SuccessIconInfoBadgeStyle",
					_                         => throw new (),
				}).AsClass<Style>();
			}
		}

		public String uRecordText_Text {
			get {
				return this.Configuration.Record.ToString();
			}
		}

		// ----------------

		public String uProgram_ToolTip {
			get {
				return $"Program {this.Configuration.Program}";
			}
		}

		public Style uProgramBadge_Style {
			get {
				return this.Host.View.FindResource(this.Configuration.Program switch {
					GameProgramState.None     => "InformationalIconInfoBadgeStyle",
					GameProgramState.Original => "CautionIconInfoBadgeStyle",
					GameProgramState.Modified => "SuccessIconInfoBadgeStyle",
					_                         => throw new (),
				}).AsClass<Style>();
			}
		}

		public String uProgramText_Text {
			get {
				return this.Configuration.Program.ToString();
			}
		}

		// ----------------

		public Boolean uActionRestoreProgram_IsEnabled {
			get {
				return this.Configuration.Program == GameProgramState.Modified;
			}
		}

		public Boolean uActionModifyProgram_IsEnabled {
			get {
				return this.Configuration.Program == GameProgramState.Original || this.Configuration.Program == GameProgramState.Modified;
			}
		}

		public Boolean uActionEncryptRecord_IsEnabled {
			get {
				return this.Configuration.Record == GameRecordState.Decrypted;
			}
		}

		public Boolean uActionDecryptRecord_IsEnabled {
			get {
				return this.Configuration.Record == GameRecordState.Original;
			}
		}

		public Boolean uActionImportRecord_IsEnabled {
			get {
				return true;
			}
		}

		public Boolean uActionExportRecord_IsEnabled {
			get {
				return this.Configuration.Record == GameRecordState.Original || this.Configuration.Record == GameRecordState.Decrypted;
			}
		}

		// ----------------

		public async void uAction_Click (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.AsClass<MenuFlyoutItem>();
			await this.Host.ActionGame(this, senders.Tag.AsClass<String>(), []);
			return;
		}

		#endregion

	}

}
