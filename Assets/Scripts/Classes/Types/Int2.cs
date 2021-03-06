using System.ComponentModel;
using System.Globalization;
using UnityEngine;
using Newtonsoft.Json;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sperlich.Types {

	[System.Serializable]
	public struct Int2 : System.IEquatable<Int2> {
		public int x;
		public int y;

		public Int2(int _x, int _y) {
			x = _x;
			y = _y;
		}

		[JsonIgnore]
		public Int2 yx => new Int2(y, x);
		/// <summary>
		/// Converts Int2 to Vector3(x, 0, y)
		/// </summary>
		[JsonIgnore]
		public Vector3 xyz => new Vector3(x, 0, y);
		[JsonIgnore]
		public UnityEngine.Vector2 Vector2 => new UnityEngine.Vector2(x, y);
		/// <summary>
		/// Converts Int2 to Vector3(x, y, 0)
		/// </summary>
		[JsonIgnore]
		public UnityEngine.Vector3 Vector3 => new UnityEngine.Vector3(x, y, 0);
		public static implicit operator UnityEngine.Vector2(Int2 item) => new UnityEngine.Vector2(item.x, item.y);
		public static implicit operator UnityEngine.Vector2Int(Int2 item) => new UnityEngine.Vector2Int(item.x, item.y);

		public static Int2 operator +(Int2 item1, Int2 item2) => new Int2(item1.x + item2.x, item1.y + item2.y);
		public static Int2 operator -(Int2 item1, Int2 item2) => new Int2(item1.x - item2.x, item1.y - item2.y);
		public static Int2 operator *(Int2 item1, Int2 item2) => new Int2(item1.x * item2.x, item1.y * item2.y);
		public static Int2 operator *(Int2 item1, int multiplier) => new Int2(item1.x * multiplier, item1.y + multiplier);
		public static Int2 operator *(int multiplier, Int2 item1) => new Int2(item1.x * multiplier, item1.y + multiplier);
		public static Int2 operator /(Int2 item1, Int2 item2) => new Int2(item1.x / item2.x, item1.y / item2.y);
		public static Int2 operator /(Int2 item1, int multiplier) => new Int2(item1.x / multiplier, item1.y / multiplier);
		public static Int2 operator /(int multiplier, Int2 item1) => new Int2(item1.x / multiplier, item1.y / multiplier);
		public static bool operator ==(Int2 item1, Int2 item2) => item1.x == item2.x && item1.y == item2.y;
		public static bool operator !=(Int2 item1, Int2 item2) => !(item1 == item2);



		override
		public string ToString() => "[" + x + ", " + y + "]";

		public static string Int2ToString(int x, int y) => "[" + x + ", " + y + "]";

		public override bool Equals(object obj) => obj is Int2 @int && Equals(@int);

		public bool Equals(Int2 other) => x == other.x && y == other.y;

		public override int GetHashCode() {
			int hashCode = 1502939027;
			hashCode = hashCode * -1521134295 + x.GetHashCode();
			hashCode = hashCode * -1521134295 + y.GetHashCode();
			return hashCode;
		}
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(Int2))]
	public class Int2PropertyDrawer : PropertyDrawer {
		// Draw the property inside the given rect
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			// Using BeginProperty / EndProperty on the parent property means that
			// prefab override logic works on the entire property.
			EditorGUI.BeginProperty(position, label, property);

			// Draw label
			position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

			// Don't make child fields be indented
			var indent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			// Calculate rects
			var propX = new Rect(position.x, position.y, 50, position.height);
			var propY = new Rect(position.x + 55, position.y, 50, position.height);

			// Draw fields - pass GUIContent.none to each so they are drawn without labels
			EditorGUI.PropertyField(propX, property.FindPropertyRelative("x"), GUIContent.none);
			EditorGUI.PropertyField(propY, property.FindPropertyRelative("y"), GUIContent.none);

			// Set indent back to what it was
			EditorGUI.indentLevel = indent;

			EditorGUI.EndProperty();
		}
	}
#endif

	class Int2Converter : TypeConverter {
		public override bool CanConvertFrom(ITypeDescriptorContext context, System.Type sourceType) {
			if(sourceType == typeof(string))
				return true;
			return base.CanConvertFrom(context, sourceType);
		}

		public override bool CanConvertTo(ITypeDescriptorContext context, System.Type destinationType) {
			if(destinationType == typeof(string))
				return true;
			return base.CanConvertTo(context, destinationType);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
			if(value is string strValue) {
				if(strValue.Length >= 5) {
					string[] elements = strValue.Substring(1, strValue.Length - 2).Split(';');
					if(elements.Length == 2) {
						if(int.TryParse(elements[0], NumberStyles.Integer, culture.NumberFormat, out int x)) {
							if(int.TryParse(elements[1], NumberStyles.Integer, culture.NumberFormat, out int y)) {
								return new Int2() { x = x, y = y };
							}
						}
					}
				}
			}
			return base.ConvertFrom(context, culture, value);
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, System.Type destinationType) {
			Int2 int2 = (Int2)value;
			if(destinationType == typeof(string)) {
				return $"[{int2.x.ToString(culture.NumberFormat)};{int2.y.ToString(culture.NumberFormat)}]";
			}
			return base.ConvertTo(context, culture, value, destinationType);
		}
	}
}