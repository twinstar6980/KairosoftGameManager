#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;

namespace KairosoftGameManager.Utility {

	public class ApplicationExceptionManager {

		#region singleton

		public static readonly ApplicationExceptionManager Instance = new ();

		#endregion

		#region constructor

		private Boolean mInitialized;

		private Action<Exception>? mHandler;

		// ----------------

		private ApplicationExceptionManager(
		) {
			this.mInitialized = false;
			this.mHandler = null;
			return;
		}

		#endregion

		#region utility

		public async Task Initialize(
			App application
		) {
			AssertTest(!this.mInitialized);
			application.UnhandledException += async (_, args) => {
				args.Handled = true;
				this.mHandler?.Invoke(args.Exception);
				return;
			};
			TaskScheduler.UnobservedTaskException += async (_, args) => {
				args.SetObserved();
				this.mHandler?.Invoke(args.Exception);
				return;
			};
			this.mInitialized = true;
			return;
		}

		public async Task Listen(
			Action<Exception>? handler
		) {
			AssertTest(this.mInitialized);
			this.mHandler = handler;
			return;
		}

		// ----------------

		public Task WrapTask(
			Task task
		) {
			return task.ContinueWith((it) => {
				if (it.Exception?.InnerException != null) {
					this.mHandler?.Invoke(it.Exception.InnerException);
				}
				return;
			});
		}

		#endregion

	}

}
