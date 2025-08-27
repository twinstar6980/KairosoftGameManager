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

		private static List<ContentDialog> Dialog { get; } = [];

		private static async Task<ContentDialogResult> PushDialog (
			ContentDialog item
		) {
			ControlHelper.Dialog.Add(item);
			if (ControlHelper.Dialog.Count > 1) {
				ControlHelper.Dialog[^2].Hide();
			}
			var semaphore = new SemaphoreSlim(0, 1);
			var result = ContentDialogResult.None;
			var index = ControlHelper.Dialog.Count;
			item.Closed += (sender, args) => {
				if (ControlHelper.Dialog.Count == index) {
					result = args.Result;
					semaphore.Release();
					ControlHelper.Dialog.RemoveAt(index - 1);
					if (index > 1) {
						_ = ControlHelper.Dialog[^1].ShowAsync().AsTask().SelfLet(ExceptionHelper.WithTaskExceptionHandler);
					}
				}
				return;
			};
			_ = item.ShowAsync().AsTask().SelfLet(ExceptionHelper.WithTaskExceptionHandler);
			await semaphore.WaitAsync();
			return result;
		}

		public static async Task IterateDialog (
			Func<ContentDialog, Task> action
		) {
			foreach (var item in ControlHelper.Dialog) {
				await action(item);
			}
			return;
		}

		// ----------------

		public static async Task<ContentDialogResult> ShowDialogAsAutomatic (
			UIElement                         root,
			String                            title,
			Object?                           content,
			Tuple<String?, String?, String?>? action
		) {
			var dialog = new ContentDialog() {
				XamlRoot = root.XamlRoot,
				RequestedTheme = root.XamlRoot.Content.As<FrameworkElement>().RequestedTheme,
				Title = title,
				Content = content == null
					? null
					: new ScrollViewer() {
						HorizontalAlignment = HorizontalAlignment.Stretch,
						VerticalAlignment = VerticalAlignment.Stretch,
						Margin = new (-16, 0, -16, 0),
						Padding = new (16, 0, 16, 0),
						IsTabStop = true,
						HorizontalScrollMode = ScrollMode.Disabled,
						HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
						VerticalScrollMode = ScrollMode.Enabled,
						VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
						Content = content,
					},
				CloseButtonText = action == null ? "Close" : action.Item1,
				PrimaryButtonText = action?.Item2,
				SecondaryButtonText = action?.Item3,
				DefaultButton = action == null
					? ContentDialogButton.Close
					: action.Item2 != null
						? ContentDialogButton.Primary
						: action.Item3 != null
							? ContentDialogButton.Secondary
							: ContentDialogButton.Close,
			};
			return await ControlHelper.PushDialog(dialog);
		}

		// ----------------

		public static async Task<Func<Task>> ShowDialogForWait (
			UIElement root
		) {
			var dialog = new ContentDialog() {
				XamlRoot = root.XamlRoot,
				RequestedTheme = root.XamlRoot.Content.As<FrameworkElement>().RequestedTheme,
				Title = "Waiting ...",
				Content = new ProgressBar() {
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

		public static async Task<Boolean> ShowDialogForConfirm (
			UIElement root,
			String?   title,
			Object?   content
		) {
			return await ControlHelper.ShowDialogAsAutomatic(root, title ?? "Confirm ?", content, new ("Cancel", "Continue", null)) == ContentDialogResult.Primary;
		}

		#endregion

	}

}
