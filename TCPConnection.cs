using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using System.Threading;

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

		private Queue<Tuple<byte, byte[]>> queuedBytes = new Queue<Tuple<byte, byte[]>>();

		public bool debugPrints { get; set; }

		internal TCPConnection(bool isServer) {
			dataIDs = new DataIDs(this);
			this.isServer = isServer;
			bf.Binder = new MyBinder();
			new Thread(new ThreadStart(SendDataFromQueue)) { Name = "Sender Thread" }.Start();
		}

		/// <summary>
		/// Send TCPData to connected device
		/// </summary>
		public void SendData(TCPData data) {
			using (MemoryStream ms = new MemoryStream()) {
				bf.Serialize(ms, data);
				byte[] bytes = ms.ToArray();
				if (debugPrints) {
					Console.WriteLine("Sending data of type TCPData of length {0}", bytes.Length + DataIDs.PACKET_ID_COMPLEXITY + sizeof(Int64));
				}
				SendData(DataIDs.TCPDataID, bytes);
			}
		}

		/// <summary>
		/// Send string to connected device
		/// </summary>
		public void SendData(string data) {
			byte[] bytes = System.Text.Encoding.UTF8.GetBytes(data);
			if (debugPrints) {
				Console.WriteLine("Sending data of type string of length {0}", bytes.Length + DataIDs.PACKET_ID_COMPLEXITY + sizeof(Int64));
			}
			SendData(DataIDs.StringID, bytes);
		}

		/// <summary>
		/// Send Int64 (long) to connected device
		/// </summary>
		public void SendData(Int64 data) {
			byte[] bytes = BitConverter.GetBytes(data);
			if (debugPrints) {
				Console.WriteLine("Sending data of type Int64 of length {0}", bytes.Length + DataIDs.PACKET_ID_COMPLEXITY + sizeof(Int64));
			}
			SendData(DataIDs.Int64ID, bytes);
		}

		/// <summary>
		/// Send a singe byte to the connected device, used for requests
		/// </summary>
		public void SendData(byte packetID, byte requestID) {
			SendData(packetID, new byte[1] { requestID });
		}

		/// <summary>
		///  Sends packet with id 'packetID' and additional byte 'requestID' ad a first elemtent of inner array of 'requestID' and 'data'
		/// </summary>
		internal void SendData(byte packetID, byte requestID, byte[] data) {
			byte[] merged = new byte[data.Length + 1];
			merged[0] = requestID;
			data.CopyTo(merged, 1);
			SendData(packetID, merged);
		}

		/// <summary>
		/// Main Send fuction, send byte[] of 'data' with packet ID 'packetID'
		/// </summary>
		public void SendData(byte packetID, byte[] data) {
			queuedBytes.Enqueue(new Tuple<byte, byte[]>(packetID, data));
			evnt.Set();
		}

		#region Queue sending

		private ManualResetEventSlim evnt;
		internal void SendDataFromQueue() {
			evnt = new ManualResetEventSlim();
			while (true) {
				evnt.Reset();
				if (queuedBytes.Count > 0) {
					SendData(queuedBytes.Dequeue());
				}
				else {
					evnt.Wait();
				}
			}
		}

		private void SendData(Tuple<byte, byte[]> dataTuple) {
			byte packetID = dataTuple.Item1;
			byte[] data = dataTuple.Item2;
			byte[] packetSize = BitConverter.GetBytes(data.LongLength);
			byte[] merged = new byte[data.Length + DataIDs.PACKET_ID_COMPLEXITY + packetSize.Length];
			packetSize.CopyTo(merged, 0);
			merged[packetSize.Length] = packetID;
			data.CopyTo(merged, packetSize.Length + DataIDs.PACKET_ID_COMPLEXITY);
			if (packetID == 0) {
				if (merged[3461] == 154 && merged[3462] == 254 && merged[3463] == 0 && merged[3463] == 0) {
					Console.WriteLine("Error");
				}
				using (MemoryStream ms = new MemoryStream()) {
					ms.Write(merged, 9, data.Length);
					ms.Seek(0, SeekOrigin.Begin);
					TCPData aaa = (TCPData)bf.Deserialize(ms);
				}
			}
			stream.Write(merged, 0, merged.Length);
		}

		#endregion

		#region Data receprion

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

		private Type ReceiveData(out object dataObj) {
			if (debugPrints) {
				Console.WriteLine("Waiting for next packet...");
			}
			byte[] packetSize = new byte[8];
			byte[] packetID = new byte[DataIDs.PACKET_ID_COMPLEXITY];
			Int64 totalReceived = 0;
			while (totalReceived < 8) {
				totalReceived += stream.Read(packetSize, 0, 8);
			}
			totalReceived = 0;
			Int64 toReceive = BitConverter.ToInt64(packetSize, 0);
			while (totalReceived < DataIDs.PACKET_ID_COMPLEXITY) {
				totalReceived += stream.Read(packetID, 0, DataIDs.PACKET_ID_COMPLEXITY);
			}
			if (debugPrints) {
				Console.WriteLine("Waiting for Data 0/" + toReceive + " bytes");
			}
			byte[] data = new byte[toReceive];
			totalReceived = 0;
			while (totalReceived < toReceive) {
				totalReceived += stream.Read(data, (int)totalReceived, (int)(toReceive - totalReceived));
				if (debugPrints) {
					Console.WriteLine("Waiting for Data " + totalReceived + "/" + toReceive + " bytes");
				}
			}
			return dataIDs.IndetifyID(packetID, out dataObj, data);
		}

		#endregion
	}
}

