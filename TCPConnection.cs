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
		internal event EventHandler<TCPResponse> _OnResponse;

		public DataIDs dataIDs;

		internal TCPConnection(bool isServer) {
			dataIDs = new DataIDs(this);
			this.isServer = isServer;
			bf.Binder = new MyBinder();
		}

		public void SendData(TCPData data) {
			using (MemoryStream ms = new MemoryStream()) {
				bf.Serialize(ms, data);
				byte[] bytes = ms.ToArray();
				Console.WriteLine("Sending data of type TCPData of length {0}", bytes.Length + DataIDs.PACKET_ID_COMPLEXITY + sizeof(Int64));
				SendData(DataIDs.TCPDataID, bytes);
			}
		}

		public void SendData(string data) {
			byte[] bytes = System.Text.Encoding.UTF8.GetBytes(data);
			Console.WriteLine("Sending data of type string of length {0}", bytes.Length + DataIDs.PACKET_ID_COMPLEXITY + sizeof(Int64));
			SendData(DataIDs.StringID, bytes);
		}

		public void SendData(Int64 data) {
			byte[] bytes = BitConverter.GetBytes(data);
			Console.WriteLine("Sending data of type Int64 of length {0}", bytes.Length + DataIDs.PACKET_ID_COMPLEXITY + sizeof(Int64));
			SendData(DataIDs.LongID, bytes);
		}

		public void SendData(byte dataID, byte[] data) {
			byte[] packetSize = BitConverter.GetBytes(data.LongLength);
			byte[] merged = new byte[data.Length + DataIDs.PACKET_ID_COMPLEXITY + packetSize.Length];

			packetSize.CopyTo(merged, 0);
			merged[packetSize.Length] = dataID;
			data.CopyTo(merged, packetSize.Length + DataIDs.PACKET_ID_COMPLEXITY);

			stream.Write(merged, 0, merged.Length);
		}

		public void SendData(byte dataID, byte requestID) {
			byte[] packetSize = BitConverter.GetBytes(1L);
			byte[] merged = new byte[DataIDs.PACKET_ID_COMPLEXITY + packetSize.Length + 1];

			packetSize.CopyTo(merged, 0);
			merged[packetSize.Length] = dataID;
			merged[packetSize.Length + 1] = requestID;

			stream.Write(merged, 0, merged.Length);
		}

		internal void SendData(byte packetID, byte requestID, byte[] data) {
			byte[] packetSize = BitConverter.GetBytes(data.LongLength);
			byte[] merged = new byte[DataIDs.PACKET_ID_COMPLEXITY + packetSize.Length + 1 + data.Length];

			packetSize.CopyTo(merged, 0);
			merged[packetSize.Length] = packetID;
			merged[packetSize.Length + DataIDs.PACKET_ID_COMPLEXITY] = requestID;
			data.CopyTo(merged, packetSize.Length + DataIDs.PACKET_ID_COMPLEXITY + 1);

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
				else if (data == typeof(TCPResponse)) {
					_OnResponse(this, (TCPResponse)receivedData);
				}
				else if (data == typeof(TCPRequest)) {
					continue;
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
				byte[] packetID = new byte[DataIDs.PACKET_ID_COMPLEXITY];
				while (totalReceived < DataIDs.PACKET_ID_COMPLEXITY) {
					totalReceived += stream.Read(packetID, 0, DataIDs.PACKET_ID_COMPLEXITY);
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
	}
}

