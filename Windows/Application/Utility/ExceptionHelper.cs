#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;

namespace KairosoftGameManager.Utility {

	public static class ExceptionHelper {

		#region utility

		private static Boolean Initialized { get; set; } = false;

		private static Action<Exception>? Handler { get; set; } = null;

		// ----------------

		public static void Initialize (
			App application
		) {
			AssertTest(!ExceptionHelper.Initialized);
			application.UnhandledException += async (_, args) => {
				args.Handled = true;
				ExceptionHelper.Handler?.Invoke(args.Exception);
				return;
			};
			TaskScheduler.UnobservedTaskException += async (_, args) => {
				args.SetObserved();
				ExceptionHelper.Handler?.Invoke(args.Exception);
				return;
			};
			ExceptionHelper.Initialized = true;
			return;
		}

		public static void Listen (
			Action<Exception>? handler
		) {
			AssertTest(ExceptionHelper.Initialized);
			ExceptionHelper.Handler = handler;
			return;
		}

		// ----------------

		public static Task WrapTask (
			Task task
		) {
			return task.ContinueWith((it) => {
				if (it.Exception?.InnerException != null) {
					ExceptionHelper.Handler?.Invoke(it.Exception.InnerException);
				}
				return;
			});
		}

		public static String GenerateMessage (
			Exception exception
		) {
			var message = $"{exception.Message}";
			var stack = new StackTrace(exception);
			if (exception.StackTrace != null) {
				foreach (var frame in exception.StackTrace.Split(Environment.NewLine)) {
					if (!frame.StartsWith("   at ")) {
						continue;
					}
					message += $"\n@ {frame.Substring("   at ".Length)}";
				}
			}
			return message;
		}

		#endregion

	}

}
