#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;

namespace KairosoftGameManager.Utility {

	public class Wrapper<TValue>
		where TValue : notnull {

		#region constructor

		private WrapperData mData;

		// ----------------

		public Wrapper (
		) {
			this.mData = new ();
			return;
		}

		public Wrapper (
			TValue? value
		) {
			this.mData = new () { Value = value };
			return;
		}

		private Wrapper (
			WrapperData data
		) {
			this.mData = data;
			return;
		}

		#endregion

		#region query

		public TValue? Value {
			get => (TValue?)this.mData.Value;
			set => this.mData.Value = value;
		}

		// ----------------

		public Wrapper<TTarget> Cast<TTarget> (
		)
			where TTarget : class {
			return new (this.mData);
		}

		#endregion

	}

	public record WrapperData {
		public Object? Value = default!;
	}

}
