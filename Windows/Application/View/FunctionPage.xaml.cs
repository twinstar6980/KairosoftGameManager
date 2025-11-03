#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;
using KairosoftGameManager.Utility;
using System.Buffers.Binary;
using Microsoft.UI.Xaml.Navigation;
using FluentIconGlyph = KairosoftGameManager.Control.FluentIconGlyph;

namespace KairosoftGameManager.View {

	public sealed partial class FunctionPage : Page {

		#region life

		private FunctionPageController Controller { get; }

		// ----------------

		public FunctionPage (
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

	public partial class FunctionPageController : CustomController {

		#region data

		public FunctionPage View { get; init; } = default!;

		// ----------------

		public GameFunctionType Type { get; set; } = GameFunctionType.EncryptRecord;

		// ----------------

		public String ArgumentOfRecordOfTargetDirectory { get; set; } = "";

		public String ArgumentOfRecordOfArchiveFile { get; set; } = "";

		public Byte[] ArgumentOfRecordOfKey { get; set; } = [0x00];

		// ----------------

		public String ArgumentOfModifyProgramOfTarget { get; set; } = "";

		public Boolean ArgumentOfModifyProgramOfDisableRecordEncryption { get; set; } = true;

		public Boolean ArgumentOfModifyProgramOfEnableDebugMode { get; set; } = false;

		// ----------------

		public Boolean Running { get; set; } = false;

		public Boolean RunningFailed { get; set; } = false;

		// ----------------

		public String Message { get; set; } = "";

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

		#region type

		public String uTypeIcon_Glyph {
			get {
				return this.Type switch {
					GameFunctionType.EncryptRecord => FluentIconGlyph.Unlock,
					GameFunctionType.ExportRecord  => FluentIconGlyph.Export,
					GameFunctionType.ImportRecord  => FluentIconGlyph.Import,
					GameFunctionType.ModifyProgram => FluentIconGlyph.Repair,
					_                              => throw new UnreachableException(),
				};
			}
		}

		public String uTypeName_Text {
			get {
				return this.Type switch {
					GameFunctionType.EncryptRecord => "Encrypt Record",
					GameFunctionType.ExportRecord  => "Export Record",
					GameFunctionType.ImportRecord  => "Import Record",
					GameFunctionType.ModifyProgram => "Modify Program",
					_                              => throw new UnreachableException(),
				};
			}
		}

		public async void uTypeSelect_Click (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.As<MenuFlyoutItem>();
			this.Type = senders.Tag.As<String>() switch {
				nameof(GameFunctionType.EncryptRecord) => GameFunctionType.EncryptRecord,
				nameof(GameFunctionType.ExportRecord)  => GameFunctionType.ExportRecord,
				nameof(GameFunctionType.ImportRecord)  => GameFunctionType.ImportRecord,
				nameof(GameFunctionType.ModifyProgram) => GameFunctionType.ModifyProgram,
				_                                      => throw new UnreachableException(),
			};
			this.NotifyPropertyChanged([
				nameof(this.uTypeIcon_Glyph),
				nameof(this.uTypeName_Text),
				nameof(this.uArgumentOfRecordOfTargetDirectory_Visibility),
				nameof(this.uArgumentOfRecordOfArchiveFile_Visibility),
				nameof(this.uArgumentOfRecordOfKey_Visibility),
				nameof(this.uArgumentOfModifyProgramOfTarget_Visibility),
				nameof(this.uArgumentOfModifyProgramOfDisableRecordEncryption_Visibility),
				nameof(this.uArgumentOfModifyProgramOfEnableDebugMode_Visibility),
			]);
			return;
		}

		#endregion

		#region argument

		public Boolean uArgumentOfRecordOfTargetDirectory_Visibility {
			get {
				return this.Type == GameFunctionType.EncryptRecord
					|| this.Type == GameFunctionType.ExportRecord
					|| this.Type == GameFunctionType.ImportRecord;
			}
		}

		public async void uArgumentOfRecordOfTargetDirectoryEditor_LostFocus (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.As<TextBox>();
			this.ArgumentOfRecordOfTargetDirectory = StorageHelper.Regularize(senders.Text);
			this.NotifyPropertyChanged([
				nameof(this.uArgumentOfRecordOfTargetDirectoryEditor_Text),
			]);
			return;
		}

		public String uArgumentOfRecordOfTargetDirectoryEditor_Text {
			get {
				return this.ArgumentOfRecordOfTargetDirectory;
			}
		}

		public async void uArgumentOfRecordOfTargetDirectoryPicker_Click (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.As<Button>();
			var value = await StorageHelper.PickLoadDirectory(App.MainWindow, "@Record.TargetDirectory");
			if (value != null) {
				this.ArgumentOfRecordOfTargetDirectory = value;
				this.NotifyPropertyChanged([
					nameof(this.uArgumentOfRecordOfTargetDirectoryEditor_Text),
				]);
			}
			return;
		}

		// ----------------

		public Boolean uArgumentOfRecordOfArchiveFile_Visibility {
			get {
				return this.Type == GameFunctionType.ExportRecord
					|| this.Type == GameFunctionType.ImportRecord;
			}
		}

		public async void uArgumentOfRecordOfArchiveFileEditor_LostFocus (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.As<TextBox>();
			this.ArgumentOfRecordOfArchiveFile = StorageHelper.Regularize(senders.Text);
			this.NotifyPropertyChanged([
				nameof(this.uArgumentOfRecordOfArchiveFileEditor_Text),
			]);
			return;
		}

		public String uArgumentOfRecordOfArchiveFileEditor_Text {
			get {
				return this.ArgumentOfRecordOfArchiveFile;
			}
		}

		public async void uArgumentOfRecordOfArchiveFilePicker_Click (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.As<Button>();
			var value = this.Type switch {
				GameFunctionType.ExportRecord => await StorageHelper.PickSaveFile(App.MainWindow, "@Record.ArchiveFile", $"game.{GameHelper.RecordArchiveFileExtension}"),
				GameFunctionType.ImportRecord => await StorageHelper.PickLoadFile(App.MainWindow, "@Record.ArchiveFile"),
				_                             => throw new UnreachableException(),
			};
			if (value != null) {
				this.ArgumentOfRecordOfArchiveFile = value;
				this.NotifyPropertyChanged([
					nameof(this.uArgumentOfRecordOfArchiveFileEditor_Text),
				]);
			}
			return;
		}

		// ----------------

		public Boolean uArgumentOfRecordOfKey_Visibility {
			get {
				return this.Type == GameFunctionType.EncryptRecord
					|| this.Type == GameFunctionType.ExportRecord
					|| this.Type == GameFunctionType.ImportRecord;
			}
		}

		public async void uArgumentOfRecordOfKeyEditor_LostFocus (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.As<TextBox>();
			if (senders.Text.Length == 0) {
				this.ArgumentOfRecordOfKey = [0x00];
			}
			else if (senders.Text.StartsWith("d32:") && IntegerU32.TryParse(senders.Text["d32:".Length..], out var value32)) {
				this.ArgumentOfRecordOfKey = new Byte[4];
				BinaryPrimitives.WriteUInt32LittleEndian(this.ArgumentOfRecordOfKey, value32);
			}
			else if (senders.Text.StartsWith("d64:") && IntegerU64.TryParse(senders.Text["d64:".Length..], out var value64)) {
				this.ArgumentOfRecordOfKey = new Byte[8];
				BinaryPrimitives.WriteUInt64LittleEndian(this.ArgumentOfRecordOfKey, value64);
			}
			else {
				var text = senders.Text.Replace(" ", "");
				if (text.Length != 0 && text.All(Character.IsAsciiHexDigit)) {
					if (text.Length % 2 == 1) {
						text += "0";
					}
					this.ArgumentOfRecordOfKey = new Byte[text.Length / 2];
					for (var index = 0; index < text.Length / 2; index++) {
						this.ArgumentOfRecordOfKey[index] = IntegerU8.Parse(text.Substring(index * 2, 2), NumberStyles.HexNumber);
					}
				}
			}
			this.NotifyPropertyChanged([
				nameof(this.uArgumentOfRecordOfKeyEditor_Text),
			]);
			return;
		}

		public String uArgumentOfRecordOfKeyEditor_Text {
			get {
				return String.Join(' ', this.ArgumentOfRecordOfKey.Select((value) => ($"{value:x2}")));
			}
		}

		// ----------------

		public Boolean uArgumentOfModifyProgramOfTarget_Visibility {
			get {
				return this.Type == GameFunctionType.ModifyProgram;
			}
		}

		public async void uArgumentOfModifyProgramOfTargetEditor_LostFocus (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.As<TextBox>();
			this.ArgumentOfModifyProgramOfTarget = StorageHelper.Regularize(senders.Text);
			this.NotifyPropertyChanged([
				nameof(this.uArgumentOfModifyProgramOfTargetEditor_Text),
			]);
			return;
		}

		public String uArgumentOfModifyProgramOfTargetEditor_Text {
			get {
				return this.ArgumentOfModifyProgramOfTarget;
			}
		}

		public async void uArgumentOfModifyProgramOfTargetPicker_Click (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.As<MenuFlyoutItem>();
			var value = await StorageHelper.Pick($"Load{senders.Tag}", App.MainWindow, "@ModifyProgram.Target", null);
			if (value != null) {
				this.ArgumentOfModifyProgramOfTarget = value;
				this.NotifyPropertyChanged([
					nameof(this.uArgumentOfModifyProgramOfTargetEditor_Text),
				]);
			}
			return;
		}

		// ----------------

		public Boolean uArgumentOfModifyProgramOfDisableRecordEncryption_Visibility {
			get {
				return this.Type == GameFunctionType.ModifyProgram;
			}
		}

		public async void uArgumentOfModifyProgramOfDisableRecordEncryptionEditor_Click (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.As<Button>();
			this.ArgumentOfModifyProgramOfDisableRecordEncryption = !this.ArgumentOfModifyProgramOfDisableRecordEncryption;
			this.NotifyPropertyChanged([
				nameof(this.uArgumentOfModifyProgramOfDisableRecordEncryptionEditor_Content),
			]);
			return;
		}

		public String uArgumentOfModifyProgramOfDisableRecordEncryptionEditor_Content {
			get {
				return ConvertHelper.MakeBooleanToStringOfConfirmation(this.ArgumentOfModifyProgramOfDisableRecordEncryption);
			}
		}

		// ----------------

		public Boolean uArgumentOfModifyProgramOfEnableDebugMode_Visibility {
			get {
				return this.Type == GameFunctionType.ModifyProgram;
			}
		}

		public async void uArgumentOfModifyProgramOfEnableDebugModeEditor_Click (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.As<Button>();
			this.ArgumentOfModifyProgramOfEnableDebugMode = !this.ArgumentOfModifyProgramOfEnableDebugMode;
			this.NotifyPropertyChanged([
				nameof(this.uArgumentOfModifyProgramOfEnableDebugModeEditor_Content),
			]);
			return;
		}

		public String uArgumentOfModifyProgramOfEnableDebugModeEditor_Content {
			get {
				return ConvertHelper.MakeBooleanToStringOfConfirmation(this.ArgumentOfModifyProgramOfEnableDebugMode);
			}
		}

		#endregion

		#region run

		public Boolean uRun_IsEnabled {
			get {
				return !this.Running;
			}
		}

		public async void uRun_Click (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.As<Button>();
			AssertTest(!this.Running);
			this.Message = "";
			this.Running = true;
			this.RunningFailed = false;
			this.NotifyPropertyChanged([
				nameof(this.uRun_IsEnabled),
				nameof(this.uProgress_ProgressIndeterminate),
				nameof(this.uProgress_ProgressError),
			]);
			try {
				PublishMessage($"Starting at {DateTime.Now:HH:mm:ss}.");
				await Task.Run(async () => {
					switch (this.Type) {
						case GameFunctionType.EncryptRecord: {
							await GameHelper.EncryptRecord(
								this.ArgumentOfRecordOfTargetDirectory,
								this.ArgumentOfRecordOfKey,
								PublishMessage
							);
							break;
						}
						case GameFunctionType.ExportRecord: {
							await GameHelper.ExportRecordArchive(
								this.ArgumentOfRecordOfTargetDirectory,
								this.ArgumentOfRecordOfArchiveFile,
								this.ArgumentOfRecordOfKey,
								async (archiveConfiguration) => {
									archiveConfiguration.Platform = "unknown";
									archiveConfiguration.Identifier = "unknown";
									archiveConfiguration.Version = "unknown";
									return true;
								}
							);
							break;
						}
						case GameFunctionType.ImportRecord: {
							await GameHelper.ImportRecordArchive(
								this.ArgumentOfRecordOfTargetDirectory,
								this.ArgumentOfRecordOfArchiveFile,
								this.ArgumentOfRecordOfKey,
								async (archiveConfiguration) => {
									return true;
								}
							);
							break;
						}
						case GameFunctionType.ModifyProgram: {
							await GameHelper.ModifyProgram(
								this.ArgumentOfModifyProgramOfTarget,
								this.ArgumentOfModifyProgramOfDisableRecordEncryption,
								this.ArgumentOfModifyProgramOfEnableDebugMode,
								ExternalToolHelper.ParseSetting(App.Setting.Data.ExternalTool),
								PublishMessage
							);
							break;
						}
						default: throw new UnreachableException();
					}
					return;
				});
				PublishMessage($"Succeeded.");
				await App.MainWindow.PushNotification(InfoBarSeverity.Success, "Succeeded.", "");
			}
			catch (Exception e) {
				PublishMessage($"Failed.");
				PublishMessage(ExceptionHelper.GenerateMessage(e));
				this.RunningFailed = true;
				await App.MainWindow.PushNotification(InfoBarSeverity.Error, "Failed.", "");
			}
			PublishMessage($"");
			this.Running = false;
			this.NotifyPropertyChanged([
				nameof(this.uRun_IsEnabled),
				nameof(this.uProgress_ProgressIndeterminate),
				nameof(this.uProgress_ProgressError),
			]);
			return;
			async void PublishMessage (
				String message
			) {
				await App.MainWindow.DispatcherQueue.EnqueueAsync(async () => {
					this.Message += message + "\n";
					this.NotifyPropertyChanged([
						nameof(this.uMessage_Text),
					]);
					await Task.Delay(40);
					this.View.uMessageScrollViewer.ChangeView(null, this.View.uMessageScrollViewer.ScrollableHeight, null, true);
					return;
				});
				return;
			}
		}

		#endregion

		#region progress

		public Boolean uProgress_ProgressIndeterminate {
			get {
				return this.Running;
			}
		}

		public Boolean uProgress_ProgressError {
			get {
				return this.RunningFailed;
			}
		}

		#endregion

		#region message

		public String uMessage_Text {
			get {
				return this.Message;
			}
		}

		#endregion

	}

}
