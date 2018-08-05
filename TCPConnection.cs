using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Igor.TCP {
	public abstract class TCPConnection {
		protected BinaryFormatter bf = new BinaryFormatter();

		protected NetworkStream stream;
		protected bool listeningForData;

		internal bool isServer;

		public event EventHandler<TCPData> OnTCPDataReceived;
		public event EventHandler<string> OnStringReceived;
		public event EventHandler<Int64> OnInt64Received;
		public event EventHandler<object> OnRequestAnswered;

		internal ServerIDs dataIDs;

		internal TCPConnection(bool isServer) {
			dataIDs = new ServerIDs(this);
			this.isServer = isServer;
		}

		public void SendData(TCPData data) {
			using (MemoryStream ms = new MemoryStream()) {
				bf.Serialize(ms, data);
				byte[] bytes = ms.ToArray();
				Console.WriteLine("Sending data of type TCPData of length {0}", bytes.Length + ServerIDs.PACKET_ID_COMPLEXITY + sizeof(Int64));
				SendData(ServerIDs.TCPDataID, bytes);
			}
		}

		public void SendData(string data) {
			byte[] bytes = System.Text.Encoding.UTF8.GetBytes(data);
			Console.WriteLine("Sending data of type string of length {0}", bytes.Length + ServerIDs.PACKET_ID_COMPLEXITY + sizeof(Int64));
			SendData(ServerIDs.StringID, bytes);
		}

		public void SendData(Int64 data) {
			byte[] bytes = BitConverter.GetBytes(data);
			Console.WriteLine("Sending data of type Int64 of length {0}", bytes.Length + ServerIDs.PACKET_ID_COMPLEXITY + sizeof(Int64));
			SendData(ServerIDs.LongID, bytes);
		}

		public void SendData(byte dataID, byte[] data) {
			byte[] packetSize = BitConverter.GetBytes(data.LongLength);
			byte packetID = dataID;
			byte[] merged = new byte[data.Length + ServerIDs.PACKET_ID_COMPLEXITY + packetSize.Length];

			packetSize.CopyTo(merged, 0);
			merged[packetSize.Length] = packetID;
			data.CopyTo(merged, packetSize.Length + ServerIDs.PACKET_ID_COMPLEXITY);

			stream.Write(merged, 0, merged.Length);
		}

		internal NetworkStream GetStream() {
			return stream;
		}

		protected void DataReception() {
			while (listeningForData) {
				Type data = ReceiveData(out object receivedData);
				if (data == typeof(TCPData)) {
					OnTCPDataReceived(this, (TCPData)receivedData);
				}
				else if (data == typeof(string)) {
					OnStringReceived(this, (string)receivedData);
				}
				else if (data == typeof(Int64)) {
					OnInt64Received(this, (Int64)receivedData);
				}
				else if(data == typeof(TCPResponse)) {
					OnRequestAnswered(this, receivedData);
				}
			}
		}

		public Type ReceiveData(out object dataObj) {
			using (MemoryStream ms = new MemoryStream()) {
				Console.WriteLine("Waiting for PacketSize bytes");
				byte[] packetSize = new byte[8];
				Int64 totalReceived = 0;
				while (totalReceived < 8) {
					totalReceived += stream.Read(packetSize, 0, 8);
				}
				totalReceived = 0;
				Int64 toReceive = BitConverter.ToInt64(packetSize, 0);
				byte[] packetID = new byte[ServerIDs.PACKET_ID_COMPLEXITY];
				while (totalReceived < ServerIDs.PACKET_ID_COMPLEXITY) {
					totalReceived += stream.Read(packetID, 0, ServerIDs.PACKET_ID_COMPLEXITY);
				}

				Console.WriteLine("Waiting for Data 0/" + toReceive + " bytes");
				byte[] data = new byte[toReceive];
				totalReceived = 0;
				while (totalReceived < toReceive) {
					if ((int)(toReceive - totalReceived) == 0) {
						break;
					}
					totalReceived += stream.Read(data, 0, (int)(toReceive - totalReceived));
					Console.WriteLine("Waiting for Data " + totalReceived + "/" + toReceive + " bytes");
				}
				ms.Flush();
				ms.Write(data, 0, data.Length);
				ms.Seek(0, SeekOrigin.Begin);

				return dataIDs.IndetifyID(packetID, out dataObj, ms);
			}
		}

		private byte[] ToBuffer(object obj) {
			using (MemoryStream ms = new MemoryStream()) {
				BinaryFormatter bf = new BinaryFormatter();
				bf.Serialize(ms, obj);
				return ms.ToArray();
			}
		}

		internal class ServerIDs {


			public const int PACKET_ID_COMPLEXITY = 1;
			public const byte TCPDataID = 0;
			public const byte StringID = 32;
			public const byte LongID = 64;
			public const byte TestID = 128;


			internal readonly Dictionary<byte, Delegate> idDict = new Dictionary<byte, Delegate>();

			private BinaryFormatter bf = new BinaryFormatter();
			private TCPConnection connection;


			internal ServerIDs(TCPConnection connection) {
				this.connection = connection;
			}

			private byte ParseID(byte[] packetID) {
				return packetID[0];
			}

			public Type IndetifyID(byte[] ID, out object dataObj, MemoryStream ms) {
				byte id = ParseID(ID);
				switch (id) {
					case TCPDataID: {
						dataObj = bf.Deserialize(ms);
						return typeof(TCPData);
					}
					case StringID: {
						dataObj = System.Text.Encoding.UTF8.GetString(ms.ToArray());
						return typeof(string);
					}
					case LongID: {
						dataObj = BitConverter.ToInt64(ms.ToArray(), 0);
						return typeof(Int64);
					}
					case TestID: {
						Type t;
						if (connection.OnRequestAnswered == null) {
							object obj = idDict[id].DynamicInvoke(null);
							using (MemoryStream internalMS = new MemoryStream()) {
								bf.Serialize(internalMS, obj);
								connection.SendData(id, internalMS.ToArray());
							}
							dataObj = null;
							t = null;
						}
						else {
							dataObj = bf.Deserialize(ms);
							t = typeof(TCPResponse);
						}
						return t;
					}

					default: {
						throw new NotSupportedException(string.Format("This identifier is not supported {{0}}",
							ID[0]));
					}
				}
			}

			internal void AddNew<TData>(byte ID, Func<TData> func) {
				idDict.Add(ID, func);
			}

			internal void RemoveFunc(byte ID) {
				idDict.Remove(ID);
			}
		}
	}
}

