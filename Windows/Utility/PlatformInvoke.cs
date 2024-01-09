#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;

namespace KairosoftGameManager.Utility {

	public static class PlatformInvoke {

		public static class User32 {

			[DllImport("User32.dll", CharSet = CharSet.Unicode)]
			public static extern UInt32 GetDpiForWindow (
				IntPtr hWnd
			);

		}

	}

}
