#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.Encodings.Web;

namespace KairosoftGameManager.Utility {

	public static class JsonHelper {

		#region serialization

		[UnconditionalSuppressMessage("Trimming", "IL2026")]
		[UnconditionalSuppressMessage("AOT", "IL3050")]
		public static JsonNode SerializeNode<TValue> (
			TValue value
		)
			where TValue : notnull {
			return JsonSerializer.SerializeToNode<TValue>(value, JsonHelper.Option).AsNotNull();
		}

		[UnconditionalSuppressMessage("Trimming", "IL2026")]
		[UnconditionalSuppressMessage("AOT", "IL3050")]
		public static TValue DeserializeNode<TValue> (
			JsonNode node
		)
			where TValue : notnull {
			return JsonSerializer.Deserialize<TValue>(node, JsonHelper.Option) ?? throw new NullReferenceException();
		}

		// ----------------

		[UnconditionalSuppressMessage("Trimming", "IL2026")]
		[UnconditionalSuppressMessage("AOT", "IL3050")]
		public static String SerializeText<TValue> (
			TValue  value,
			Boolean indented = true
		)
			where TValue : notnull {
			return JsonSerializer.Serialize<TValue>(value, indented ? JsonHelper.Option : JsonHelper.OptionNoIndented);
		}

		[UnconditionalSuppressMessage("Trimming", "IL2026")]
		[UnconditionalSuppressMessage("AOT", "IL3050")]
		public static TValue DeserializeText<TValue> (
			String text
		)
			where TValue : notnull {
			return JsonSerializer.Deserialize<TValue>(text, JsonHelper.Option) ?? throw new NullReferenceException();
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
			return JsonHelper.DeserializeNode<TValue>(JsonHelper.SerializeNode(value));
		}

		#endregion

		#region attribute

