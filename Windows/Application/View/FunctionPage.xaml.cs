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

		public String ArgumentOfEncryptRecordOfTargetDirectory { get; set; } = "";

		public Byte[] ArgumentOfEncryptRecordOfKey { get; set; } = [0x00];

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
					GameFunctionType.ModifyProgram => FluentIconGlyph.Repair,
					_                              => throw new UnreachableException(),
				};
			}
		}

		public String uTypeName_Text {
			get {
				return this.Type switch {
					GameFunctionType.EncryptRecord => "Encrypt Record",
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
				nameof(GameFunctionType.ModifyProgram) => GameFunctionType.ModifyProgram,
				_                                      => throw new UnreachableException(),
			};
			this.NotifyPropertyChanged([
				nameof(this.uTypeIcon_Glyph),
				nameof(this.uTypeName_Text),
				nameof(this.uArgumentOfEncryptRecord_Visibility),
				nameof(this.uArgumentOfModifyProgram_Visibility),
			]);
			return;
		}

		#endregion

		#region argument

		public Boolean uArgumentOfEncryptRecord_Visibility {
			get {
				return this.Type == GameFunctionType.EncryptRecord;
			}
		}

		// ----------------

		public async void uArgumentOfEncryptRecordOfTargetDirectoryEditor_LostFocus (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.As<TextBox>();
			this.ArgumentOfEncryptRecordOfTargetDirectory = StorageHelper.Regularize(senders.Text);
			this.NotifyPropertyChanged([
				nameof(this.uArgumentOfEncryptRecordOfTargetDirectoryEditor_Text),
			]);
			return;
		}

		public String uArgumentOfEncryptRecordOfTargetDirectoryEditor_Text {
			get {
				return this.ArgumentOfEncryptRecordOfTargetDirectory;
			}
		}

		public async void uArgumentOfEncryptRecordOfTargetDirectoryPicker_Click (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.As<Button>();
			var value = await StorageHelper.PickLoadDirectory(App.MainWindow, "@EncryptRecord.TargetDirectory");
			if (value != null) {
				this.ArgumentOfEncryptRecordOfTargetDirectory = value;
				this.NotifyPropertyChanged([
					nameof(this.uArgumentOfEncryptRecordOfTargetDirectoryEditor_Text),
				]);
			}
			return;
		}

		// ----------------

		public async void uArgumentOfEncryptRecordOfKeyEditor_LostFocus (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.As<TextBox>();
			if (senders.Text.Length == 0) {
				this.ArgumentOfEncryptRecordOfKey = [0x00];
			}
			else if (senders.Text.StartsWith("d32:") && IntegerU32.TryParse(senders.Text["d32:".Length..], out var value32)) {
				this.ArgumentOfEncryptRecordOfKey = new Byte[4];
				BinaryPrimitives.WriteUInt32LittleEndian(this.ArgumentOfEncryptRecordOfKey, value32);
			}
			else if (senders.Text.StartsWith("d64:") && IntegerU64.TryParse(senders.Text["d64:".Length..], out var value64)) {
				this.ArgumentOfEncryptRecordOfKey = new Byte[8];
				BinaryPrimitives.WriteUInt64LittleEndian(this.ArgumentOfEncryptRecordOfKey, value64);
			}
			else {
				var text = senders.Text.Replace(" ", "");
				if (text.Length != 0 && text.All(Character.IsAsciiHexDigit)) {
					if (text.Length % 2 == 1) {
						text += "0";
					}
					this.ArgumentOfEncryptRecordOfKey = new Byte[text.Length / 2];
					for (var index = 0; index < text.Length / 2; index++) {
						this.ArgumentOfEncryptRecordOfKey[index] = IntegerU8.Parse(text.Substring(index * 2, 2), NumberStyles.HexNumber);
					}
				}
			}
			this.NotifyPropertyChanged([
				nameof(this.uArgumentOfEncryptRecordOfKeyEditor_Text),
			]);
			return;
		}

		public String uArgumentOfEncryptRecordOfKeyEditor_Text {
			get {
				return String.Join(' ', this.ArgumentOfEncryptRecordOfKey.Select((value) => ($"{value:x2}")));
			}
		}

		// ----------------

		public Boolean uArgumentOfModifyProgram_Visibility {
			get {
				return this.Type == GameFunctionType.ModifyProgram;
			}
		}

		// ----------------

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
			GF.AssertTest(!this.Running);
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
							await GameUtility.EncryptRecord(
								this.ArgumentOfEncryptRecordOfTargetDirectory,
								this.ArgumentOfEncryptRecordOfKey,
								PublishMessage
							);
							break;
						}
						case GameFunctionType.ModifyProgram: {
							await GameUtility.ModifyProgram(
								this.ArgumentOfModifyProgramOfTarget,
								this.ArgumentOfModifyProgramOfDisableRecordEncryption,
								this.ArgumentOfModifyProgramOfEnableDebugMode,
								App.Setting.Data.ProgramFileOfIl2CppDumper,
								PublishMessage
							);
							break;
						}
						default: throw new UnreachableException();
					}
					return;
				});
				PublishMessage($"Done.");
			}
			catch (Exception e) {
				await App.MainWindow.PushNotification(InfoBarSeverity.Error, "Failed to run function.", "");
				PublishMessage($"Exception!");
				PublishMessage(ExceptionHelper.GenerateMessage(e));
				this.RunningFailed = true;
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
