#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace KairosoftGameManager.Utility {

	public static class JsonHelper {

		#region serialization

		private static readonly JsonSerializer Serializer = JsonSerializer.CreateDefault(new () {
			NullValueHandling = NullValueHandling.Include,
			Converters = [
				new StringEnumConverter() {
					NamingStrategy = new SnakeCaseNamingStrategy(),
				},
				new TupleJsonConverter(),
			],
			ContractResolver = new DefaultContractResolver() {
				NamingStrategy = new SnakeCaseNamingStrategy(),
			},
		});

		// ----------------

		public static JToken SerializeToken<TValue> (
			TValue value
		)
			where TValue : notnull {
			return JToken.FromObject(value, JsonHelper.Serializer);
		}

		public static TValue DeserializeToken<TValue> (
			JToken token
		)
			where TValue : notnull {
			return token.ToObject<TValue>(JsonHelper.Serializer) ?? throw new NullReferenceException();
		}

		// ----------------

		public static String SerializeText<TValue> (
			TValue  value,
			Boolean indented = true
		)
			where TValue : notnull {
			var text = new StringWriter(new (256), CultureInfo.InvariantCulture) {
				NewLine = "\n",
			};
			var writer = new JsonTextWriter(text) {
				Indentation = 1,
				IndentChar = '\t',
			};
			JsonHelper.Serializer.Formatting = indented ? Formatting.Indented : Formatting.None;
			JsonHelper.Serializer.Serialize(writer, value);
			return text.ToString();
		}

		public static TValue DeserializeText<TValue> (
			String text
		)
			where TValue : notnull {
			var reader = new JsonTextReader(new StringReader(text));
			return JsonHelper.Serializer.Deserialize<TValue>(reader) ?? throw new NullReferenceException();
		}

		// ----------------

		public static async Task SerializeFile<TValue> (
			String  path,
			TValue  value,
			Boolean indented = true
		)
			where TValue : notnull {
			await StorageHelper.WriteFileText(path, JsonHelper.SerializeText<TValue>(value, indented));
			return;
		}

		public static async Task<TValue> DeserializeFile<TValue> (
			String path
		)
			where TValue : notnull {
			return JsonHelper.DeserializeText<TValue>(await StorageHelper.ReadFileText(path));
		}

		// ----------------

		public static TValue DeepCopy<TValue> (
			TValue value
		)
			where TValue : notnull {
			return JsonHelper.DeserializeToken<TValue>(JsonHelper.SerializeToken(value));
		}

		#endregion

		#region converter

		public class TupleJsonConverter : JsonConverter {

			public override Boolean CanConvert (
				Type objectType
			) {
				return ConvertHelper.IsTypeOfTuple(objectType) || ConvertHelper.IsTypeOfValueTuple(objectType);
			}

			public override void WriteJson (
				JsonWriter     writer,
				Object?        value,
				JsonSerializer serializer
			) {
				if (value == null) {
					serializer.Serialize(writer, null);
					return;
				}
				var values = value.As<ITuple>();
				var types = value.GetType().GetGenericArguments();
				var token = new JArray(types.Select((type, index) => (values[index] == null ? null : JToken.FromObject(values[index].AsNotNull(), serializer))));
				serializer.Serialize(writer, token);
				return;
			}

			public override Object? ReadJson (
				JsonReader     reader,
				Type           objectType,
				Object?        existingValue,
				JsonSerializer serializer
			) {
				var token = serializer.Deserialize<JArray>(reader);
				if (token == null) {
					return null;
				}
				var types = objectType.GetGenericArguments();
				GF.AssertTest(token.Count == types.Length);
				var values = objectType.GetConstructor(types).AsNotNull().Invoke(types.Select((type, index) => (token[index].ToObject(type, serializer))).ToArray());
				return values;
			}

		}

		#endregion

	}

}
