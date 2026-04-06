#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;

namespace KairosoftGameManager.Utility {

	public enum StoragePathType {
		Detached,
		Relative,
		Absolute,
	}

	public enum StoragePathStyle {
		Posix,
		Windows,
	}

	// ----------------

	public class StoragePath {

		#region constructor

		private StoragePathType mType;

		private String? mRoot;

		private List<String> mSegment;

		// ----------------

		public StoragePath(
		) {
			this.mType = StoragePathType.Detached;
			this.mRoot = null;
			this.mSegment = [];
			return;
		}

		public StoragePath(
			StoragePath other
		) {
			this.mType = other.mType;
			this.mRoot = other.mRoot;
			this.mSegment = other.mSegment.ToList();
			return;
		}

		public StoragePath(
			StoragePathType type
		) {
			this.mType = type;
			this.mRoot = null;
			this.mSegment = [];
			return;
		}

		public StoragePath(
			String text
		) {
			this.mType = StoragePathType.Detached;
			this.mRoot = null;
			this.mSegment = [];
			this.Parse(text);
			return;
		}

		#endregion

		#region equality

		public override Boolean Equals(
			Object? other
		) {
			return other is StoragePath that && this.mType == that.mType && this.mRoot == that.mRoot && this.mSegment.SequenceEqual(that.mSegment);
		}

		public override Size GetHashCode(
		) {
			return new List<Object?>([this.mType, this.mRoot, ..this.mSegment]).GetMixedHashCode();
		}

		// ----------------

		public static Boolean operator ==(
			StoragePath? self,
			Object?      other
		) => Object.Equals(self, other);

		public static Boolean operator !=(
			StoragePath? self,
			Object?      other
		) => !Object.Equals(self, other);

		#endregion

		#region query

		public StoragePathType Type(
		) {
			return this.mType;
		}

		public String? Root(
		) {
			return this.mRoot;
		}

		public List<String> Segment(
		) {
			return this.mSegment;
		}

		#endregion

		#region part

		public StoragePath? Parent(
		) {
			var result = null as StoragePath;
			if (!this.mSegment.IsEmpty()) {
				result = new ();
				result.mType = this.mType;
				result.mRoot = this.mRoot;
				result.mSegment.AddRange(this.mSegment.Slice(0, this.mSegment.Count - 1));
			}
			return result;
		}

		public String? Name(
		) {
			var result = null as String;
			if (!this.mSegment.IsEmpty()) {
				result = this.mSegment[^1];
			}
			return result;
		}

		public String? Stem(
		) {
			var result = null as String;
			if (!this.mSegment.IsEmpty()) {
				var segment = this.mSegment[^1];
				var position = segment.LastIndexOf('.');
				if (position != -1) {
					result = segment.Substring(0, position);
				}
				else {
					result = segment;
				}
			}
			return result;
		}

		public String? Extension(
		) {
			var result = null as String;
			if (!this.mSegment.IsEmpty()) {
				var segment = this.mSegment[^1];
				var position = segment.LastIndexOf('.');
				if (position != -1) {
					result = segment.Substring(position + 1);
				}
			}
			return result;
		}

		#endregion

		#region compose

		public StoragePath Join(
			String other
		) {
			var result = new StoragePath();
			result.mType = this.mType;
			result.mRoot = this.mRoot;
			result.mSegment.AddRange(this.mSegment);
			result.mSegment.Add(other);
			return result;
		}

		public StoragePath Push(
			StoragePath other
		) {
			AssertTest(other.mType != StoragePathType.Absolute);
			var result = new StoragePath();
			result.mType = this.mType;
			result.mRoot = this.mRoot;
			result.mSegment.AddRange(this.mSegment);
			result.mSegment.AddRange(other.mSegment);
			return result;
		}

		#endregion

		#region convert

		public void Parse(
			String text
		) {
			this.mType = StoragePathType.Detached;
			this.mRoot = null;
			this.mSegment = [];
			var position = 0;
			if (text.Length >= 2 && text[1] == ':' && ConvertHelper.IsLetter(text[0])) {
				this.mRoot = text.Substring(0, 2);
				position += 2;
			}
			if (text.Length > position && ConvertHelper.IsPathDot(text[position])) {
				var isRelative = false;
				var isParent = false;
				var offset = 1;
				if (text.Length > position + offset && ConvertHelper.IsPathDot(text[position + offset])) {
					isParent = true;
					offset += 1;
				}
				if (text.Length == position + offset) {
					isRelative = true;
				}
				else if (ConvertHelper.IsPathSeparator(text[position + offset])) {
					isRelative = true;
					offset += 1;
				}
				if (isRelative) {
					this.mType = StoragePathType.Relative;
					if (!isParent) {
						position += offset;
					}
				}
			}
			else if (text.Length > position && ConvertHelper.IsPathSeparator(text[position])) {
				this.mType = StoragePathType.Absolute;
				position += 1;
			}
			var location = position;
			for (; position <= text.Length; position++) {
				if (position == text.Length || ConvertHelper.IsPathSeparator(text[position])) {
					var segment = text.Substring(location, position - location);
					if (!segment.IsEmpty()) {
						this.mSegment.Add(segment);
					}
					location = position + 1;
				}
			}
			return;
		}

		public String Emit(
			StoragePathStyle style   = StoragePathStyle.Posix,
			Boolean          rectify = false
		) {
			var text = new StringBuilder();
			var dot = '.';
			var separator = style == StoragePathStyle.Posix ? '/' : '\\';
			if (this.mRoot != null) {
				text.Append(this.mRoot);
			}
			if (this.mType == StoragePathType.Relative) {
				text.Append(dot);
				text.Append(separator);
			}
			if (this.mType == StoragePathType.Absolute) {
				text.Append(separator);
			}
			for (var segmentIndex = 0; segmentIndex < this.mSegment.Count; segmentIndex++) {
				var segment = this.mSegment[segmentIndex];
				if (segmentIndex != 0) {
					text.Append(separator);
				}
				if (!rectify) {
					text.Append(segment);
				}
				else {
					if (style == StoragePathStyle.Posix) {
						text.Append(segment);
					}
					if (style == StoragePathStyle.Windows) {
						var segmentSize = segment.Length;
						while (segmentSize != 0) {
							if (segment[segmentSize - 1] == ' ') {
								segmentSize -= 1;
								continue;
							}
							if (segment[segmentSize - 1] == '.' && !((segmentSize == 1) || (segmentSize == 2 && segment[0] == '.'))) {
								segmentSize -= 1;
								continue;
							}
							break;
						}
						text.Append(segment.Substring(0, segmentSize));
					}
				}
			}
			return text.ToString();
		}

		// ----------------

		public String EmitPosix(
			Boolean rectify = false
		) {
			return this.Emit(StoragePathStyle.Posix, rectify);
		}

		public String EmitWindows(
			Boolean rectify = false
		) {
			return this.Emit(StoragePathStyle.Windows, rectify);
		}

		public String EmitNative(
		) {
			var mappedStyle = StoragePathStyle.Windows;
			return this.Emit(mappedStyle, true);
		}

		#endregion

	}

}
