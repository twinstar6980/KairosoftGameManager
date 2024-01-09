#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;
using KairosoftGameManager.Utility;
using Microsoft.UI.Windowing;
using Windows.ApplicationModel;
using Microsoft.UI.Xaml.Media.Animation;

namespace KairosoftGameManager.View {

	public partial class MainWindow : Window {

		#region life

		public MainWindow (
		) {
			this.InitializeComponent();
			this.ExtendsContentIntoTitleBar = true;
			this.SetTitleBar(this.uTitle.AsClass<UIElement>());
			this.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
			this.Controller = new () { View = this };
			this.Controller.Initialize();
		}

		// ----------------

		private MainWindowController Controller { get; }

		// ----------------

		public async void PublishTip (
			InfoBarSeverity severity,
			String          title,
			String          message,
			Size            duration = 4000
		) => this.Controller.PublishTip(severity, title, message, duration);

		#endregion

	}

	public class MainWindowController : CustomController {

		#region data

		public MainWindow View { get; init; } = default!;

		#endregion

		#region initialize

		public void Initialize (
		) {
			return;
		}

		// ----------------

		public async void PublishTip (
			InfoBarSeverity severity,
			String          title,
			String          message,
			Size            duration = 4000
		) {
			this.uTip_IsOpen = false;
			this.uTip_Severity = severity;
			this.uTip_Title = title;
			this.uTip_Message = message;
			this.NotifyPropertyChanged(
				nameof(this.uTip_IsOpen),
				nameof(this.uTip_Severity),
				nameof(this.uTip_Title),
				nameof(this.uTip_Message)
			);
			await Task.Delay(80);
			this.uTip_IsOpen = true;
			this.NotifyPropertyChanged(
				nameof(this.uTip_IsOpen)
			);
			this.uTip_vDelayCount++;
			await Task.Delay(duration);
			this.uTip_vDelayCount--;
			if (this.uTip_vDelayCount == 0) {
				this.uTip_IsOpen = false;
				this.NotifyPropertyChanged(
					nameof(this.uTip_IsOpen)
				);
			}
			return;
		}

		#endregion

		#region title

		public String uTitleText_Text {
			get {
				return Package.Current.DisplayName;
			}
		}

		#endregion

		#region navigation

		public async void uNavigation_SelectionChanged (
			NavigationView                          sender,
			NavigationViewSelectionChangedEventArgs args
		) {
			var senders = sender.AsClass<NavigationView>();
			if (args.SelectedItemContainer is not null) {
				var pageType = args.SelectedItemContainer.Tag.AsClass<String>() switch {
					"Manager"  => typeof(ManagerPage),
					"Function" => typeof(FunctionPage),
					"Setting"  => typeof(SettingPage),
					_          => throw new (),
				};
				senders.Content.AsClass<Frame>().NavigateToType(pageType, null, new () { IsNavigationStackEnabled = false, TransitionInfoOverride = new EntranceNavigationTransitionInfo() });
			}
			return;
		}

		#endregion

		#region tip

		public Boolean uTip_IsOpen { get; set; } = false;

		public InfoBarSeverity uTip_Severity { get; set; } = InfoBarSeverity.Informational;

		public String uTip_Title { get; set; } = "";

		public String uTip_Message { get; set; } = "";

		// ----------------

		public Size uTip_vDelayCount { get; set; } = 0;

		#endregion

	}

}
