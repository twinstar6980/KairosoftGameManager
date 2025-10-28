#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;
using KairosoftGameManager.Utility;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace KairosoftGameManager.View {

	public sealed partial class ManagerPage : Page {

		#region life

		private ManagerPageController Controller { get; }

		// ----------------

		public ManagerPage (
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

	public partial class ManagerPageController : CustomController {

		#region data

		public ManagerPage View { get; init; } = default!;

		#endregion

		#region life

		public void InitializeView (
		) {
			return;
		}

		public async Task UpdateView (
		) {
			await Task.Delay(200); // wait for LostFocus event on SettingPage
			if (App.Setting.State.CurrentRepositoryDirectory != App.Setting.Data.RepositoryDirectory) {
				this.NotifyPropertyChanged([
					nameof(this.uRepositoryDirectoryText_Text),
				]);
				await this.LoadRepository();
				App.Setting.State.CurrentRepositoryDirectory = App.Setting.Data.RepositoryDirectory;
			}
			return;
		}

		#endregion

		#region action

		public async Task LoadRepository (
		) {
			this.uGameList_ItemsSource.Clear();
			this.NotifyPropertyChanged([
				nameof(this.uRepositoryDirectoryCount_Value),
			]);
			var hideDialog = await ControlHelper.ShowDialogForWait(this.View);
			try {
				AssertTest(await GameHelper.CheckRepositoryDirectory(App.Setting.Data.RepositoryDirectory));
				var gameConfigurationList = new List<GameConfiguration>();
				foreach (var gameIdentifier in await GameHelper.ListGameInRepository(App.Setting.Data.RepositoryDirectory)) {
					var gameConfiguration = await GameHelper.LoadGameConfiguration(App.Setting.Data.RepositoryDirectory, gameIdentifier);
					if (gameConfiguration != null) {
						gameConfigurationList.Add(gameConfiguration);
					}
				}
				gameConfigurationList.Sort((left, right) => (String.CompareOrdinal(left.Name, right.Name)));
				foreach (var gameConfiguration in gameConfigurationList) {
					this.uGameList_ItemsSource.Add(new () { Host = this, Configuration = gameConfiguration });
				}
			}
			catch (Exception e) {
				await App.MainWindow.PushNotification(InfoBarSeverity.Error, "Failed to load repository.", ExceptionHelper.GenerateMessage(e));
			}
			await hideDialog();
			this.NotifyPropertyChanged([
				nameof(this.uRepositoryDirectoryCount_Value),
			]);
			return;
		}

		public async Task ReloadGame (
			ManagerPageGameItemController gameController
		) {
			gameController.Configuration = (await GameHelper.LoadGameConfiguration(App.Setting.Data.RepositoryDirectory, gameController.Configuration.Identifier)).AsNotNull();
			gameController.NotifyPropertyChanged([
				nameof(gameController.uIcon_Source),
				nameof(gameController.uName_Text),
				nameof(gameController.uIdentifierText_Text),
				nameof(gameController.uVersionText_Text),
				nameof(gameController.uProgramBadge_Style),
				nameof(gameController.uProgramText_Text),
				nameof(gameController.uRecordBadge_Style),
				nameof(gameController.uRecordText_Text),
				nameof(gameController.uActionRestoreProgram_IsEnabled),
				nameof(gameController.uActionModifyProgram_IsEnabled),
				nameof(gameController.uActionEncryptRecord_IsEnabled),
				nameof(gameController.uActionDecryptRecord_IsEnabled),
				nameof(gameController.uActionImportRecord_IsEnabled),
				nameof(gameController.uActionExportRecord_IsEnabled),
			]);
			return;
		}

		public async Task<Boolean?> ActionGame (
			ManagerPageGameItemController gameController,
			String                        action,
			Dictionary<String, Object>    temporaryStateMap
		) {
			var state = true as Boolean?;
			var shouldReload = false;
			var hideDialog = await ControlHelper.ShowDialogForWait(this.View);
			try {
				var cancelled = false;
				var game = gameController.Configuration;
				var temporaryState = temporaryStateMap.GetValueOrDefault(action);
				switch (action) {
					case "ReloadGame": {
						shouldReload = true;
						break;
					}
					case "RevealGame": {
						await StorageHelper.Reveal(game.Path);
						break;
					}
					case "LaunchGame": {
						await ProcessHelper.RunProcess($"{game.Path}/{GameHelper.ExecutableFile}", [], false);
						break;
					}
					case "RestoreProgram": {
						if (game.Program != GameProgramState.Modified) {
							cancelled = true;
							break;
						}
						var programFile = $"{game.Path}/{GameHelper.ProgramFile}";
						var backupFile = $"{game.Path}/{GameHelper.ProgramFile}.{game.Version}.bak";
						StorageHelper.Trash(programFile);
						StorageHelper.Rename(backupFile, programFile);
						shouldReload = true;
						break;
					}
					case "ModifyProgram": {
						if (game.Program != GameProgramState.Original && game.Program != GameProgramState.Modified) {
							cancelled = true;
							break;
						}
						var argumentDisableRecordEncryption = true;
						var argumentEnableDebugMode = false;
						if (temporaryState != null) {
							var temporaryData = temporaryState.As<Tuple<Boolean, Boolean>>();
							argumentDisableRecordEncryption = temporaryData.Item1;
							argumentEnableDebugMode = temporaryData.Item2;
						}
						else {
							var dialogPanel = new StackPanel() {
								HorizontalAlignment = HorizontalAlignment.Stretch,
								VerticalAlignment = VerticalAlignment.Stretch,
								Orientation = Orientation.Vertical,
								Spacing = 8,
								Children = {
									new ToggleButton() {
										HorizontalAlignment = HorizontalAlignment.Stretch,
										VerticalAlignment = VerticalAlignment.Stretch,
										Content = "Disable Record Encryption",
										IsChecked = argumentDisableRecordEncryption,
									}.SelfAlso((it) => {
										it.Click += async (_, _) => {
											argumentDisableRecordEncryption = it.IsChecked.AsNotNull();
											return;
										};
									}),
									new ToggleButton() {
										HorizontalAlignment = HorizontalAlignment.Stretch,
										VerticalAlignment = VerticalAlignment.Stretch,
										Content = "Enable Debug Mode",
										IsChecked = argumentEnableDebugMode,
									}.SelfAlso((it) => {
										it.Click += async (_, _) => {
											argumentEnableDebugMode = it.IsChecked.AsNotNull();
											return;
										};
									}),
								},
							};
							if (!await ControlHelper.ShowDialogForConfirm(this.View, "Program Feature", dialogPanel)) {
								cancelled = true;
								break;
							}
							temporaryStateMap.Add(action, new Tuple<Boolean, Boolean>(argumentDisableRecordEncryption, argumentEnableDebugMode));
						}
						var programFile = $"{game.Path}/{GameHelper.ProgramFile}";
						var backupFile = $"{game.Path}/{GameHelper.ProgramFile}.{game.Version}.bak";
						if (!StorageHelper.ExistFile(backupFile)) {
							StorageHelper.Rename(programFile, backupFile);
						}
						if (StorageHelper.ExistFile(programFile)) {
							StorageHelper.Trash(programFile);
						}
						StorageHelper.Copy(backupFile, programFile);
						await GameHelper.ModifyProgram(
							game.Path,
							argumentDisableRecordEncryption,
							argumentEnableDebugMode,
							ExternalToolHelper.ParseSetting(App.Setting.Data.ExternalTool),
							(_) => { }
						);
						shouldReload = true;
						break;
					}
					case "EncryptRecord": {
						if (game.Record != GameRecordState.Decrypted) {
							cancelled = true;
							break;
						}
						var recordDirectory = $"{game.Path}/{GameHelper.RecordBundleDirectory}/{game.User}";
						await GameHelper.EncryptRecord(
							recordDirectory,
							GameHelper.ConvertKeyFromUser(game.User),
							(_) => { }
						);
						shouldReload = true;
						break;
					}
					case "DecryptRecord": {
						if (game.Record != GameRecordState.Original) {
							cancelled = true;
							break;
						}
						var recordDirectory = $"{game.Path}/{GameHelper.RecordBundleDirectory}/{game.User}";
						await GameHelper.EncryptRecord(
							recordDirectory,
							GameHelper.ConvertKeyFromUser(game.User),
							(_) => { }
						);
						shouldReload = true;
						break;
					}
					case "ImportRecord": {
						var shouldEncrypt = false;
						if (!(game.Record == GameRecordState.Original || game.Record == GameRecordState.Decrypted)) {
							if (temporaryState != null) {
								var temporaryData = temporaryState.As<Tuple<Boolean>>();
								shouldEncrypt = temporaryData.Item1;
							}
							else {
								var dialogPanel = new StackPanel() {
									HorizontalAlignment = HorizontalAlignment.Stretch,
									VerticalAlignment = VerticalAlignment.Stretch,
									Orientation = Orientation.Vertical,
									Spacing = 8,
									Children = {
										new ToggleButton() {
											HorizontalAlignment = HorizontalAlignment.Stretch,
											VerticalAlignment = VerticalAlignment.Stretch,
											Content = "Original",
											IsChecked = shouldEncrypt,
										}.SelfAlso((it) => {
											it.Click += async (_, _) => {
												shouldEncrypt = true;
												it.IsChecked = true;
												it.Parent.As<StackPanel>().Children[1].As<ToggleButton>().IsChecked = false;
												return;
											};
										}),
										new ToggleButton() {
											HorizontalAlignment = HorizontalAlignment.Stretch,
											VerticalAlignment = VerticalAlignment.Stretch,
											Content = "Decrypted",
											IsChecked = !shouldEncrypt,
										}.SelfAlso((it) => {
											it.Click += async (_, _) => {
												shouldEncrypt = false;
												it.IsChecked = true;
												it.Parent.As<StackPanel>().Children[0].As<ToggleButton>().IsChecked = false;
												return;
											};
										}),
									},
								};
								if (!await ControlHelper.ShowDialogForConfirm(this.View, "Encryption Mode", dialogPanel)) {
									cancelled = true;
									break;
								}
								temporaryStateMap.Add(action, new Tuple<Boolean>(shouldEncrypt));
							}
						}
						else {
							shouldEncrypt = game.Record == GameRecordState.Original;
						}
						var archiveFile = await StorageHelper.PickLoadFile(App.MainWindow, "@RecordFile");
						if (archiveFile == null) {
							cancelled = true;
							break;
						}
						var recordDirectory = $"{game.Path}/{GameHelper.RecordBundleDirectory}/{game.User}";
						var archiveConfigurationForLocal = new GameRecordArchiveConfiguration() { Platform = GamePlatform.WindowsIntel32.ToString().ToLower(), Identifier = game.Identifier, Version = game.Version };
						await GameHelper.ImportRecordArchive(
							archiveFile,
							recordDirectory,
							!shouldEncrypt ? null : GameHelper.ConvertKeyFromUser(game.User),
							async (archiveConfiguration) => {
								if (archiveConfiguration != archiveConfigurationForLocal) {
									if (!await ControlHelper.ShowDialogForConfirm(this.View, "Record Incompatible", $"This archive may not work with the current game.\nProvided: {GameHelper.MakeRecordArchiveConfigurationText(archiveConfiguration)}\nExpected: {GameHelper.MakeRecordArchiveConfigurationText(archiveConfigurationForLocal)}")) {
										cancelled = true;
										return false;
									}
								}
								if (StorageHelper.ExistDirectory(recordDirectory)) {
									StorageHelper.Trash(recordDirectory);
								}
								return true;
							}
						);
						shouldReload = true;
						break;
					}
					case "ExportRecord": {
						var shouldEncrypt = false;
						if (game.Record != GameRecordState.Original && game.Record != GameRecordState.Decrypted) {
							cancelled = true;
							break;
						}
						else {
							shouldEncrypt = game.Record == GameRecordState.Original;
						}
						var archiveFile = await StorageHelper.PickSaveFile(App.MainWindow, "@RecordFile", $"{game.Name}.{GameHelper.RecordArchiveFileExtension}");
						if (archiveFile == null) {
							cancelled = true;
							break;
						}
						var recordDirectory = $"{game.Path}/{GameHelper.RecordBundleDirectory}/{game.User}";
						var archiveConfigurationForLocal = new GameRecordArchiveConfiguration() { Platform = GamePlatform.WindowsIntel32.ToString().ToLower(), Identifier = game.Identifier, Version = game.Version };
						await GameHelper.ExportRecordArchive(
							archiveFile,
							recordDirectory,
							!shouldEncrypt ? null : GameHelper.ConvertKeyFromUser(game.User),
							async (archiveConfiguration) => {
								archiveConfiguration.Platform = archiveConfigurationForLocal.Platform;
								archiveConfiguration.Identifier = archiveConfigurationForLocal.Identifier;
								archiveConfiguration.Version = archiveConfigurationForLocal.Version;
								return true;
							}
						);
						break;
					}
					default: throw new UnreachableException();
				}
				if (!cancelled) {
					await App.MainWindow.PushNotification(InfoBarSeverity.Success, "Succeeded.", "");
				}
				else {
					await App.MainWindow.PushNotification(InfoBarSeverity.Warning, "Cancelled.", "");
					state = null;
				}
			}
			catch (Exception e) {
				await App.MainWindow.PushNotification(InfoBarSeverity.Error, "Failed.", ExceptionHelper.GenerateMessage(e));
				state = false;
			}
			await hideDialog();
			if (shouldReload) {
				await this.ReloadGame(gameController);
			}
			return state;
		}

		#endregion

		#region repository

		public String uRepositoryDirectoryText_Text {
			get {
				return App.Setting.Data.RepositoryDirectory;
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
			var senders = sender.As<MenuFlyoutItem>();
			switch (senders.Tag.As<String>()) {
				case "Reload": {
					await this.LoadRepository();
					break;
				}
				case "Reselect": {
					var directory = await StorageHelper.PickLoadDirectory(App.MainWindow, "@RepositoryDirectory");
					if (directory != null) {
						App.Setting.Data.RepositoryDirectory = directory;
						this.NotifyPropertyChanged([
							nameof(this.uRepositoryDirectoryText_Text),
						]);
						await App.Setting.Save();
						await this.LoadRepository();
					}
					break;
				}
				default: throw new UnreachableException();
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
			var senders = sender.As<MenuFlyoutItem>();
			var action = senders.Tag.As<String>();
			var actionTemporaryState = new Dictionary<String, Object>();
			var result = new List<Tuple<ManagerPageGameItemController, Boolean?>>();
			foreach (var item in this.View.uGameList.SelectedItems.Select(CommonUtility.As<ManagerPageGameItemController>)) {
				var state = await this.ActionGame(item, action, actionTemporaryState);
				result.Add(new (item, state));
				if (state != null && !state.AsNotNull()) {
					if (!await ControlHelper.ShowDialogForConfirm(this.View, "Error Occurred", $"Failed to action for '{item.Configuration.Name}'.\nWhether to proceed with the remaining item?")) {
						break;
					}
				}
			}
			if (result.Count != this.View.uGameList.SelectedItems.Count || !result.All((value) => (value.Item2 != null && value.Item2.AsNotNull()))) {
				await ControlHelper.ShowDialogAsAutomatic(this.View, "Result Report", new TextBlock() {
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch,
					Text = String.Join('\n', result.Select((value) => ($"{(value.Item2 == null ? "Cancelled" : !value.Item2.AsNotNull() ? "Failed" : "Succeeded")} - {value.Item1.Configuration.Name}"))),
				}, null);
			}
			return;
		}

		#endregion

	}

	public partial class ManagerPageGameItemController : CustomController {

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

		public String uName_ToolTip {
			get {
				return $"{this.Configuration.Path}\nUser {this.Configuration.User}";
			}
		}

		public String uName_Text {
			get {
				return this.Configuration.Name;
			}
		}

		// ----------------

		public String uIdentifierText_Text {
			get {
				return this.Configuration.Identifier;
			}
		}

		// ----------------

		public String uVersionText_Text {
			get {
				return this.Configuration.Version;
			}
		}

		// ----------------

		public Style uProgramBadge_Style {
			get {
				return this.Host.View.FindResource(this.Configuration.Program switch {
					GameProgramState.None     => "InformationalIconInfoBadgeStyle",
					GameProgramState.Original => "CautionIconInfoBadgeStyle",
					GameProgramState.Modified => "SuccessIconInfoBadgeStyle",
					_                         => throw new UnreachableException(),
				}).As<Style>();
			}
		}

		public String uProgramText_Text {
			get {
				return this.Configuration.Program.ToString();
			}
		}

		// ----------------

		public Style uRecordBadge_Style {
			get {
				return this.Host.View.FindResource(this.Configuration.Record switch {
					GameRecordState.None      => "InformationalIconInfoBadgeStyle",
					GameRecordState.Invalid   => "CriticalIconInfoBadgeStyle",
					GameRecordState.Original  => "CautionIconInfoBadgeStyle",
					GameRecordState.Decrypted => "SuccessIconInfoBadgeStyle",
					_                         => throw new UnreachableException(),
				}).As<Style>();
			}
		}

		public String uRecordText_Text {
			get {
				return this.Configuration.Record.ToString();
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
			var senders = sender.As<MenuFlyoutItem>();
			await this.Host.ActionGame(this, senders.Tag.As<String>(), []);
			return;
		}

		#endregion

	}

}
