#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;

namespace KairosoftGameManager.Utility {

	public partial class DisposableWrapper : IDisposable {

		#region structor

		private Action mAction;

		// ----------------

		public DisposableWrapper (
			Action action
		) {
			this.mAction = action;
		}

		#endregion

		#region implement

		public void Dispose (
		) {
			this.mAction();
			GC.SuppressFinalize(this);
			return;
		}

		#endregion

	}

}
