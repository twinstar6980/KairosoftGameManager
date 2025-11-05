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

		// ----------------

		public String? ActiveRepositoryDirectory { get; set; } = null;

		#endregion

		#region life

		public void InitializeView (
		) {
			return;
		}

		public async Task UpdateView (
		) {
			await Task.Delay(200); // wait for LostFocus event on SettingPage
			if (!App.Setting.State.CurrentRepositoryDirectory.SequenceEqual(App.Setting.Data.RepositoryDirectory)) {
				App.Setting.State.CurrentRepositoryDirectory = [..App.Setting.Data.RepositoryDirectory];
				this.NotifyPropertyChanged([
					nameof(this.uRepositoryDirectoryList_Flyout),
				]);
				if (App.Setting.State.CurrentRepositoryDirectory.Count == 0) {
					await this.LoadRepository(null);
				}
				else if (this.ActiveRepositoryDirectory == null || !App.Setting.State.CurrentRepositoryDirectory.Contains(this.ActiveRepositoryDirectory)) {
					await this.LoadRepository(App.Setting.Data.RepositoryDirectory.First());
				}
			}
			return;
		}

		#endregion

		#region action

		public async Task LoadRepository (
			String? repositoryDirectory
		) {
			this.uGameList_ItemsSource.Clear();
			this.ActiveRepositoryDirectory = repositoryDirectory;
			this.NotifyPropertyChanged([
				nameof(this.uRepositoryDirectoryText_Text),
				nameof(this.uRepositoryDirectoryGameCount_Value),
			]);
			if (repositoryDirectory == null) {
				return;
			}
			try {
				await using var hideDialogFinalizer = new Finalizer(await ControlHelper.ShowDialogForWait(this.View));
				var gameList = null as List<GameConfiguration>;
				if (await GameHelper.CheckSteamRepository(repositoryDirectory)) {
					gameList = await GameHelper.LoadSteamRepository(repositoryDirectory);
				}
				else if (await GameHelper.CheckCustomRepository(repositoryDirectory)) {
					gameList = await GameHelper.LoadCustomRepository(repositoryDirectory);
				}
				if (gameList == null) {
					await App.MainWindow.PushNotification(InfoBarSeverity.Warning, "The specified repository directory is invalid.", "");
				}
				else {
					gameList.Sort((left, right) => (String.CompareOrdinal(left.Name, right.Name)));
					foreach (var game in gameList) {
						this.uGameList_ItemsSource.Add(new () { Host = this, Configuration = game });
					}
				}
			}
			catch (Exception e) {
				await App.MainWindow.PushNotification(InfoBarSeverity.Error, "Failed to load repository.", ExceptionHelper.GenerateMessage(e));
			}
			this.NotifyPropertyChanged([
				nameof(this.uRepositoryDirectoryGameCount_Value),
			]);
			return;
		}

		public async Task ReloadGame (
			ManagerPageGameItemController gameController
		) {
			if (gameController.Configuration.Library == null) {
				gameController.Configuration = (await GameHelper.LoadCustomGame(gameController.Configuration.Path)).AsNotNull();
			}
			else {
				gameController.Configuration = (await GameHelper.LoadSteamGame(gameController.Configuration.Library, gameController.Configuration.Identifier.AsNotNull())).AsNotNull();
			}
			gameController.NotifyPropertyChanged([
				nameof(gameController.uIcon_Source),
				nameof(gameController.uName_ToolTip),
				nameof(gameController.uName_Text),
				nameof(gameController.uIdentifierBadge_Style),
				nameof(gameController.uIdentifierText_Text),
				nameof(gameController.uVersionBadge_Style),
				nameof(gameController.uVersionText_Text),
				nameof(gameController.uProgramBadge_Style),
				nameof(gameController.uProgramText_Text),
				nameof(gameController.uRecordBadge_Style),
				nameof(gameController.uRecordText_Text),
				nameof(gameController.uActionModifyProgram_IsEnabled),
				nameof(gameController.uActionRestoreProgram_IsEnabled),
				nameof(gameController.uActionEncryptRecord_IsEnabled),
				nameof(gameController.uActionDecryptRecord_IsEnabled),
				nameof(gameController.uActionExportRecord_IsEnabled),
				nameof(gameController.uActionImportRecord_IsEnabled),
			]);
			return;
		}

		public async Task<Tuple<Boolean?, Exception?>> ActionGame (
			ManagerPageGameItemController gameController,
			String                        action,
			Dictionary<String, Object>    temporaryStateMap
		) {
			var state = null as Boolean?;
			var exception = null as Exception;
			var shouldReload = false;
			await using var shouldReloadFinalizer = new Finalizer(async () => {
				if (shouldReload) {
					await this.ReloadGame(gameController);
				}
			});
			try {
				await using var hideDialogFinalizer = new Finalizer(await ControlHelper.ShowDialogForWait(this.View));
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
						await ProcessHelper.RunProcess($"{game.Path}/{GameHelper.ExecutableFile}", [], null, false);
						break;
					}
					case "ModifyProgram": {
						shouldReload = true;
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
						var backupFile = $"{game.Path}/{GameHelper.ProgramFile}.{game.Version ?? "0"}.bak";
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
						break;
					}
					case "RestoreProgram": {
						shouldReload = true;
						if (game.Program != GameProgramState.Modified) {
							cancelled = true;
							break;
						}
						var programFile = $"{game.Path}/{GameHelper.ProgramFile}";
						var backupFile = $"{game.Path}/{GameHelper.ProgramFile}.{game.Version ?? "0"}.bak";
						StorageHelper.Trash(programFile);
						StorageHelper.Rename(backupFile, programFile);
						break;
					}
					case "EncryptRecord": {
						shouldReload = true;
						if (game.Record != GameRecordState.Decrypted) {
							cancelled = true;
							break;
						}
						var recordDirectory = $"{game.Path}/{GameHelper.RecordBundleDirectory}/{game.User}";
						await GameHelper.EncryptRecord(
							recordDirectory,
							GameHelper.MakeKeyFromSteamUser(game.User),
							(_) => { }
						);
						break;
					}
					case "DecryptRecord": {
						shouldReload = true;
						if (game.Record != GameRecordState.Original) {
							cancelled = true;
							break;
						}
						var recordDirectory = $"{game.Path}/{GameHelper.RecordBundleDirectory}/{game.User}";
						await GameHelper.EncryptRecord(
							recordDirectory,
							GameHelper.MakeKeyFromSteamUser(game.User),
							(_) => { }
						);
						break;
					}
					case "ExportRecord": {
						shouldReload = true;
						if (game.Record != GameRecordState.Original && game.Record != GameRecordState.Decrypted) {
							cancelled = true;
							break;
						}
						var shouldEncrypt = game.Record == GameRecordState.Original;
						var archiveFile = await StorageHelper.PickSaveFile(App.MainWindow, "@RecordArchiveFile", $"{game.Name}.{GameHelper.RecordArchiveFileExtension}");
						if (archiveFile == null) {
							cancelled = true;
							break;
						}
						var recordDirectory = $"{game.Path}/{GameHelper.RecordBundleDirectory}/{game.User}";
						var archiveConfigurationForLocal = new GameRecordArchiveConfiguration() {
							Platform = GameHelper.GetPlatformSystemName(GamePlatform.WindowsIntel32),
							Identifier = game.Identifier ?? "unknown",
							Version = game.Version ?? "unknown",
						};
						await GameHelper.ExportRecordArchive(
							recordDirectory,
							archiveFile,
							!shouldEncrypt ? null : GameHelper.MakeKeyFromSteamUser(game.User),
							async (archiveConfiguration) => {
								archiveConfiguration.Platform = archiveConfigurationForLocal.Platform;
								archiveConfiguration.Identifier = archiveConfigurationForLocal.Identifier;
								archiveConfiguration.Version = archiveConfigurationForLocal.Version;
								return true;
							}
						);
						break;
					}
					case "ImportRecord": {
						shouldReload = true;
						var shouldEncrypt = game.Record == GameRecordState.Original;
						if (game.Record != GameRecordState.Original && game.Record != GameRecordState.Decrypted) {
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
						var archiveFile = await StorageHelper.PickLoadFile(App.MainWindow, "@RecordArchiveFile");
						if (archiveFile == null) {
							cancelled = true;
							break;
						}
						var recordDirectory = $"{game.Path}/{GameHelper.RecordBundleDirectory}/{game.User}";
						var archiveConfigurationForLocal = new GameRecordArchiveConfiguration() {
							Platform = GameHelper.GetPlatformSystemName(GamePlatform.WindowsIntel32),
							Identifier = game.Identifier ?? "unknown",
							Version = game.Version ?? "unknown",
						};
						await GameHelper.ImportRecordArchive(
							recordDirectory,
							archiveFile,
							!shouldEncrypt ? null : GameHelper.MakeKeyFromSteamUser(game.User),
							async (archiveConfiguration) => {
								if (archiveConfiguration != archiveConfigurationForLocal) {
									if (!await ControlHelper.ShowDialogForConfirm(this.View, "Record Incompatible", new TextBlock() {
											HorizontalAlignment = HorizontalAlignment.Stretch,
											VerticalAlignment = VerticalAlignment.Stretch,
											Style = this.View.FindResource("BodyTextBlockStyle").As<Style>(),
											TextWrapping = TextWrapping.Wrap,
											Text = String.Join('\n', [
												$"This archive may not work with the current game.",
												$"Provided: {GameHelper.MakeRecordArchiveConfigurationText(archiveConfiguration)}",
												$"Expected: {GameHelper.MakeRecordArchiveConfigurationText(archiveConfigurationForLocal)}",
											]),
										})) {
										cancelled = true;
										return false;
									}
								}
								return true;
							}
						);
						break;
					}
					default: throw new UnreachableException();
				}
				if (!cancelled) {
					state = true;
				}
			}
			catch (Exception e) {
				state = false;
				exception = e;
			}
			return new (state, exception);
		}

		#endregion

		#region repository

		public MenuFlyout uRepositoryDirectoryList_Flyout {
			get {
				var menu = new MenuFlyout() {
					Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft,
				};
				foreach (var item in App.Setting.Data.RepositoryDirectory) {
					menu.Items.Add(new MenuFlyoutItem() {
						Text = item,
					}.SelfAlso((it) => {
						it.Click += async (_, _) => {
							await this.LoadRepository(item);
							return;
						};
					}));
				}
				return menu;
			}
		}

		public String uRepositoryDirectoryText_Text {
			get {
				return this.ActiveRepositoryDirectory ?? "None";
			}
		}

		public Size uRepositoryDirectoryGameCount_Value {
			get {
				return this.uGameList_ItemsSource.Count;
			}
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
				var (state, exception) = await this.ActionGame(item, action, actionTemporaryState);
				result.Add(new (item, state));
				if (state != null && !state.AsNotNull()) {
					if (!await ControlHelper.ShowDialogForConfirm(this.View, "Error Occurred", new TextBlock() {
							HorizontalAlignment = HorizontalAlignment.Stretch,
							VerticalAlignment = VerticalAlignment.Stretch,
							Style = this.View.FindResource("BodyTextBlockStyle").As<Style>(),
							IsTextSelectionEnabled = true,
							TextWrapping = TextWrapping.Wrap,
							Text = String.Join('\n', [
								$"Failed to action for '{item.Configuration.Name}'.",
								$"Whether to proceed with the remaining item?",
								$"",
								$"{ExceptionHelper.GenerateMessage(exception.AsNotNull())}",
							]),
						})) {
						break;
					}
				}
			}
			if (result.Count != this.View.uGameList.SelectedItems.Count || !result.All((value) => (value.Item2 != null && value.Item2.AsNotNull()))) {
				await ControlHelper.ShowDialogAsAutomatic(this.View, "Result Report", new TextBlock() {
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch,
					Style = this.View.FindResource("BodyTextBlockStyle").As<Style>(),
					TextWrapping = TextWrapping.Wrap,
					Text = String.Join('\n', result.Select((value) => ($"{(value.Item2 == null ? "Cancelled" : !value.Item2.AsNotNull() ? "Failed" : "Succeeded")} - {value.Item1.Configuration.Name}"))),
				}, null);
			}
			else {
				await App.MainWindow.PushNotification(InfoBarSeverity.Success, "Succeeded.", "");
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

		public Style uIdentifierBadge_Style {
			get {
				return this.Host.View.FindResource(this.Configuration.Identifier switch {
					null => "InformationalIconInfoBadgeStyle",
					_    => "SuccessIconInfoBadgeStyle",
				}).As<Style>();
			}
		}

		public String uIdentifierText_Text {
			get {
				return this.Configuration.Identifier ?? "Unknown";
			}
		}

		// ----------------

		public Style uVersionBadge_Style {
			get {
				return this.Host.View.FindResource(this.Configuration.Version switch {
					null => "InformationalIconInfoBadgeStyle",
					_    => "SuccessIconInfoBadgeStyle",
				}).As<Style>();
			}
		}

		public String uVersionText_Text {
			get {
				return this.Configuration.Version ?? "Unknown";
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

		public Boolean uActionModifyProgram_IsEnabled {
			get {
				return this.Configuration.Program == GameProgramState.Original || this.Configuration.Program == GameProgramState.Modified;
			}
		}

		public Boolean uActionRestoreProgram_IsEnabled {
			get {
				return this.Configuration.Program == GameProgramState.Modified;
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

		public Boolean uActionExportRecord_IsEnabled {
			get {
				return this.Configuration.Record == GameRecordState.Original || this.Configuration.Record == GameRecordState.Decrypted;
			}
		}

		public Boolean uActionImportRecord_IsEnabled {
			get {
				return true;
			}
		}

		// ----------------

		public async void uAction_Click (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.As<MenuFlyoutItem>();
			var (state, exception) = await this.Host.ActionGame(this, senders.Tag.As<String>(), []);
			if (state == null) {
				await App.MainWindow.PushNotification(InfoBarSeverity.Warning, "Cancelled.", "");
			}
			else if (state.AsNotNull()) {
				await App.MainWindow.PushNotification(InfoBarSeverity.Success, "Succeeded.", "");
			}
			else {
				await App.MainWindow.PushNotification(InfoBarSeverity.Error, "Failed.", ExceptionHelper.GenerateMessage(exception.AsNotNull()));
			}
			return;
		}

		#endregion

	}

}
