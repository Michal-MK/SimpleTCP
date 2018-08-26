using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

namespace Igor.TCP {
	/// <summary>
	/// Static class containing useful methods for data transmission
	/// </summary>
	public static class Helper {
		private static BinaryFormatter bf = new BinaryFormatter();
		/// <summary>
		/// Returns active IPv4 Address of this computer
		/// </summary>
		/// <exception cref="WebException"></exception>
		public static IPAddress GetActiveIPv4Address() {
			using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0)) {
				try {
					socket.Connect("8.8.8.8", 65530);
					IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
					return endPoint.Address;
				}
				catch {
					throw new WebException("Unable to connect to Google proxy", WebExceptionStatus.ConnectFailure);
				}
			}
		}

		/// <summary>
		/// Wrapper to all object to byte[] conversions
		/// <para>WARNING! When serializing/deserializing custom structures the name-space has to match on both ends!</para> 
		/// </summary>
		public static byte[] GetBytesFromObject(object obj) {
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

		/// <summary>
		/// Objects have to be in the same name-space in order to return an actual object, otherwise a byte[] is returned!
		/// </summary>
		internal static object GetObject(Type t, byte[] bytes) {
			object obj;

			if (t == typeof(bool)) {
				obj = BitConverter.ToBoolean(bytes, 0);
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
					using (MemoryStream ms = new MemoryStream()) {
						ms.Write(bytes, 0, bytes.Length);
						ms.Seek(0, SeekOrigin.Begin);
						bf.Binder = new MyBinder();
						obj = bf.Deserialize(ms);
					}
				}
				catch(Exception e) {
					Console.WriteLine(e.Message);
					obj = bytes;
				}
			}
			return obj;
		}

		/// <summary>
		/// Wrapper to all object to byte[] conversions, includes request ID 'customID' as first element of the array
		/// </summary>
		public static byte[] GetBytesFromObject(byte customID, object obj) {
			byte[] bytes = GetBytesFromObject(obj);
			byte[] result = new byte[1 + bytes.Length];
			result[0] = customID;
			Array.Copy(bytes, 0, result, 1, bytes.Length);
			return result;
		}

		/// <summary>
		/// Convert numeric value of bytes to an integer, lower index >> less important byte
		/// </summary>
		public static UInt64 ConvertToUInt64(byte[] byteRepresentaion) {
			int len = byteRepresentaion.Length;
			UInt64 ret = 0;
			for (uint i = 0; i < len; i++) {
				ret += byteRepresentaion[i] * IntPow(byte.MaxValue, i);
			}
			return ret;
		}

		private static UInt64 IntPow(UInt64 x, uint pow) {
			UInt64 ret = 1;
			while (pow != 0) {
				if ((pow & 1) == 1) {
					ret *= x;
				}
				x *= x;
				pow >>= 1;
			}
			return ret;
		}


		internal static void SaveArrayToFile(string file, byte[] array) {
			using (StreamWriter sw = File.CreateText(file)) {
				foreach (byte b in array) {
					sw.Write(b + ",");
				}
			}
		}

		internal static string BytesToString(byte[] bytes) {
			return System.Text.Encoding.UTF8.GetString(bytes);
		}
	}
}
