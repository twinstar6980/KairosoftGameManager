#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;

namespace KairosoftGameManager.Utility {

	public partial class Finalizer : IDisposable, IAsyncDisposable {

		#region constructor

		private Func<Task> mAction;

		// ----------------

		public Finalizer (
			Func<Task> action
		) {
			this.mAction = action;
			return;
		}

		#endregion

		#region implement IDisposable

		public void Dispose (
		) {
			this.mAction();
			GC.SuppressFinalize(this);
			return;
		}

		#endregion

		#region implement IAsyncDisposable

		public async ValueTask DisposeAsync (
		) {
			await this.mAction();
			GC.SuppressFinalize(this);
			return;
		}

		#endregion

	}

}
