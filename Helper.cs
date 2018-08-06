using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

namespace Igor.TCP {
	public static class Helper {
		private static BinaryFormatter bf = new BinaryFormatter();

		public static IPAddress GetActivePIv4Address() {
			using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0)) {
				socket.Connect("8.8.8.8", 65530);
				IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
				return endPoint.Address;
			}
		}

		public static byte[] GetBytesFromObject<T>(object obj) {
			byte[] bytes;

			if (obj is bool) {
				bytes = BitConverter.GetBytes((bool)obj);
			}
			else if (obj is char) {
				bytes = BitConverter.GetBytes((char)obj);
			}
			else if (obj is double) {
				bytes = BitConverter.GetBytes((double)obj);
			}
			else if (obj is float) {
				bytes = BitConverter.GetBytes((float)obj);
			}
			else if (obj is int) {
				bytes = BitConverter.GetBytes((int)obj);
			}
			else if (obj is long) {
				bytes = BitConverter.GetBytes((long)obj);
			}
			else if (obj is short) {
				bytes = BitConverter.GetBytes((short)obj);
			}
			else if (obj is uint) {
				bytes = BitConverter.GetBytes((uint)obj);
			}
			else if (obj is ulong) {
				bytes = BitConverter.GetBytes((bool)obj);
			}
			else if (obj is ushort) {
				bytes = BitConverter.GetBytes((ushort)obj);
			}
			else {
				using (MemoryStream ms = new MemoryStream()) {
					bf.Serialize(ms, obj);
					bytes = ms.ToArray();
				}
			}
			return bytes;
		}
	
	
		internal static object GetObject(Type t, byte[] bytes) {
			object obj;

			if (t == typeof(bool)) {
				obj = BitConverter.ToBoolean(bytes,0);
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
				using (MemoryStream ms = new MemoryStream()) {
					ms.Write(bytes, 0, bytes.Length);
					ms.Seek(0, SeekOrigin.Begin);
					obj = bf.Deserialize(ms);
				}
			}
			return obj;
		}


		public static byte[] GetBytesFromObject<T>(byte customID, object obj) {
			byte[] bytes = GetBytesFromObject<T>(obj);
			byte[] result = new byte[1 + bytes.Length];
			result[0] = customID;
			Array.Copy(bytes, 0, result, 1, bytes.Length);
			return result;
		}
	}
}
