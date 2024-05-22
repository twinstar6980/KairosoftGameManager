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

		public async void PublishNotification (
			InfoBarSeverity severity,
			String          title,
			String          message,
			Size            duration = 4000
		) => this.Controller.PublishNotification(severity, title, message, duration);

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

		public async void PublishNotification (
			InfoBarSeverity severity,
			String          title,
			String          message,
			Size            duration = 4000
		) {
			this.View.uNotificationsBehavior.Show(new () {
				Severity = severity,
				Title = title,
				Message = message,
				Duration = TimeSpan.FromMilliseconds(duration),
			});
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

	}

}
