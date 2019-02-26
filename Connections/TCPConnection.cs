using System;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace Igor.TCP {
	/// <summary>
	/// Base class for all client and server
	/// </summary>
	public class TCPConnection : IDisposable {

		#region Public Properties and fields
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
		/// Called when successfully received data marked as a <see cref="string"/>
		/// </summary>
		public event EventHandler<PacketReceivedEventArgs<string>> OnStringReceived;

		/// <summary>
		/// Called when successfully received data marked as a <see cref="Int64"/>
		/// </summary>
		public event EventHandler<PacketReceivedEventArgs<Int64>> OnInt64Received;

		/// <summary>
		/// Access to data types for custom packets
		/// </summary>
		public RequestHandler responseGenerator { get; }

		#endregion

		#region Internal and private properties/fields

		internal NetworkStream mainNetworkStream;

		internal event EventHandler<TCPResponse> _OnResponse;

		internal Thread senderThread;
		internal Thread receiverThread;

		internal TCPClientInfo myInfo;
		internal TCPClientInfo infoAboutOtherSide;

		internal RequestCreator requestCreator;
		internal Queue<SendQueueItem> queuedData = new Queue<SendQueueItem>();

		internal DataIDs dataIDs { get; }

		internal IValueProvider valueProvider;
		#endregion

		internal TCPConnection(TcpClient baseClient, TCPClientInfo myInfo, TCPClientInfo infoAboutOtherSide, IValueProvider valueProvider) {
			this.infoAboutOtherSide = infoAboutOtherSide;
			this.myInfo = myInfo;

			mainNetworkStream = baseClient.GetStream();
			dataIDs = new DataIDs(this);

			senderThread = new Thread(new ThreadStart(SendDataFromQueue)) { Name = $"Owner:{myInfo.computerName}, IsClient={!myInfo.isServer} Sender Thread" };
			senderThread.Start();
			receiverThread = new Thread(new ThreadStart(DataReception)) { Name = $"Owner:{myInfo.computerName}, IsClient={!myInfo.isServer} Receiver Thread" };
			receiverThread.Start();

			requestCreator = new RequestCreator(this);
			responseGenerator = new RequestHandler(this);

			this.valueProvider = valueProvider;
		}

		#region Send Primitives (string and long)

		/// <summary>
		/// Send UTF-8 encoded string to connected device
		/// </summary>
		public void SendData(string data) {
			byte[] bytes = System.Text.Encoding.UTF8.GetBytes(data);
			if (debugPrints) {
				Console.WriteLine("Sending data of type string of length {0}", bytes.Length + DataIDs.PACKET_ID_COMPLEXITY + DataIDs.PACKET_TOTAL_HEADER_SIZE_COMPLEXITY);
			}
			SendData(DataIDs.StringID, myInfo.clientID, bytes);
		}

		/// <summary>
		/// Send Int64 (long) to connected device
		/// </summary>
		public void SendData(Int64 data) {
			byte[] bytes = BitConverter.GetBytes(data);
			if (debugPrints) {
				Console.WriteLine("Sending data of type Int64 of length {0}", bytes.Length + DataIDs.PACKET_ID_COMPLEXITY + DataIDs.PACKET_TOTAL_HEADER_SIZE_COMPLEXITY);
			}
			SendData(DataIDs.Int64ID, myInfo.clientID, bytes);
		}

		#endregion

		/// <summary>
		/// Send a single byte to the connected device
		/// </summary>
		internal void SendData(byte packetID, byte requestID) {
			SendData(packetID, myInfo.clientID, new byte[1] { requestID });
		}

		/// <summary>
		/// Main Send function, send byte[] of 'data' with packet ID 'packetID'
		/// </summary>
		internal void SendData(byte packetID, byte senderID, byte[] data) {
			queuedData.Enqueue(new SendQueueItem(packetID, senderID, data));
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
			SendData(packetID, myInfo.clientID, data);
		}

		/// <summary>
		/// Send custom data type using internal serialization/deserialization mechanisms
		/// </summary>
		/// <exception cref="UndefinedPacketException"></exception>
		/// <exception cref="InvalidOperationException"></exception>
		public void SendData<TData>(byte packetID, TData data) {
			if (!dataIDs.customIDs.ContainsKey(packetID)) {
				throw new UndefinedPacketException($"Trying to send data with {packetID}, but this data was not defined!", packetID, typeof(TData));
			}
			if (!typeof(TData).IsSerializable) {
				throw new InvalidOperationException("Trying to send data that is not marked as [Serializable]");
			}
			SendData(packetID, myInfo.clientID, SimpleTCPHelper.GetBytesFromObject(data));
		}

		/// <summary>
		/// Send data immediately on current thread, generally unsafe and should be avoided if possible
		/// </summary>
		internal void SendDataImmediate(byte packetID, byte[] data) {
			SendData(new SendQueueItem(packetID, myInfo.clientID, data));
		}

		#region Queue sending

		private ManualResetEventSlim evnt;

		internal void SendDataFromQueue() {
			using (evnt = new ManualResetEventSlim()) {
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
		}

		private void SendData(SendQueueItem item) {
			byte[] packetSize = BitConverter.GetBytes(item.rawData.LongLength);
			byte[] merged = new byte[item.rawData.Length + DataIDs.PACKET_ID_COMPLEXITY + DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY + packetSize.Length];
			packetSize.CopyTo(merged, 0); //Add data size to the beginning of the packet
			merged[packetSize.Length] = item.packetID; //Append packetID
			merged[packetSize.Length + DataIDs.PACKET_ID_COMPLEXITY] = item.originClientID; //Append senderID
			item.rawData.CopyTo(merged, packetSize.Length + DataIDs.PACKET_ID_COMPLEXITY + DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY); //Append the data
			mainNetworkStream.Write(merged, 0, merged.Length);
		}

		#endregion

		#region Data reception

		internal void DataReception() {
			while (listeningForData) {
				ReceivedData data;
				try {
					data = ReceiveData();
				}
				catch (ObjectDisposedException) {
					listeningForData = false;
					return;
				}
				catch (IOException e) {
					if (e.InnerException is SocketException socketE) {
						if (socketE.SocketErrorCode == SocketError.Interrupted) {
							listeningForData = false;
							return;
						}
					}
					continue;
				}

				if (!listeningForData)
					return;

				if(data.dataType == typeof(SocketException)) {
					listeningForData = false;
					return;
				}

				#region Primitives

				if (data.dataID == 0) {
					if (OnStringReceived == null) {
						throw new NullReferenceException("Received a string packet, but the OnStringReceived event is not consumed!");
					}
					OnStringReceived(this, new PacketReceivedEventArgs<string>((string)data.receivedObject, data.senderID));
				}
				else if (data.dataID == 1) {
					if (OnInt64Received == null) {
						throw new NullReferenceException("Received an Int64(long) packet, but the OnInt64Received event is not consumed!");
					}
					OnInt64Received(this, new PacketReceivedEventArgs<Int64>((Int64)data.receivedObject, data.senderID));
				}

				#endregion

				if (dataIDs.customIDs.ContainsKey(data.dataID)) {
					dataIDs.customIDs[data.dataID].action.Invoke(data.senderID, data.receivedObject);
					continue;
				}

				if (data.dataID == DataIDs.ResponseReceptionID) {
					_OnResponse(this, (TCPResponse)data.receivedObject);
				}
				else if (data.dataID == DataIDs.RequestReceptionID) {
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
		protected virtual void HigherLevelDataReceived(ReceivedData data) { }

		private ReceivedData ReceiveData() {
			#region Working with the stream

			if (debugPrints) {
				Console.WriteLine("Waiting for next packet...");
			}

			byte[] packetSize = new byte[DataIDs.PACKET_TOTAL_HEADER_SIZE_COMPLEXITY];
			byte[] packetID = new byte[DataIDs.PACKET_ID_COMPLEXITY];
			byte[] fromClient = new byte[DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY];

			Int64 totalReceived = 0;

			while (totalReceived < packetSize.Length && listeningForData) {
				totalReceived += mainNetworkStream.Read(packetSize, 0, packetSize.Length);
				if (totalReceived == 0) {
					return new ReceivedData(typeof(SocketException),0,0,0);
				}
			}
			totalReceived = 0;
			Int64 toReceive = BitConverter.ToInt64(packetSize, 0);
			while (totalReceived < DataIDs.PACKET_ID_COMPLEXITY) {
				totalReceived += mainNetworkStream.Read(packetID, 0, DataIDs.PACKET_ID_COMPLEXITY);
			}
			totalReceived = 0;
			while (totalReceived < DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY && listeningForData) {
				totalReceived += mainNetworkStream.Read(fromClient, 0, DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY);
			}
			if (debugPrints) {
				Console.WriteLine("Waiting for Data 0/" + toReceive + " bytes");
			}
			byte[] data = new byte[toReceive];
			totalReceived = 0;
			while (totalReceived < toReceive && listeningForData) {
				totalReceived += mainNetworkStream.Read(data, (int)totalReceived, (int)(toReceive - totalReceived));
				if (debugPrints) {
					Console.WriteLine("Waiting for Data " + totalReceived + "/" + toReceive + " bytes");
				}
			}

			#endregion

			var packetIDField = GetPacketID(packetID);

			Type dataType = dataIDs.IndetifyID(packetIDField, GetSenderID(fromClient), data);
			object dataObject;

			if (dataType == typeof(TCPResponse)) {
				dataObject = new TCPResponse(data[0], new byte[data.Length - 1], requestCreator.responseObjectType);
				Array.Copy(data, 1, (dataObject as TCPResponse).rawData, 0, (dataObject as TCPResponse).rawData.Length);
			}
			else if (dataType == typeof(ClientDisconnectedPacket)) {
				dataObject = data[0];
			}
			else if (dataType == typeof(TCPRequest)) {
				dataObject = data[0];
			}
			else if (dataType == typeof(OnPropertySynchronizationEventArgs)) {
				dataObject = data;
			}
			else if (packetIDField == DataIDs.StringID || packetIDField == DataIDs.Int64ID) {
				dataObject = SimpleTCPHelper.GetObject(dataType, data);
			}
			else {
				dataObject = SimpleTCPHelper.GetObject(dataIDs.customIDs[GetPacketID(packetID)].dataType, data);
			}

			return new ReceivedData(dataType, GetSenderID(fromClient), GetPacketID(packetID), dataObject);
		}

		#region Helpers for getting senderID and packetID from byte[]

		private byte GetSenderID(byte[] fromClient) {
			if (DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY != 1) {
#pragma warning disable CS0162 // Unreachable code detected
				Debugger.Break();
#pragma warning restore CS0162 // Unreachable code detected	
				throw new Exception();
			}
			return fromClient[0];
		}

		private byte GetPacketID(byte[] packetID) {
			if (DataIDs.PACKET_ID_COMPLEXITY != 1) {
#pragma warning disable CS0162 // Unreachable code detected
				Debugger.Break();
#pragma warning restore CS0162 // Unreachable code detected
				throw new Exception();
			}
			return packetID[0];
		}

		#endregion

		#endregion

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		/// <summary>
		/// Dispose of the object
		/// </summary>
		protected virtual void Dispose(bool disposing) {
			if (!disposedValue) {
				requestCreator.Dispose();
				mainNetworkStream.Dispose();
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

