#pragma warning disable 0,

using KairosoftGameManager;
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;

namespace KairosoftGameManager.Utility {

	public static class VdfHelper {

		#region encoding

		public static String EncodeText(
			VProperty value
		) {
			return VdfConvert.Serialize(value);
		}

		public static VProperty DecodeText(
			String target
		) {
			return VdfConvert.Deserialize(target) ?? throw new NullReferenceException();
		}

		// ----------------

		public static async Task EncodeFile(
			StoragePath target,
			VProperty   value
		) {
			await StorageHelper.WriteFileText(target, VdfHelper.EncodeText(value));
			return;
		}

		public static async Task<VProperty> DecodeFile(
			StoragePath target
		) {
			return VdfHelper.DecodeText(await StorageHelper.ReadFileText(target));
		}

		#endregion

	}

}
