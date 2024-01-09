#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;
using KairosoftGameManager.Utility;
using FluentIconGlyph = KairosoftGameManager.Control.FluentIconGlyph;

namespace KairosoftGameManager.View {

	public sealed partial class FunctionPage : Page {

		#region life

		public FunctionPage (
		) {
			this.InitializeComponent();
			this.Controller = new () { View = this };
			this.Controller.Initialize();
		}

		// ----------------

		private FunctionPageController Controller { get; }

		#endregion

	}

	public class FunctionPageController : CustomController {

		#region data

		public FunctionPage View { get; init; } = default!;

		// ----------------

		public FunctionType Type { get; set; } = FunctionType.EncryptRecord;

		// ----------------

		public String ArgumentOfEncryptRecordOfTargetDirectory { get; set; } = "";

		public IntegerU64 ArgumentOfEncryptRecordOfKey { get; set; } = 0;

		// ----------------

		public String ArgumentOfModifyProgramOfTargetDirectory { get; set; } = "";

		public Boolean ArgumentOfModifyProgramOfDisableRecordEncryption { get; set; } = true;

		public Boolean ArgumentOfModifyProgramOfEnableDebugMode { get; set; } = false;

		// ----------------

		public Boolean Running { get; set; } = false;

		// ----------------

		public String Message { get; set; } = "";

		#endregion

		#region initialize

		public void Initialize (
		) {
			return;
		}

		#endregion

		#region type

		public String uTypeIcon_Glyph {
			get {
				return this.Type switch {
					FunctionType.EncryptRecord => FluentIconGlyph.Unlock,
					FunctionType.ModifyProgram => FluentIconGlyph.Repair,
					_                          => throw new (),
				};
			}
		}

		public String uTypeName_Text {
			get {
				return this.Type switch {
					FunctionType.EncryptRecord => "Encrypt Record",
					FunctionType.ModifyProgram => "Modify Program",
					_                          => throw new (),
				};
			}
		}

		public async void uTypeSelect_Click (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.AsClass<MenuFlyoutItem>();
			this.Type = senders.Tag.AsClass<String>() switch {
				nameof(FunctionType.EncryptRecord) => FunctionType.EncryptRecord,
				nameof(FunctionType.ModifyProgram) => FunctionType.ModifyProgram,
				_                                  => throw new (),
			};
			this.NotifyPropertyChanged(
				nameof(this.uTypeIcon_Glyph),
				nameof(this.uTypeName_Text),
				nameof(this.uArgumentOfEncryptRecord_Visibility),
				nameof(this.uArgumentOfModifyProgram_Visibility)
			);
			return;
		}

		#endregion

		#region argument

		public Boolean uArgumentOfEncryptRecord_Visibility {
			get {
				return this.Type == FunctionType.EncryptRecord;
			}
		}

		// ----------------

		public String uArgumentOfEncryptRecordOfTargetDirectoryEditor_Text {
			get {
				return this.ArgumentOfEncryptRecordOfTargetDirectory;
			}
		}

		public async void uArgumentOfEncryptRecordOfTargetDirectoryEditor_LostFocus (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.AsClass<TextBox>();
			this.ArgumentOfEncryptRecordOfTargetDirectory = senders.Text;
			this.NotifyPropertyChanged(
				nameof(this.uArgumentOfEncryptRecordOfTargetDirectoryEditor_Text)
			);
			return;
		}

		public async void uArgumentOfEncryptRecordOfTargetDirectoryPicker_Click (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.AsClass<Button>();
			var value = await StorageHelper.PickDirectory(WindowHelper.Find(this.View));
			if (value is not null) {
				this.ArgumentOfEncryptRecordOfTargetDirectory = value;
				this.NotifyPropertyChanged(
					nameof(this.uArgumentOfEncryptRecordOfTargetDirectoryEditor_Text)
				);
			}
			return;
		}

		// ----------------

		public String uArgumentOfEncryptRecordOfKeyEditor_Text {
			get {
				return this.ArgumentOfEncryptRecordOfKey.ToString();
			}
		}

		public async void uArgumentOfEncryptRecordOfKeyEditor_LostFocus (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.AsClass<TextBox>();
			if (senders.Text.Length == 0) {
				this.ArgumentOfEncryptRecordOfKey = 0;
			}
			else if (IntegerU64.TryParse(senders.Text, out var value)) {
				this.ArgumentOfEncryptRecordOfKey = value;
			}
			this.NotifyPropertyChanged(
				nameof(this.uArgumentOfEncryptRecordOfKeyEditor_Text)
			);
			return;
		}

