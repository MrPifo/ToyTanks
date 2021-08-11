using System.ComponentModel;
using System.Globalization;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sperlich.Types {

	[System.Serializable]
	public struct Int3 : System.IEquatable<Int3> {
		public int x;
		public int y;
		public int z;

		public Int3(int _x, int _y, int _z) {
			x = _x;
			y = _y;
			z = _z;
		}

		public Int3(Vector3 vector3) {
			x = Mathf.RoundToInt(vector3.x);
			y = Mathf.RoundToInt(vector3.y);
			z = Mathf.RoundToInt(vector3.z);
		}

		public Int2 xy => new Int2(x, y);
		public Int2 xz => new Int2(x, z);
		public Int3 xyz => new Int3(x, y, z);
		public Int3 xzy => new Int3(x, z, y);
		public Int2 yz => new Int2(y, z);
		public Int2 yx => new Int2(y, x);
		public Int3 yxz => new Int3(y, x, z);
		public Int3 yzx => new Int3(y, z, x);
		public Int2 zx => new Int2(z, x);
		public Int2 zy => new Int2(z, y);
		public Int3 zxy => new Int3(z, x, y);
		public Int3 zyx => new Int3(z, y, z);
		public UnityEngine.Vector3 Vector3 => new UnityEngine.Vector3(x, y, z);

		public static implicit operator UnityEngine.Vector3(Int3 item) => new UnityEngine.Vector3(item.x, item.y, item.z);
		public static implicit operator UnityEngine.Vector3Int(Int3 item) => new UnityEngine.Vector3Int(item.x, item.y, item.z);

		public static Int3 operator +(Int3 item1, Int3 item2) => new Int3(item1.x + item2.x, item1.y + item2.y, item1.z + item2.z);
		public static Int3 operator -(Int3 item1, Int3 item2) => new Int3(item1.x - item2.x, item1.y - item2.y, item1.z - item2.z);
		public static Int3 operator *(Int3 item1, Int3 item2) => new Int3(item1.x * item2.x, item1.y * item2.y, item1.z * item2.z);
		public static Int3 operator *(Int3 item1, int multiplier) => new Int3(item1.x * multiplier, item1.y + multiplier, item1.z * multiplier);
		public static Int3 operator *(int multiplier, Int3 item1) => new Int3(item1.x * multiplier, item1.y + multiplier, item1.z * multiplier);
		public static Int3 operator /(Int3 item1, Int3 item2) => new Int3(item1.x / item2.x, item1.y / item2.y, item1.z / item2.z);
		public static Int3 operator /(Int3 item1, int multiplier) => new Int3(item1.x / multiplier, item1.y / multiplier, item1.z / multiplier);
		public static Int3 operator /(int multiplier, Int3 item1) => new Int3(item1.x / multiplier, item1.y / multiplier, item1.z / multiplier);
		public static bool operator ==(Int3 item1, Int3 item2) => item1.x == item2.x && item1.y == item2.y && item1.z == item2.z;
		public static bool operator !=(Int3 item1, Int3 item2) => !(item1 == item2);



		override
		public string ToString() => "[" + x + ", " + y + ", " + z + "]";

		public static string Int3ToString(int x, int y, int z) => "[" + x + ", " + y + ", " + z + "]";

		public override bool Equals(object obj) => obj is Int3 @int && Equals(@int);

		public bool Equals(Int3 other) => x == other.x && y == other.y && z == other.z;

		public override int GetHashCode() {
			int hashCode = 373119288;
			hashCode = hashCode * -1521134295 + x.GetHashCode();
			hashCode = hashCode * -1521134295 + y.GetHashCode();
			hashCode = hashCode * -1521134295 + z.GetHashCode();
			return hashCode;
		}
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(Int3))]
	public class Int3PropertyDrawer : PropertyDrawer {
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
			var propZ = new Rect(position.x + 110, position.y, 50, position.height);

			// Draw fields - pass GUIContent.none to each so they are drawn without labels
			EditorGUI.PropertyField(propX, property.FindPropertyRelative("x"), GUIContent.none);
			EditorGUI.PropertyField(propY, property.FindPropertyRelative("y"), GUIContent.none);
			EditorGUI.PropertyField(propZ, property.FindPropertyRelative("z"), GUIContent.none);

			// Set indent back to what it was
			EditorGUI.indentLevel = indent;

			EditorGUI.EndProperty();
		}
	}
#endif

	class Int3Converter : TypeConverter {
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
				if(strValue.Length >= 7) {
					string[] elements = strValue.Substring(1, strValue.Length - 2).Split(';');
					if(elements.Length == 3) {
						if(int.TryParse(elements[0], NumberStyles.Integer, culture.NumberFormat, out int x)) {
							if(int.TryParse(elements[1], NumberStyles.Integer, culture.NumberFormat, out int y)) {
								if(int.TryParse(elements[2], NumberStyles.Integer, culture.NumberFormat, out int z)) {
									return new Int3() { x = x, y = y, z = z };
								}
							}
						}
					}
				}
			}
			return base.ConvertFrom(context, culture, value);
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, System.Type destinationType) {
			Int3 int3 = (Int3)value;
			if(destinationType == typeof(string)) {
				return $"[{int3.x.ToString(culture.NumberFormat)};{int3.y.ToString(culture.NumberFormat)};{int3.z.ToString(culture.NumberFormat)}]";
			}
			return base.ConvertTo(context, culture, value, destinationType);
		}
	}
}