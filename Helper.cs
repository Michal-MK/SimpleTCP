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

		public static byte[] GetBytesFromObject<T>(byte customID, object obj) {
			byte[] bytes = GetBytesFromObject<T>(obj);
			byte[] result = new byte[1 + bytes.Length];
			result[0] = customID;
			Array.Copy(bytes, 0, result, 1, bytes.Length);
			return result;
		}
	}
}
