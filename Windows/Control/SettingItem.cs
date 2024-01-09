#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;
using KairosoftGameManager.Utility;

namespace KairosoftGameManager.Control {

	public sealed class SettingItem : Microsoft.UI.Xaml.Controls.Control {

		#region life

		public SettingItem (
		) {
			this.DefaultStyleKey = typeof(SettingItem);
		}

		#endregion

		#region property

		public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
			nameof(SettingItem.Icon),
			typeof(String),
			typeof(SettingItem),
			new ("")
		);

		public String Icon {
			get => this.GetValue(SettingItem.IconProperty).AsClass<String>();
			set => this.SetValue(SettingItem.IconProperty, value);
		}

		// ----------------

		public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
			nameof(SettingItem.Title),
			typeof(String),
			typeof(SettingItem),
			new ("")
		);

		public String Title {
			get => this.GetValue(SettingItem.TitleProperty).AsClass<String>();
			set => this.SetValue(SettingItem.TitleProperty, value);
		}

		// ----------------

		public static readonly DependencyProperty ActionProperty = DependencyProperty.Register(
			nameof(SettingItem.Action),
			typeof(UIElement),
			typeof(SettingItem),
			new (new Box())
		);

		public UIElement Action {
			get => this.GetValue(SettingItem.ActionProperty).AsClass<UIElement>();
			set => this.SetValue(SettingItem.ActionProperty, value);
		}

		#endregion

	}

}
