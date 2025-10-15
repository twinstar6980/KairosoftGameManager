#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;
using KairosoftGameManager.Utility;

namespace KairosoftGameManager.Control {

	public sealed partial class DividerProgress : Microsoft.UI.Xaml.Controls.Control {

		#region life

		public DividerProgress (
		) {
			this.DefaultStyleKey = typeof(DividerProgress);
			return;
		}

		#endregion

		#region property

		public static readonly DependencyProperty ProgressValueProperty = DependencyProperty.Register(
			nameof(DividerProgress.ProgressValue),
			typeof(Floater),
			typeof(DividerProgress),
			new (0.0)
		);

		public Floater ProgressValue {
			get => this.GetValue(DividerProgress.ProgressValueProperty).As<Floater>();
			set => this.SetValue(DividerProgress.ProgressValueProperty, value);
		}

		// ----------------

		public static readonly DependencyProperty ProgressIndeterminateProperty = DependencyProperty.Register(
			nameof(DividerProgress.ProgressIndeterminate),
			typeof(Boolean),
			typeof(DividerProgress),
			new (false)
		);

		public Boolean ProgressIndeterminate {
			get => this.GetValue(DividerProgress.ProgressIndeterminateProperty).As<Boolean>();
			set => this.SetValue(DividerProgress.ProgressIndeterminateProperty, value);
		}

		// ----------------

		public static readonly DependencyProperty ProgressPausedProperty = DependencyProperty.Register(
			nameof(DividerProgress.ProgressPaused),
			typeof(Boolean),
			typeof(DividerProgress),
			new (false)
		);

		public Boolean ProgressPaused {
			get => this.GetValue(DividerProgress.ProgressPausedProperty).As<Boolean>();
			set => this.SetValue(DividerProgress.ProgressPausedProperty, value);
		}

		// ----------------

		public static readonly DependencyProperty ProgressErrorProperty = DependencyProperty.Register(
			nameof(DividerProgress.ProgressError),
			typeof(Boolean),
			typeof(DividerProgress),
			new (false)
		);

		public Boolean ProgressError {
			get => this.GetValue(DividerProgress.ProgressErrorProperty).As<Boolean>();
			set => this.SetValue(DividerProgress.ProgressErrorProperty, value);
		}

		#endregion

	}

}
