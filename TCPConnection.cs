using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

namespace Igor.TCP {
	public abstract class TCPConnection {
		protected BinaryFormatter bf = new BinaryFormatter();

		protected NetworkStream stream;
		protected bool listeningForData;

		public event EventHandler<TCPData> OnTCPDataReceived;
		public event EventHandler<string> OnStringReceived;
		public event EventHandler<Int64> OnInt64Received;



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
			byte packetID = ServerIDs.TCPDataID;
			byte[] merged = new byte[data.Length + ServerIDs.PACKET_ID_COMPLEXITY + packetSize.Length];

			packetSize.CopyTo(merged, 0);
			merged[packetSize.Length] = packetID;
			data.CopyTo(merged, packetSize.Length + ServerIDs.PACKET_ID_COMPLEXITY);

			stream.Write(merged, 0, merged.Length);
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

				switch (ParseID(packetID)) {
					case 0: {
						//TCPData
						dataObj = bf.Deserialize(ms);
						return typeof(TCPData);
					}
					case 32: {
						//String
						dataObj = System.Text.Encoding.UTF8.GetString(ms.ToArray());
						return typeof(string);
					}
					case 64: {
						//Int64
						dataObj = BitConverter.ToInt64(ms.ToArray(), 0);
						return typeof(Int64);
					}
					default: {
						throw new NotSupportedException(string.Format("This identifier is not supported {{0}}",
							packetID[0]));
					}
				}
			}
		}

		private int ParseID(byte[] packetID) {
			return packetID[0];
		}

		private class ServerIDs {
			public const int PACKET_ID_COMPLEXITY = 1;
			public const byte TCPDataID = 0;
			public const byte StringID = 32;
			public const byte LongID = 64;
		}
	}
}

