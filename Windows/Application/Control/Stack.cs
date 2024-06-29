#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;
using KairosoftGameManager.Utility;
using Windows.Foundation;

namespace KairosoftGameManager.Control {

	public class Stack : Panel {

		#region measure & arrange

		protected override Windows.Foundation.Size MeasureWithoutPadding (
			Windows.Foundation.Size availableSize
		) {
			var desiredSize = new Windows.Foundation.Size(0.0, 0.0);
			var isHorizontal = this.Orientation == Orientation.Horizontal;
			var childSpacing = this.Spacing;
			var totalSpacing = childSpacing * Math.Max(0, this.Children.Count - 1);
			var totalWeight = 0.0;
			var perSpace = 0.0;
			if (isHorizontal) {
				desiredSize.Width += totalSpacing;
			}
			else {
				desiredSize.Height += totalSpacing;
			}
			foreach (var child in this.Children) {
				var childWeight = Stack.GetWeight(child);
				if (!Floater.IsNaN(childWeight)) {
					totalWeight += childWeight;
					continue;
				}
				if (isHorizontal) {
					child.Measure(new (Math.Max(0.0, availableSize.Width - desiredSize.Width), availableSize.Height));
					desiredSize.Height = Math.Max(desiredSize.Height, child.DesiredSize.Height);
					desiredSize.Width += child.DesiredSize.Width;
				}
				else {
					child.Measure(new (availableSize.Width, Math.Max(0.0, availableSize.Height - desiredSize.Height)));
					desiredSize.Width = Math.Max(desiredSize.Width, child.DesiredSize.Width);
					desiredSize.Height += child.DesiredSize.Height;
				}
			}
			if (totalWeight != 0.0) {
				if (isHorizontal) {
					perSpace = Math.Max(0.0, availableSize.Width - desiredSize.Width) / totalWeight;
				}
				else {
					perSpace = Math.Max(0.0, availableSize.Height - desiredSize.Height) / totalWeight;
				}
			}
			foreach (var child in this.Children) {
				var childWeight = Stack.GetWeight(child);
				if (Floater.IsNaN(childWeight)) {
					continue;
				}
				var childSpace = perSpace * childWeight;
				if (isHorizontal) {
					child.Measure(new (childSpace, availableSize.Height));
					desiredSize.Height = Math.Max(desiredSize.Height, child.DesiredSize.Height);
					desiredSize.Width += child.DesiredSize.Width;
				}
				else {
					child.Measure(new (availableSize.Width, childSpace));
					desiredSize.Width = Math.Max(desiredSize.Width, child.DesiredSize.Width);
					desiredSize.Height += child.DesiredSize.Height;
				}
			}
			return new (Math.Min(availableSize.Width, desiredSize.Width), Math.Min(availableSize.Height, desiredSize.Height));
		}

		protected override Windows.Foundation.Size ArrangeWithoutPadding (
			Windows.Foundation.Point finalOffset,
			Windows.Foundation.Size  finalSize
		) {
			var desiredSize = new Windows.Foundation.Size(0.0, 0.0);
			var isHorizontal = this.Orientation == Orientation.Horizontal;
			var childSpacing = this.Spacing;
			var totalSpacing = childSpacing * Math.Max(0, this.Children.Count - 1);
			var totalWeight = 0.0;
			var perSpace = 0.0;
			if (isHorizontal) {
				desiredSize.Width += totalSpacing;
			}
			else {
				desiredSize.Height += totalSpacing;
			}
			foreach (var child in this.Children) {
				var childWeight = Stack.GetWeight(child);
				if (!Floater.IsNaN(childWeight)) {
					totalWeight += childWeight;
					continue;
				}
				if (isHorizontal) {
					desiredSize.Width += child.DesiredSize.Width;
				}
				else {
					desiredSize.Height += child.DesiredSize.Height;
				}
			}
			if (totalWeight != 0.0) {
				if (isHorizontal) {
					perSpace = Math.Max(0.0, finalSize.Width - desiredSize.Width) / totalWeight;
				}
				else {
					perSpace = Math.Max(0.0, finalSize.Height - desiredSize.Height) / totalWeight;
				}
			}
			var currentOffset = new Point(finalOffset.X, finalOffset.Y);
			foreach (var child in this.Children) {
				var childSize = new Windows.Foundation.Size();
				var childWeight = Stack.GetWeight(child);
				if (Floater.IsNaN(childWeight)) {
					if (isHorizontal) {
						childSize.Width = child.DesiredSize.Width;
						childSize.Height = finalSize.Height;
					}
					else {
						childSize.Height = child.DesiredSize.Height;
						childSize.Width = finalSize.Width;
					}
				}
				else {
					var childSpace = perSpace * childWeight;
					if (isHorizontal) {
						childSize.Width = childSpace;
						childSize.Height = finalSize.Height;
					}
					else {
						childSize.Height = childSpace;
						childSize.Width = finalSize.Width;
					}
				}
				child.Arrange(new (currentOffset, childSize));
				if (isHorizontal) {
					currentOffset.X += childSize.Width + childSpacing;
				}
				else {
					currentOffset.Y += childSize.Height + childSpacing;
				}
			}
			return finalSize;
		}

		#endregion

		#region property

		public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
			nameof(Stack.Orientation),
			typeof(Orientation),
			typeof(Stack),
			new (Orientation.Horizontal, (o, e) => { o.As<Stack>().InvalidateMeasure(); })
		);

		public Orientation Orientation {
			get => this.GetValue(Stack.OrientationProperty).As<Orientation>();
			set => this.SetValue(Stack.OrientationProperty, value);
		}

		// ----------------

		public static readonly DependencyProperty SpacingProperty = DependencyProperty.Register(
			nameof(Stack.Spacing),
			typeof(Floater),
			typeof(Stack),
			new (0.0, (o, e) => { o.As<Stack>().InvalidateMeasure(); })
		);

		public Floater Spacing {
			get => this.GetValue(Stack.SpacingProperty).As<Floater>();
			set => this.SetValue(Stack.SpacingProperty, value);
		}

		// ----------------

		public static readonly DependencyProperty WeightProperty = DependencyProperty.RegisterAttached(
			"Weight",
			typeof(Floater),
			typeof(Stack),
			new (Floater.NaN, (o, e) => { o.As<FrameworkElement>().Parent?.As<Stack>()?.InvalidateMeasure(); })
		);

		public static Floater GetWeight (
			DependencyObject element
		) => element.GetValue(Stack.WeightProperty).As<Floater>();

		public static void SetWeight (
			DependencyObject element,
			Floater          value
		) => element.SetValue(Stack.WeightProperty, value);

		#endregion

	}

}
