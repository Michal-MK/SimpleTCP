using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using System.Threading;

namespace Igor.TCP {
	/// <summary>
	/// Base class for all client and server
	/// </summary>
	public class TCPConnection : IDisposable {

		private BinaryFormatter bf = new BinaryFormatter();
		/// <summary>
		/// Stream on which all transmission happens
		/// </summary>
		internal NetworkStream stream;

		/// <summary>
		/// Is client listening for incoming data
		/// </summary>
		public bool listeningForData { get; internal set; } = true;

		/// <summary>
		/// Is client actively trying to send data
		/// </summary>
		public bool sendingData { get; internal set; } = true;

		/// <summary>
		/// Print debug information to console
		/// </summary>
		public bool debugPrints { get; set; }

		/// <summary>
		/// Called when successfully received data from <see cref="DataIDs.StringID"></see> marked packet
		/// </summary>
		public event EventHandler<PacketReceivedEventArgs<string>> OnStringReceived;

		/// <summary>
		/// Called when successfully received data from <see cref="DataIDs.Int64ID"></see> marked packet
		/// </summary>
		public event EventHandler<PacketReceivedEventArgs<Int64>> OnInt64Received;

		internal event EventHandler<TCPResponse> _OnResponse;

		/// <summary>
		/// Handle Requests to the server
		/// </summary>
		internal RequestManager requestHandler { get; }

		/// <summary>
		/// Access to data types for custom packets
		/// </summary>
		public ResponseManager responseHandler { get; }


		internal Thread senderThread;
		internal Thread receiverThread;

		/// <summary>
		/// Define simple data packets and get internally/externally defined packet IDs
		/// </summary>
		public DataIDs dataIDs { get; }

		/// <summary>
		/// Queue to synchronize packet sending
		/// </summary>
		protected Queue<SendQueueItem> queuedData = new Queue<SendQueueItem>();

		internal TCPClientInfo myInfo;
		internal TCPClientInfo connectedClientInfo;

		internal TCPConnection(TcpClient baseClient,TCPClientInfo me, TCPClientInfo connectecClient) {
			connectedClientInfo = connectecClient;
			myInfo = me;
			stream = baseClient.GetStream();
			dataIDs = new DataIDs(this);
			bf.Binder = new MyBinder();

			senderThread = new Thread(new ThreadStart(SendDataFromQueue)) { Name = string.Format("{0} ({1}) \"{2}\" Sender", connectecClient.clientAddress, connectecClient.clientID, connectecClient.computerName) };
			senderThread.Start();
			receiverThread = new Thread(new ThreadStart(DataReception)) { Name = string.Format("{0} ({1}) \"{2}\" Receiver", connectecClient.clientAddress, connectecClient.clientID, connectecClient.computerName) };
			receiverThread.Start();

			requestHandler = new RequestManager(this);
			responseHandler = new ResponseManager(this);
		}

		/// <summary>
		/// Send UTF-8 encoded string to connected device
		/// </summary>
		public void SendData(string data) {
			byte[] bytes = System.Text.Encoding.UTF8.GetBytes(data);
			if (debugPrints) {
				Console.WriteLine("Sending data of type string of length {0}", bytes.Length + DataIDs.PACKET_ID_COMPLEXITY + DataIDs.PACKET_TOTAL_SIZE_COMPLEXITY);
			}
			_SendData(DataIDs.StringID, myInfo.clientID, bytes);
		}

		/// <summary>
		/// Send Int64 (long) to connected device
		/// </summary>
		public void SendData(Int64 data) {
			byte[] bytes = BitConverter.GetBytes(data);
			if (debugPrints) {
				Console.WriteLine("Sending data of type Int64 of length {0}", bytes.Length + DataIDs.PACKET_ID_COMPLEXITY + DataIDs.PACKET_TOTAL_SIZE_COMPLEXITY);
			}
			_SendData(DataIDs.Int64ID, myInfo.clientID, bytes);
		}

		/// <summary>
		/// Send a singe byte to the connected device, used for requests
		/// </summary>
		public void SendData(byte packetID, byte requestID) {
			_SendData(packetID, myInfo.clientID, new byte[1] { requestID });
		}

		/// <summary>
		///  Sends packet with id 'packetID' and additional byte 'subPacketID' as a first element of inner array of 'subPacketID' and 'data'
		/// </summary>
		public void SendData(byte packetID, byte subPacketID, byte[] data) {
			byte[] merged = new byte[data.Length + 1];
			merged[0] = subPacketID;
			data.CopyTo(merged, 1);
			_SendData(packetID, myInfo.clientID, merged);
		}

		/// <summary>
		/// Main Send function, send byte[] of 'data' with packet ID 'packetID'
		/// </summary>
		internal void _SendData(byte packetID, byte clientID, byte[] data) {
			queuedData.Enqueue(new SendQueueItem(packetID, clientID, data, false));
			evnt.Set();
		}

