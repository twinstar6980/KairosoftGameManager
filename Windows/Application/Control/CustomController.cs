#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;
using KairosoftGameManager.Utility;

namespace KairosoftGameManager {

	public partial class CustomController : INotifyPropertyChanged {

		#region implement

		public event PropertyChangedEventHandler? PropertyChanged;

		public void NotifyPropertyChanged (
			String[] propertyNameList
		) {
			foreach (var propertyName in propertyNameList) {
				this.PropertyChanged?.Invoke(this, new (propertyName));
			}
			return;
		}

		#endregion

	}

}
