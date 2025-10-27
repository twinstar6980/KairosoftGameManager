#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;
using KairosoftGameManager.Utility;
using Windows.ApplicationModel;
using Microsoft.UI.Xaml.Media.Animation;

namespace KairosoftGameManager.View {

	public partial class MainWindow : Window {

		#region life

		private MainWindowController Controller { get; }

		// ----------------

		public MainWindow (
		) {
			this.InitializeComponent();
			WindowHelper.SetIcon(this, $"{App.PackageDirectory}/Asset/Logo.ico");
			WindowHelper.SetTitle(this, ApplicationInformation.Name);
			WindowHelper.SetTitleBar(this, true, this.uTitle, true);
			this.Controller = new () { View = this };
			this.Controller.InitializeView();
			return;
		}

		#endregion

		#region action

		public async Task PushNotification (
			InfoBarSeverity severity,
			String          title,
			String          message,
			Size            duration = 4000
		) {
			await this.Controller.PushNotification(severity, title, message, duration);
			return;
		}

		#endregion

	}

	public partial class MainWindowController : CustomController {

		#region data

		public MainWindow View { get; init; } = default!;

		#endregion

		#region life

		public void InitializeView (
		) {
			return;
		}

		#endregion

		#region action

		public async Task PushNotification (
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
				return ApplicationInformation.Name;
			}
		}

		#endregion

		#region navigation

		public async void uNavigation_SelectionChanged (
			NavigationView                          sender,
			NavigationViewSelectionChangedEventArgs args
		) {
			var senders = sender.As<NavigationView>();
			if (args.SelectedItemContainer != null) {
				var pageType = args.SelectedItemContainer.Tag.As<String>() switch {
					"Manager"  => typeof(ManagerPage),
					"Function" => typeof(FunctionPage),
					"Setting"  => typeof(SettingPage),
					_          => throw new UnreachableException(),
				};
				senders.Content.As<Frame>().NavigateToType(pageType, null, new () { IsNavigationStackEnabled = false, TransitionInfoOverride = new EntranceNavigationTransitionInfo() });
			}
			return;
		}

		#endregion

	}

}
