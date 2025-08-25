#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;

namespace KairosoftGameManager.Utility {

	public static class WindowHelper {

		#region utility

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
			var ratio = Win32.PInvoke.GetDpiForWindow(new (WindowHelper.Handle(window))) / 96.0;
			window.AppWindow.Resize(new ((width * ratio).CastPrimitive<Int32>(), (height * ratio).CastPrimitive<Int32>()));
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
