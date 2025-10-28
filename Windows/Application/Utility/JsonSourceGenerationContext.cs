#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;
using System.Text.Json.Serialization;

namespace KairosoftGameManager.Utility {

	[JsonSerializable(typeof(Object))]
	[JsonSerializable(typeof(Boolean))]
	[JsonSerializable(typeof(Integer))]
	[JsonSerializable(typeof(Floater))]
	[JsonSerializable(typeof(String))]
	[JsonSerializable(typeof(List<Object>))]
	[JsonSerializable(typeof(Dictionary<String, Object>))]
	[JsonSerializable(typeof(Dictionary<String, String>))]
	[JsonSerializable(typeof(SortedDictionary<String, List<String>>))]
	[JsonSerializable(typeof(ElementTheme), TypeInfoPropertyName = "ElementTheme")]
	[JsonSerializable(typeof(ExternalToolSetting), TypeInfoPropertyName = "KairosoftGameManager_ExternalToolSetting")]
	[JsonSerializable(typeof(SettingData), TypeInfoPropertyName = "KairosoftGameManager_SettingData")]
	public partial class JsonSourceGenerationContext : JsonSerializerContext {
	}

}
