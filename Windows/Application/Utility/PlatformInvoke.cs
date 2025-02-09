#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;

namespace KairosoftGameManager.Utility {

	public static class PlatformInvoke {

		public static class Shell32 {

			public const Int32 SW_SHOWNORMAL = 1;

			// ----------------

			[DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
			public static extern IntPtr ShellExecute (
				IntPtr  hwnd,
				String? lpOperation,
				String  lpFile,
				String? lpParameters,
				String? lpDirectory,
				Int32   nShowCmd
			);

		}

		public static class User32 {

			[DllImport("User32.dll", CharSet = CharSet.Unicode)]
			public static extern UInt32 GetDpiForWindow (
				IntPtr hWnd
			);

		}

	}

}
