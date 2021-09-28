using System.ComponentModel;
using System.Globalization;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sperlich.Types {

	[Serializable]
	public struct Float3 : System.IEquatable<Float3> {
		public double x;
		public double y;
		public double z;

		public Float3(float _x, float _y, float _z) {
			x = _x;
			y = _y;
			z = _z;
		}

		public Float3(double _x, double _y, double _z) {
			x = _x;
			y = _y;
			z = _z;
		}

		public Float3(Vector3 vector3, int round = -1) {
			if(round <= 0) {
				x = vector3.x;
				y = vector3.y;
				z = vector3.z;
			} else {
				x = Math.Round(vector3.x, round);
				y = Math.Round(vector3.y, round);
				z = Math.Round(vector3.z, round);
			}
		}

		public Float3 XYZ => new Float3(x, y, z);
		public Float3 YZY => new Float3(x, z, y);
		public Float3 YXZ => new Float3(y, x, z);
		public Float3 YZX => new Float3(y, z, x);
		public Float3 ZXY => new Float3(z, x, y);
		public Float3 ZYX => new Float3(z, y, z);
		public Float2 XY => new Float2((float)x, (float)y);
		public Float2 XZ => new Float2((float)x, (float)z);
		public Vector3 Vector3 => new Vector3((float)x, (float)y, (float)z);
		public Vector2 Vector2 => new Vector2((float)x, (float)y);

		public static implicit operator Float3(Vector2 float2) => new Float3(float2.x, float2.y, 0);
		public static implicit operator Float3(Vector3 float3) => new Float3(float3.x, float3.y, float3.z);
		public static implicit operator Vector3(Float3 vec) => new Vector3((float)vec.x, (float)vec.y, (float)vec.z);
		public static implicit operator Vector3Int(Float3 float3) => new Vector3Int(Mathf.RoundToInt((float)float3.x), Mathf.RoundToInt((float)float3.y), Mathf.RoundToInt((float)float3.z));
		public static implicit operator Int3(Float3 float3) => new Int3(Mathf.RoundToInt((float)float3.x), Mathf.RoundToInt((float)float3.y), Mathf.RoundToInt((float)float3.z));

		public static Float3 operator +(Float3 item1, Float3 item2) => new Float3(item1.x + item2.x, item1.y + item2.y, item1.z + item2.z);
		public static Float3 operator -(Float3 item1, Float3 item2) => new Float3(item1.x - item2.x, item1.y - item2.y, item1.z - item2.z);
		public static Float3 operator *(Float3 item1, Float3 item2) => new Float3(item1.x * item2.x, item1.y * item2.y, item1.z * item2.z);
		public static Float3 operator *(Float3 item1, float multiplier) => new Float3(item1.x * multiplier, item1.y + multiplier, item1.z * multiplier);
		public static Float3 operator *(float multiplier, Float3 item1) => new Float3(item1.x * multiplier, item1.y + multiplier, item1.z * multiplier);
		public static Float3 operator /(Float3 item1, Float3 item2) => new Float3(item1.x / item2.x, item1.y / item2.y, item1.z / item2.z);
		public static Float3 operator /(Float3 item1, float multiplier) => new Float3(item1.x / multiplier, item1.y / multiplier, item1.z / multiplier);
		public static Float3 operator /(float multiplier, Float3 item1) => new Float3(item1.x / multiplier, item1.y / multiplier, item1.z / multiplier);
		public static bool operator ==(Float3 item1, Float3 item2) => item1.x == item2.x && item1.y == item2.y && item1.z == item2.z;
		public static bool operator !=(Float3 item1, Float3 item2) => !(item1 == item2);


		public override string ToString() => "[" + x + ", " + y + ", " + z + "]";

		public static string Int3ToString(int x, int y, int z) => "[" + x + ", " + y + ", " + z + "]";

		public override bool Equals(object obj) => obj is Float3 @int && Equals(@int);

		public bool Equals(Float3 other) => x == other.x && y == other.y && z == other.z;

		public override int GetHashCode() {
			int hashCode = 373119289;
			hashCode = hashCode * -1521134295 + x.GetHashCode();
			hashCode = hashCode * -1521134295 + y.GetHashCode();
			hashCode = hashCode * -1521134295 + z.GetHashCode();
			return hashCode;
		}
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(Float3))]
	public class Float3PropertyDrawer : PropertyDrawer {
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

	class Float3Converter : TypeConverter {
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
						if(float.TryParse(elements[0], NumberStyles.Float, culture.NumberFormat, out float x)) {
							if(float.TryParse(elements[1], NumberStyles.Float, culture.NumberFormat, out float y)) {
								if(float.TryParse(elements[2], NumberStyles.Float, culture.NumberFormat, out float z)) {
									return new Float3() { x = x, y = y, z = z };
								}
							}
						}
					}
				}
			}
			return base.ConvertFrom(context, culture, value);
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, System.Type destinationType) {
			Float3 float3 = (Float3)value;
			if(destinationType == typeof(string)) {
				return $"[{float3.x.ToString(culture.NumberFormat)};{float3.y.ToString(culture.NumberFormat)};{float3.z.ToString(culture.NumberFormat)}]";
			}
			return base.ConvertTo(context, culture, value, destinationType);
		}
	}
}