		// ----------------

		public Boolean uArgumentOfModifyProgram_Visibility {
			get {
				return this.Type == FunctionType.ModifyProgram;
			}
		}

		// ----------------

		public String uArgumentOfModifyProgramOfTargetDirectoryEditor_Text {
			get {
				return this.ArgumentOfModifyProgramOfTargetDirectory;
			}
		}

		public async void uArgumentOfModifyProgramOfTargetDirectoryEditor_LostFocus (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.AsClass<TextBox>();
			this.ArgumentOfModifyProgramOfTargetDirectory = senders.Text;
			this.NotifyPropertyChanged(
				nameof(this.uArgumentOfModifyProgramOfTargetDirectoryEditor_Text)
			);
			return;
		}

		public async void uArgumentOfModifyProgramOfTargetDirectoryPicker_Click (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.AsClass<Button>();
			var value = await StorageHelper.PickDirectory(WindowHelper.Find(this.View));
			if (value is not null) {
				this.ArgumentOfModifyProgramOfTargetDirectory = value;
				this.NotifyPropertyChanged(
					nameof(this.uArgumentOfModifyProgramOfTargetDirectoryEditor_Text)
				);
			}
			return;
		}

		// ----------------

		public String uArgumentOfModifyProgramOfDisableRecordEncryptionEditor_Content {
			get {
				return ConvertHelper.BooleanToConfirmationStringCamel(this.ArgumentOfModifyProgramOfDisableRecordEncryption);
			}
		}

		public async void uArgumentOfModifyProgramOfDisableRecordEncryptionEditor_Click (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.AsClass<Button>();
			this.ArgumentOfModifyProgramOfDisableRecordEncryption = !this.ArgumentOfModifyProgramOfDisableRecordEncryption;
			this.NotifyPropertyChanged(
				nameof(this.uArgumentOfModifyProgramOfDisableRecordEncryptionEditor_Content)
			);
			return;
		}

		// ----------------

		public String uArgumentOfModifyProgramOfEnableDebugModeEditor_Content {
			get {
				return ConvertHelper.BooleanToConfirmationStringCamel(this.ArgumentOfModifyProgramOfEnableDebugMode);
			}
		}

		public async void uArgumentOfModifyProgramOfEnableDebugModeEditor_Click (
			Object          sender,
			RoutedEventArgs args
		) {
			var senders = sender.AsClass<Button>();
			this.ArgumentOfModifyProgramOfEnableDebugMode = !this.ArgumentOfModifyProgramOfEnableDebugMode;
			this.NotifyPropertyChanged(
				nameof(this.uArgumentOfModifyProgramOfEnableDebugModeEditor_Content)
			);
			return;
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
			var senders = sender.AsClass<Button>();
			GF.AssertTest(!this.Running);
			this.Running = true;
			this.NotifyPropertyChanged(
				nameof(this.uRun_IsEnabled)
			);
			try {
				PublishMessage($"Starting at {DateTime.Now:HH:mm:ss}.");
				switch (this.Type) {
					case FunctionType.EncryptRecord: {
						await GameUtility.EncryptRecord(
							this.ArgumentOfEncryptRecordOfTargetDirectory,
							this.ArgumentOfEncryptRecordOfKey,
							PublishMessage
						);
						break;
					}
					case FunctionType.ModifyProgram: {
						await GameUtility.ModifyProgram(
							this.ArgumentOfModifyProgramOfTargetDirectory,
							this.ArgumentOfModifyProgramOfDisableRecordEncryption,
							this.ArgumentOfModifyProgramOfEnableDebugMode,
							Setting.Data.ProgramFileOfIl2CppDumper,
							PublishMessage
						);
						break;
					}
					default: throw new ();
				}
				PublishMessage($"Done.");
			}
			catch (Exception e) {
				App.MainWindow.PublishTip(InfoBarSeverity.Error, "Failed to run function.", "");
				PublishMessage($"Exception!");
				PublishMessage(e.ToString());
			}
			PublishMessage($"");
			this.Running = false;
			this.NotifyPropertyChanged(
				nameof(this.uRun_IsEnabled)
			);
			return;
			async void PublishMessage (
				String message
			) {
				this.Message += message + "\n";
				this.NotifyPropertyChanged(
					nameof(this.uMessage_Text)
				);
				await Task.Delay(40);
				this.View.uMessageScrollViewer.ChangeView(null, this.View.uMessageScrollViewer.ScrollableHeight, null, true);
				return;
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

	public enum FunctionType {
		EncryptRecord,
		ModifyProgram,
	}

}