		/// <summary>
		/// Send user defined byte[] 'data' under 'packetID'
		/// </summary>
		/// <exception cref="NotImplementedException"></exception>
		public void SendData(byte packetID, byte[] data) {
			if (!dataIDs.customIDs.ContainsKey(packetID)) {
				throw new NotImplementedException("Trying to send data with " + packetID + ", but this data was not defined!");
			}
			_SendData(packetID, myInfo.clientID, data);
		}

		/// <summary>
		/// Send data immediately on current thread, generally unsafe and should be avoided if possible
		/// </summary>
		internal void SendDataImmediate(byte packetID, byte[] data) {
			SendData(new SendQueueItem(packetID, myInfo.clientID, data, this as ServerToClientConnection != null));
		}

		#region Queue sending

		protected ManualResetEventSlim evnt;
		internal void SendDataFromQueue() {
			evnt = new ManualResetEventSlim();
			while (sendingData) {
				evnt.Reset();
				if (queuedData.Count > 0) {
					SendData(queuedData.Dequeue());
				}
				else {
					evnt.Wait();
				}
			}
		}

		internal void EnquqeAndSend(SendQueueItem item) {
			queuedData.Enqueue(item);
			evnt.Set();
		}

		private void SendData(SendQueueItem item) {
			byte[] packetSize = BitConverter.GetBytes(item.rawData.LongLength);
			byte[] merged = new byte[item.rawData.Length + DataIDs.PACKET_ID_COMPLEXITY + DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY + packetSize.Length];
			packetSize.CopyTo(merged, 0); //Add data size to the beginning of the packet
			merged[packetSize.Length] = item.packetID; //Append packetID
			merged[packetSize.Length + DataIDs.PACKET_ID_COMPLEXITY] = item.originClientID; //Append senderID
			item.rawData.CopyTo(merged, packetSize.Length + DataIDs.PACKET_ID_COMPLEXITY + DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY); //Append the data
			stream.Write(merged, 0, merged.Length);
		}

		#endregion

		#region Data reception

		internal void DataReception() {
			while (listeningForData) {
				ReceivedData data = ReceiveData();
				if (data.dataType == typeof(string) && data.dataID == 0) {
					if (OnStringReceived == null) {
						throw new NullReferenceException("Received a string packet, but the OnStringReceived event is not consumed!");
					}
					OnStringReceived(this, new PacketReceivedEventArgs<string>((string)data.receivedObject, data.senderID));
				}
				else if (data.dataType == typeof(Int64) && data.dataID == 1) {
					if (OnInt64Received == null) {
						throw new NullReferenceException("Received an Int64(long) packet, but the OnInt64Received event is not consumed!");
					}
					OnInt64Received(this, new PacketReceivedEventArgs<Int64>((Int64)data.receivedObject, data.senderID));
				}
				else if (data.dataType == typeof(TCPResponse)) {
					_OnResponse(this, (TCPResponse)data.receivedObject);
				}
				else if (data.dataType == typeof(TCPRequest)) {
					continue;
				}
				else {
					HigherLevelDataReceived(data);
				}
			}
		}

		/// <summary>
		/// Override in specific connection to handle higher level data
		/// </summary>
		/// <param name="data">The data received</param>
		public virtual void HigherLevelDataReceived(ReceivedData data) { }


		private ReceivedData ReceiveData() {
			if (debugPrints) {
				Console.WriteLine("Waiting for next packet...");
			}
			byte[] packetSize = new byte[8];
			byte[] packetID = new byte[DataIDs.PACKET_ID_COMPLEXITY];
			byte[] fromClient = new byte[DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY];

			Int64 totalReceived = 0;

			while (totalReceived < packetSize.Length) {
				totalReceived += stream.Read(packetSize, 0, packetSize.Length);
			}
			totalReceived = 0;
			Int64 toReceive = BitConverter.ToInt64(packetSize, 0);
			while (totalReceived < DataIDs.PACKET_ID_COMPLEXITY) {
				totalReceived += stream.Read(packetID, 0, DataIDs.PACKET_ID_COMPLEXITY);
			}
			totalReceived = 0;
			while (totalReceived < DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY) {
				totalReceived += stream.Read(fromClient, 0, DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY);
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
			return new ReceivedData(dataIDs.IndetifyID(packetID[0], fromClient[0], data, out object dataObj), fromClient[0], packetID[0], dataObj);
		}
		#endregion

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		/// <summary>
		/// Dispose of the object
		/// </summary>
		protected virtual void Dispose(bool disposing) {
			if (!disposedValue) {
				evnt.Dispose();
				requestHandler.Dispose();
				stream.Dispose();
				disposedValue = true;
			}
		}

		/// <summary>
		/// Dispose of the object
		/// </summary>
		public void Dispose() {
			Dispose(true);
		}
		#endregion
	}
}

