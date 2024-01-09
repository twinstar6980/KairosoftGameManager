#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;

namespace KairosoftGameManager.Utility {

	public static class WindowHelper {

		#region utility

		public static List<Window> Current { get; } = [];

		// ----------------

		public static void Track (
			Window window
		) {
			window.Closed += (_, _) => { WindowHelper.Current.Remove(window); };
			WindowHelper.Current.Add(window);
			return;
		}

		public static Window Find (
			UIElement element
		) {
			return WindowHelper.Current.Find((value) => (Object.ReferenceEquals(value.Content.XamlRoot, element.XamlRoot))) ?? throw new ("Could not find window.");
		}

		// ----------------

		public static IntPtr Handle (
			Window window
		) {
			return WinRT.Interop.WindowNative.GetWindowHandle(window);
		}

		// ----------------

		public static void Title (
			Window window,
			String title
		) {
			window.AppWindow.Title = title;
			return;
		}

		public static void Icon (
			Window window,
			String icon
		) {
			window.AppWindow.SetIcon(icon);
			return;
		}

		// ----------------

		public static void Size (
			Window window,
			Size   width,
			Size   height
		) {
			var ratio = PlatformInvoke.User32.GetDpiForWindow(WindowHelper.Handle(window)) / 96.0;
			window.AppWindow.Resize(new ((Int32)(width * ratio), (Int32)(height * ratio)));
			return;
		}

		// ----------------

		public static void Theme (
			Window       window,
			ElementTheme theme
		) {
			if (window.Content is FrameworkElement content) {
				content.RequestedTheme = theme;
			}
			return;
		}

		// ----------------

		public static void Activate (
			Window window
		) {
			window.Activate();
			return;
		}

		#endregion

	}

}
