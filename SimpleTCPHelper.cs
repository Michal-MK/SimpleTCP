using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Igor.TCP {
	/// <summary>
	/// Static class containing useful methods for data transmission
	/// </summary>
	public static class SimpleTCPHelper {
		private static readonly IFormatter FORMATTER;

		static SimpleTCPHelper() {
			FORMATTER = new BinaryFormatter { Binder = new MyBinder() };
		}

		/// <summary>
		/// Returns active IPv4 Address of this computer
		/// </summary>
		/// <exception cref="WebException"></exception>
		public static IPAddress GetActiveIPv4Address(int timeout = 2000) {
			using Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, 0);
			
			socket.Connect("8.8.8.8", 80);
			return ((IPEndPoint)socket.LocalEndPoint).Address;
		}

		/// <summary>
		/// Wrapper to all object to byte[] conversions
		/// </summary>
		internal static byte[] GetBytesFromObject(object obj) {
			switch (obj) {
				case bool bo:    return BitConverter.GetBytes(bo);
				case byte b:     return new[] { b };
				case string str: return System.Text.Encoding.UTF8.GetBytes(str);
				case char c:     return BitConverter.GetBytes(c);
				case double d:   return BitConverter.GetBytes(d);
				case float f:    return BitConverter.GetBytes(f);
				case int i:      return BitConverter.GetBytes(i);
				case long l:     return BitConverter.GetBytes(l);
				case short sh:   return BitConverter.GetBytes(sh);
				case uint ui:    return BitConverter.GetBytes(ui);
				case ulong ul:   return BitConverter.GetBytes(ul);
				case ushort us:  return BitConverter.GetBytes(us);
				default: {
					using MemoryStream ms = new();
					FORMATTER.Serialize(ms, obj);
					ms.Seek(0, SeekOrigin.Begin);
					return ms.ToArray();
				}
			}
		}

		internal static object GetObject(Type t, byte[] bytes) {
			object obj;

			if (t == typeof(bool)) {
				obj = BitConverter.ToBoolean(bytes, 0);
			}
			else if (t == typeof(byte)) {
				obj = bytes[0];
			}
			else if (t == typeof(string)) {
				obj = System.Text.Encoding.UTF8.GetString(bytes);
			}
			else if (t == typeof(char)) {
				obj = BitConverter.ToChar(bytes, 0);
			}
			else if (t == typeof(double)) {
				obj = BitConverter.ToDouble(bytes, 0);
			}
			else if (t == typeof(float)) {
				obj = BitConverter.ToSingle(bytes, 0);
			}
			else if (t == typeof(int)) {
				obj = BitConverter.ToInt32(bytes, 0);
			}
			else if (t == typeof(long)) {
				obj = BitConverter.ToInt64(bytes, 0);
			}
			else if (t == typeof(short)) {
				obj = BitConverter.ToInt16(bytes, 0);
			}
			else if (t == typeof(uint)) {
				obj = BitConverter.ToUInt32(bytes, 0);
			}
			else if (t == typeof(ulong)) {
				obj = BitConverter.ToUInt64(bytes, 0);
			}
			else if (t == typeof(ushort)) {
				obj = BitConverter.ToUInt16(bytes, 0);
			}
			else {
				try {
					using MemoryStream ms = new(bytes);
					obj = FORMATTER.Deserialize(ms);
				}
				catch (Exception) {
					obj = bytes;
				}
			}
			return obj;
		}

		internal static T GetObject<T>(byte[] bytes) where T : new() {
			Type tType = typeof(T);

			if (tType == typeof(bool)) {
				return (T)Convert.ChangeType(BitConverter.ToBoolean(bytes, 0), typeof(T));
			}
			if (tType == typeof(char)) {
				return (T)Convert.ChangeType(BitConverter.ToChar(bytes, 0), typeof(T));
			}
			if (tType == typeof(double)) {
				return (T)Convert.ChangeType(BitConverter.ToDouble(bytes, 0), typeof(T));
			}
			if (tType == typeof(float)) {
				return (T)Convert.ChangeType(BitConverter.ToSingle(bytes, 0), typeof(T));
			}
			if (tType == typeof(Int32)) {
				return (T)Convert.ChangeType(BitConverter.ToInt32(bytes, 0), typeof(T));
			}
			if (tType == typeof(Int64)) {
				return (T)Convert.ChangeType(BitConverter.ToInt64(bytes, 0), typeof(T));
			}
			if (tType == typeof(Int16)) {
				return (T)Convert.ChangeType(BitConverter.ToInt16(bytes, 0), typeof(T));
			}
			if (tType == typeof(UInt32)) {
				return (T)Convert.ChangeType(BitConverter.ToUInt32(bytes, 0), typeof(T));
			}
			if (tType == typeof(UInt64)) {
				return (T)Convert.ChangeType(BitConverter.ToUInt64(bytes, 0), typeof(T));
			}
			if (tType == typeof(UInt16)) {
				return (T)Convert.ChangeType(BitConverter.ToUInt16(bytes, 0), typeof(T));
			}
			try {
				using MemoryStream ms = new();
				ms.Write(bytes, 0, bytes.Length);
				ms.Seek(0, SeekOrigin.Begin);
				return (T)FORMATTER.Deserialize(ms);
			}
			catch (Exception) {
				throw new SerializationException($"Unable to deserialize stream into type '{tType}'" +
												 " possibly the stream was not serialized using the internal serializer," +
												 " in that case you have to write your own deserializer as well.");
			}
		}

		internal static void SaveArrayToFile(string file, byte[] array) {
			using StreamWriter sw = File.CreateText(file);
			foreach (byte b in array) {
				sw.Write(b + ",");
			}
		}
	}
}