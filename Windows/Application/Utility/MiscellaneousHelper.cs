#pragma warning disable 0,

using KairosoftGameManager;

namespace KairosoftGameManager.Utility {

	public static class MiscellaneousHelper {

		#region storage

		public static async Task<List<StoragePath>> PickStorageItem(
			String          tag,
			StoragePickType type,
			Boolean         multiply,
			StoragePath?    location,
			String?         name
		) {
			if (location == null) {
				location = App.Instance.Setting.Data.StoragePickerHistoryLocation.GetValueOrDefault(tag);
			}
			var target = await StorageHelper.Pick(type, multiply, location, name, App.Instance.MainWindow);
			if (!target.IsEmpty()) {
				App.Instance.Setting.Data.StoragePickerHistoryLocation[tag] = type switch {
					StoragePickType.LoadFile      => target.First().Parent().AsNotNull(),
					StoragePickType.LoadDirectory => target.First(),
					StoragePickType.SaveFile      => target.First().Parent().AsNotNull(),
					_                             => throw new UnreachableException(),
				};
				await App.Instance.Setting.Save(apply: false);
			}
			return target;
		}

		#endregion

	}

}
