#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;

namespace KairosoftGameManager.Utility {

	public static class ControlHelper {

		#region common

		public static async Task WaitUntilLoaded (
			FrameworkElement target,
			Size             delay = 40
		) {
			while (!target.IsLoaded) {
				await Task.Delay(delay);
			}
			return;
		}

		#endregion

		#region dialog

		private static List<ContentDialog> CurrentDialog { get; } = [];

		private static async Task PushDialog (
			ContentDialog item
		) {
			ControlHelper.CurrentDialog.Add(item);
			if (ControlHelper.CurrentDialog.Count > 1) {
				ControlHelper.CurrentDialog[^2].Hide();
			}
			var semaphore = new SemaphoreSlim(0, 1);
			var index = ControlHelper.CurrentDialog.Count;
			item.Closed += (_, _) => {
				if (ControlHelper.CurrentDialog.Count == index) {
					semaphore.Release();
					ControlHelper.CurrentDialog.RemoveAt(index - 1);
					if (index > 1) {
						_ = ControlHelper.CurrentDialog[^1].ShowAsync();
					}
				}
				return;
			};
			_ = item.ShowAsync();
			await semaphore.WaitAsync();
			return;
		}

		// ----------------

		public static async Task ShowDialogSimple (
			UIElement root,
			String    title,
			Object?   content
		) {
			var dialog = new ContentDialog() {
				XamlRoot = root.XamlRoot,
				RequestedTheme = root.XamlRoot.Content.As<FrameworkElement>().RequestedTheme,
				Title = title,
				Content = content,
				CloseButtonText = "Close",
				DefaultButton = ContentDialogButton.Close,
			};
			await ControlHelper.PushDialog(dialog);
			return;
		}

		public static async Task ShowDialogFixed (
			UIElement          root,
			String             title,
			Object?            content,
			Tuple<Size, Size>? size = null
		) {
			var dialog = new ContentDialog() {
				XamlRoot = root.XamlRoot,
				RequestedTheme = root.XamlRoot.Content.As<FrameworkElement>().RequestedTheme,
				Resources = [
					new ("ContentDialogMinWidth", size?.Item1 ?? 720.0),
					new ("ContentDialogMaxWidth", size?.Item1 ?? 720.0),
					new ("ContentDialogMinHeight", size?.Item2 ?? 640.0),
					new ("ContentDialogMaxHeight", size?.Item2 ?? 640.0),
				],
				Title = title,
				Content = new ScrollViewer() {
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch,
					Padding = new (12, 0, 12, 0),
					IsTabStop = true,
					HorizontalScrollMode = ScrollMode.Disabled,
					HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
					VerticalScrollMode = ScrollMode.Enabled,
					VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
					Content = content,
				},
				CloseButtonText = "Close",
				DefaultButton = ContentDialogButton.Close,
			};
			await ControlHelper.PushDialog(dialog);
			return;
		}

		// ----------------

		public static async Task<Boolean> ShowDialogForPausing (
			UIElement root,
			String?   title,
			Object?   content
		) {
			var result = false;
			var dialog = new ContentDialog() {
				XamlRoot = root.XamlRoot,
				RequestedTheme = root.XamlRoot.Content.As<FrameworkElement>().RequestedTheme,
				Title = title ?? "Pausing ...",
				Content = content,
				PrimaryButtonText = "Continue",
				CloseButtonText = "Cancel",
				DefaultButton = ContentDialogButton.Primary,
			}.SelfAlso((it) => {
				it.Closed += (_, args) => {
					result = args.Result == ContentDialogResult.Primary;
					return;
				};
			});
			await ControlHelper.PushDialog(dialog);
			return result;
		}

		public static async Task<Func<Task>> ShowDialogForWaiting (
			UIElement root,
			String?   title,
			Object?   content
		) {
			var dialog = new ContentDialog() {
				XamlRoot = root.XamlRoot,
				RequestedTheme = root.XamlRoot.Content.As<FrameworkElement>().RequestedTheme,
				Title = title ?? "Waiting ...",
				Content = content ?? new ProgressBar() {
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Center,
					IsIndeterminate = true,
				},
				CloseButtonText = "Hide",
				DefaultButton = ContentDialogButton.None,
			};
			var task = ControlHelper.PushDialog(dialog);
			return async () => {
				dialog.Hide();
				await task;
				return;
			};
		}

		#endregion

	}

}
