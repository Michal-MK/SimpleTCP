using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using System.Threading;
#if UNITY_ANDROID || UNITY_STANDALONE
using UnityEngine;
#endif


namespace Igor.TCP {
	/// <summary>
	/// Base class for all client and server
	/// </summary>
	public abstract class TCPConnection : IDisposable {
		private BinaryFormatter bf = new BinaryFormatter();
		/// <summary>
		/// Stream on which all transimssion happens
		/// </summary>
		protected internal NetworkStream stream;

		/// <summary>
		/// Is client listening for incomming data
		/// </summary>
		protected bool listeningForData;

		internal bool isServer;

		/// <summary>
		/// Called when successfully received data from <see cref="DataIDs.TCPDataID"></see> marked packet
		/// </summary>
		public event EventHandler<TCPData> OnTCPDataReceived;

		/// <summary>
		/// Called when successfully received data from <see cref="DataIDs.StringID"></see> marked packet
		/// </summary>
		public event EventHandler<string> OnStringReceived;

		/// <summary>
		/// Called when successfully received data from <see cref="DataIDs.Int64ID"></see> marked packet
		/// </summary>
		public event EventHandler<Int64> OnInt64Received;

		internal event EventHandler<TCPResponse> _OnResponse;

		/// <summary>
		/// Define simple data packets and get internal/externaly defined packet IDs
		/// </summary>
		public DataIDs dataIDs { get; }

		private Queue<Tuple<byte, byte[]>> queuedBytes = new Queue<Tuple<byte, byte[]>>();

		/// <summary>
		/// Print debug information to console
		/// </summary>
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
#if UNITY_ANDROID || UNITY_STANDALONE
					Debug.Log(string.Format("Sending data of type TCPData of length {0}", bytes.Length + DataIDs.PACKET_ID_COMPLEXITY + sizeof(Int64)));
#else
					Console.WriteLine("Sending data of type TCPData of length {0}", bytes.Length + DataIDs.PACKET_ID_COMPLEXITY + sizeof(Int64));
#endif
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
#if UNITY_ANDROID || UNITY_STANDALONE
				Debug.Log(string.Format("Sending data of type string of length {0}", bytes.Length + DataIDs.PACKET_ID_COMPLEXITY + sizeof(Int64)));
#else
				Console.WriteLine("Sending data of type string of length {0}", bytes.Length + DataIDs.PACKET_ID_COMPLEXITY + sizeof(Int64));
#endif
			}
			SendData(DataIDs.StringID, bytes);
		}

		/// <summary>
		/// Send Int64 (long) to connected device
		/// </summary>
		public void SendData(Int64 data) {
			byte[] bytes = BitConverter.GetBytes(data);
			if (debugPrints) {
#if UNITY_ANDROID || UNITY_STANDALONE
				Debug.Log(string.Format("Sending data of type Int64 of length {0}", bytes.Length + DataIDs.PACKET_ID_COMPLEXITY + sizeof(Int64)));
#else
				Console.WriteLine("Sending data of type Int64 of length {0}", bytes.Length + DataIDs.PACKET_ID_COMPLEXITY + sizeof(Int64));
#endif
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
			stream.Write(merged, 0, merged.Length);
		}

		#endregion

		#region Data receprion

		internal void DataReception() {
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
#if UNITY_ANDROID || UNITY_STANDALONE
				Debug.Log("Waiting for next packet...");
#else
				Console.WriteLine("Waiting for next packet...");
#endif
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
#if UNITY_ANDROID || UNITY_STANDALONE
				Debug.Log("Waiting for Data 0/" + toReceive + " bytes");
#else
				Console.WriteLine("Waiting for Data 0/" + toReceive + " bytes");
#endif
			}
			byte[] data = new byte[toReceive];
			totalReceived = 0;
			while (totalReceived < toReceive) {
				totalReceived += stream.Read(data, (int)totalReceived, (int)(toReceive - totalReceived));
				if (debugPrints) {
#if UNITY_ANDROID || UNITY_STANDALONE
					Debug.Log("Waiting for Data " + totalReceived + "/" + toReceive + " bytes");
#else
					Console.WriteLine("Waiting for Data " + totalReceived + "/" + toReceive + " bytes");
#endif
				}
			}
			return dataIDs.IndetifyID(packetID, out dataObj, data);
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		/// <summary>
		/// Dispose of the object
		/// </summary>
		protected virtual void Dispose(bool disposing) {
			if (!disposedValue) {
				if (disposing) {

				}
				evnt.Dispose();
				disposedValue = true;
			}
		}

		/// <summary>
		/// Finalizer
		/// </summary>
		~TCPConnection() {
			Dispose(false);
		}

		/// <summary>
		/// Dispose of the object
		/// </summary>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion

		#endregion
	}
}