		[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
		public class JsonOptionalAttribute : JsonAttribute {
		}

		#endregion

		#region converter

		private class ObjectJsonConverter : JsonConverter<Object> {

			public override Boolean HandleNull => false;

			[UnconditionalSuppressMessage("Trimming", "IL2026")]
			[UnconditionalSuppressMessage("AOT", "IL3050")]
			public override Object Read (
				ref Utf8JsonReader    reader,
				Type                  typeToConvert,
				JsonSerializerOptions options
			) {
				return reader.TokenType switch {
					JsonTokenType.False                                                => false,
					JsonTokenType.True                                                 => true,
					JsonTokenType.Number when reader.TryGetInt64(out var valueInteger) => valueInteger,
					JsonTokenType.Number                                               => reader.GetDouble(),
					JsonTokenType.String                                               => reader.GetString().AsNotNull(),
					JsonTokenType.StartArray                                           => JsonSerializer.Deserialize<List<Object>>(ref reader, options).AsNotNull(),
					JsonTokenType.StartObject                                          => JsonSerializer.Deserialize<Dictionary<String, Object>>(ref reader, options).AsNotNull(),
					_                                                                  => throw new (),
				};
			}

			[UnconditionalSuppressMessage("Trimming", "IL2026")]
			[UnconditionalSuppressMessage("AOT", "IL3050")]
			public override void Write (
				Utf8JsonWriter        writer,
				Object                value,
				JsonSerializerOptions options
			) {
				JsonSerializer.Serialize(writer, value, value.GetType(), options);
				return;
			}

		}

		private class FloaterJsonConverter<TValue> : JsonConverter<TValue>
			where TValue : struct {

			public override Boolean HandleNull => false;

			public override TValue Read (
				ref Utf8JsonReader    reader,
				Type                  typeToConvert,
				JsonSerializerOptions options
			) {
				var value = default(TValue);
				if (value is FloaterS32) {
					value = reader.GetSingle().As<TValue>();
				}
				if (value is FloaterS64) {
					value = reader.GetDouble().As<TValue>();
				}
				return value;
			}

			public override void Write (
				Utf8JsonWriter        writer,
				TValue                value,
				JsonSerializerOptions options
			) {
				var text = value.ToString().AsNotNull();
				if (value is FloaterS32) {
					text += !FloaterS32.IsInteger(value.As<FloaterS32>()) ? "" : ".0";
				}
				if (value is FloaterS64) {
					text += !FloaterS64.IsInteger(value.As<FloaterS64>()) ? "" : ".0";
				}
				writer.WriteRawValue(text);
				return;
			}

		}

		private class TupleJsonConverter<TValue> : JsonConverter<TValue>
			where TValue : class, ITuple {

			public override Boolean HandleNull => false;

			[UnconditionalSuppressMessage("Trimming", "IL2026")]
			[UnconditionalSuppressMessage("Trimming", "IL2070")]
			[UnconditionalSuppressMessage("AOT", "IL3050")]
			public override TValue Read (
				ref Utf8JsonReader    reader,
				Type                  typeToConvert,
				JsonSerializerOptions options
			) {
				var types = typeof(TValue).GetGenericArguments();
				var values = Enumerable.Repeat<Object?>(null, types.Length).ToArray();
				AssertTest(reader.TokenType == JsonTokenType.StartArray);
				for (var index = 0; index < types.Length; index++) {
					reader.Read();
					values[index] = JsonSerializer.Deserialize(ref reader, types[index], options);
				}
				reader.Read();
				AssertTest(reader.TokenType == JsonTokenType.EndArray);
				return typeToConvert.GetConstructor(types).AsNotNull().Invoke(values).As<TValue>();
			}

			[UnconditionalSuppressMessage("Trimming", "IL2026")]
			[UnconditionalSuppressMessage("AOT", "IL3050")]
			public override void Write (
				Utf8JsonWriter        writer,
				TValue                value,
				JsonSerializerOptions options
			) {
				var types = typeof(TValue).GetGenericArguments();
				var values = value.As<ITuple>();
				writer.WriteStartArray();
				for (var index = 0; index < types.Length; index++) {
					JsonSerializer.Serialize(writer, values[index], types[index], options);
				}
				writer.WriteEndArray();
				return;
			}

		}

		private class TupleJsonConverterFactory : JsonConverterFactory {

			public override Boolean CanConvert (
				Type typeToConvert
			) {
				return ConvertHelper.IsTypeOfTuple(typeToConvert) || ConvertHelper.IsTypeOfValueTuple(typeToConvert);
			}

			public override JsonConverter CreateConverter (
				Type                  type,
				JsonSerializerOptions options
			) {
				return Activator.CreateInstance(typeof(TupleJsonConverter<>).MakeGenericType(type)).AsNotNull().As<JsonConverter>();
			}

		}

		#endregion

		#region option

		[UnconditionalSuppressMessage("AOT", "IL3050")]
		private static JsonSerializerOptions GetCustomJsonSerializerOptions (
			Boolean indented
		) {
			return new () {
				TypeInfoResolver = JsonSourceGenerationContext.Default.WithAddedModifier((typeInfo) => {
					if (typeInfo.Kind != JsonTypeInfoKind.Object) {
						return;
					}
					foreach (var propertyInfo in typeInfo.Properties) {
						propertyInfo.IsRequired = !propertyInfo.IsExtensionData && propertyInfo.AttributeProvider.AsNotNull().GetCustomAttributes(typeof(JsonOptionalAttribute), false).Length == 0;
					}
					return;
				}),
				Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
				WriteIndented = indented,
				IndentCharacter = '\t',
				IndentSize = 1,
				NewLine = "\n",
				ReadCommentHandling = JsonCommentHandling.Skip,
				AllowTrailingCommas = true,
				PropertyNameCaseInsensitive = false,
				PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
				DictionaryKeyPolicy = null,
				IncludeFields = true,
				DefaultIgnoreCondition = JsonIgnoreCondition.Never,
				RespectNullableAnnotations = true,
				UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
				NumberHandling = JsonNumberHandling.Strict,
				Converters = {
					new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower, false),
					new ObjectJsonConverter(),
					new FloaterJsonConverter<FloaterS32>(),
					new FloaterJsonConverter<FloaterS64>(),
					new TupleJsonConverterFactory(),
				},
			};
		}

		// ----------------

		private static readonly JsonSerializerOptions Option = JsonHelper.GetCustomJsonSerializerOptions(true);

		private static readonly JsonSerializerOptions OptionNoIndented = JsonHelper.GetCustomJsonSerializerOptions(false);

		#endregion

	}

}
