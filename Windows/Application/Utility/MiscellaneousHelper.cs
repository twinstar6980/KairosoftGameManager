#pragma warning disable 0,

using KairosoftGameManager;

namespace KairosoftGameManager.Utility {

	public static class MiscellaneousHelper {

		#region storage

		public static async Task<StoragePath?> PickStorageItem(
			StoragePickType type,
			String          locationTag,
			String?         name
		) {
			var location = App.Instance.Setting.Data.StoragePickerHistoryLocation.GetValueOrDefault(locationTag);
			var target = await StorageHelper.Pick(type, location, name, App.Instance.MainWindow);
			if (target != null) {
				App.Instance.Setting.Data.StoragePickerHistoryLocation[locationTag] = type switch {
					StoragePickType.LoadFile      => target.Parent().AsNotNull(),
					StoragePickType.LoadDirectory => target,
					StoragePickType.SaveFile      => target.Parent().AsNotNull(),
					_                             => throw new UnreachableException(),
				};
				await App.Instance.Setting.Save(apply: false);
			}
			return target;
		}

		#endregion

	}

}
