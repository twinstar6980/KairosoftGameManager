#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;
using KairosoftGameManager.Utility;
using Windows.Foundation;

namespace KairosoftGameManager.Control {

	public abstract class Panel : Microsoft.UI.Xaml.Controls.Panel {

		#region measure & arrange

		protected override Windows.Foundation.Size MeasureOverride (
			Windows.Foundation.Size availableSize
		) {
			var parentPadding = this.Padding;
			var parentSpace = new Windows.Foundation.Size(Math.Max(0.0, availableSize.Width - (parentPadding.Left + parentPadding.Right)), Math.Max(0.0, availableSize.Height - (parentPadding.Top + parentPadding.Bottom)));
			var desiredSize = this.MeasureWithoutPadding(parentSpace);
			desiredSize.Width = Math.Min(availableSize.Width, Math.Max(0.0, desiredSize.Width + (parentPadding.Left + parentPadding.Right)));
			desiredSize.Height = Math.Min(availableSize.Height, Math.Max(0.0, desiredSize.Height + (parentPadding.Top + parentPadding.Bottom)));
			return desiredSize;
		}

		protected override Windows.Foundation.Size ArrangeOverride (
			Windows.Foundation.Size finalSize
		) {
			var parentPadding = this.Padding;
			var parentSpace = new Windows.Foundation.Size(Math.Max(0.0, finalSize.Width - (parentPadding.Left + parentPadding.Right)), Math.Max(0.0, finalSize.Height - (parentPadding.Top + parentPadding.Bottom)));
			var currentOffset = new Point(parentPadding.Left, parentPadding.Top);
			this.ArrangeWithoutPadding(currentOffset, parentSpace);
			return finalSize;
		}

		// ----------------

		protected abstract Windows.Foundation.Size MeasureWithoutPadding (
			Windows.Foundation.Size availableSize
		);

		protected abstract Windows.Foundation.Size ArrangeWithoutPadding (
			Windows.Foundation.Point finalOffset,
			Windows.Foundation.Size  finalSize
		);

		#endregion

		#region property

		public static readonly DependencyProperty PaddingProperty = DependencyProperty.Register(
			nameof(Panel.Padding),
			typeof(Thickness),
			typeof(Panel),
			new (new Thickness(0.0), (o, e) => { o.AsClass<Panel>().InvalidateMeasure(); })
		);

		public Thickness Padding {
			get => this.GetValue(Panel.PaddingProperty).AsStruct<Thickness>();
			set => this.SetValue(Panel.PaddingProperty, value);
		}

		#endregion

	}

}
