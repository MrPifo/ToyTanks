using System.ComponentModel;
using System.Globalization;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sperlich.Types {

	[System.Serializable]
	public struct Float2 : System.IEquatable<Float2> {
		public float x;
		public float y;

		public Float2(float _x, float _y) {
			x = _x;
			y = _y;
		}

		public Float2(Vector2 vector2, int round = -1) {
			if(round <= 0) {
				x = vector2.x;
				y = vector2.y;
			} else {
				x = Mathf.Round(vector2.x * Mathf.Pow(10, round)) / Mathf.Pow(10, round);
				y = Mathf.Round(vector2.y * Mathf.Pow(10, round)) / Mathf.Pow(10, round);
			}
		}

		public Int2 xy => new Int2((int)x, (int)y);
		public Int2 yx => new Int2((int)y, (int)x);
		public UnityEngine.Vector2 Vector2 => new UnityEngine.Vector2(x, y);

		public static implicit operator Vector2(Float2 item) => new Vector2(item.x, item.y);
		public static implicit operator Vector2Int(Float2 item) => new Vector2Int(Mathf.RoundToInt(item.x), Mathf.RoundToInt(item.y));
		public static implicit operator Int2(Float2 item) => new Int2(Mathf.RoundToInt(item.x), Mathf.RoundToInt(item.y));

		public static Float2 operator +(Float2 item1, Float2 item2) => new Float2(item1.x + item2.x, item1.y + item2.y);
		public static Float2 operator -(Float2 item1, Float2 item2) => new Float2(item1.x - item2.x, item1.y - item2.y);
		public static Float2 operator *(Float2 item1, Float2 item2) => new Float2(item1.x * item2.x, item1.y * item2.y);
		public static Float2 operator *(Float2 item1, float multiplier) => new Float2(item1.x * multiplier, item1.y + multiplier);
		public static Float2 operator *(float multiplier, Float2 item1) => new Float2(item1.x * multiplier, item1.y + multiplier);
		public static Float2 operator /(Float2 item1, Float2 item2) => new Float2(item1.x / item2.x, item1.y / item2.y);
		public static Float2 operator /(Float2 item1, float multiplier) => new Float2(item1.x / multiplier, item1.y / multiplier);
		public static Float2 operator /(float multiplier, Float2 item1) => new Float2(item1.x / multiplier, item1.y / multiplier);
		public static bool operator ==(Float2 item1, Float2 item2) => item1.x == item2.x && item1.y == item2.y;
		public static bool operator !=(Float2 item1, Float2 item2) => !(item1 == item2);



		override
		public string ToString() => "[" + x + ", " + y + "]";

		public static string Float2ToString(int x, int y) => "[" + x + ", " + y + "]";

		public override bool Equals(object obj) => obj is Float2 @int && Equals(@int);

		public bool Equals(Float2 other) => x == other.x && y == other.y;

		public override int GetHashCode() {
			int hashCode = 373119290;
			hashCode = hashCode * -1521134295 + x.GetHashCode();
			hashCode = hashCode * -1521134295 + y.GetHashCode();
			return hashCode;
		}
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(Float2))]
	public class Float2PropertyDrawer : PropertyDrawer {
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

	class Float2Converter : TypeConverter {
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
						if(float.TryParse(elements[0], NumberStyles.Float, culture.NumberFormat, out float x)) {
							if(float.TryParse(elements[1], NumberStyles.Float, culture.NumberFormat, out float y)) {
								return new Float2() { x = x, y = y };
							}
						}
					}
				}
			}
			return base.ConvertFrom(context, culture, value);
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, System.Type destinationType) {
			Float2 float2 = (Float2)value;
			if(destinationType == typeof(string)) {
				return $"[{float2.x.ToString(culture.NumberFormat)};{float2.y.ToString(culture.NumberFormat)}]";
			}
			return base.ConvertTo(context, culture, value, destinationType);
		}
	}
}