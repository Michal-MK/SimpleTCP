using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using SimpleTCP.Connections.Interfaces;
using SimpleTCP.DataTransfer;
using SimpleTCP.Events;
using SimpleTCP.Exceptions;
using SimpleTCP.Structures;

namespace SimpleTCP.Connections {
	/// <summary>
	/// Base class for all client and server
	/// </summary>
	public class TCPConnection : IDisposable {
		#region Public Properties

		/// <summary>
		/// Is client listening for incoming data
		/// </summary>
		public bool ListeningForData { get; internal set; } = true;

		/// <summary>
		/// Is client actively trying to send data
		/// </summary>
		public bool SendingData { get; internal set; } = true;

		/// <summary>
		/// Called when successfully received data by <see cref="DataIDs.STRING_ID"></see> marked packet
		/// </summary>
		public event EventHandler<PacketReceivedEventArgs<string>> OnStringReceived;

		/// <summary>
		/// Called when successfully received data marked as a <see cref="Int64"/>
		/// </summary>
		public event EventHandler<PacketReceivedEventArgs<Int64>> OnInt64Received;

		/// <summary>
		/// Called when receiving a packet ID that is not defined internally and in the user's packet list
		/// </summary>
		public event EventHandler<UndefinedPacketEventArgs> OnUndefinedPacketReceived;

		/// <summary>
		/// Access to data types for custom packets
		/// </summary>
		public RequestHandler ResponseGenerator { get; }

		#endregion

		#region Internal and private fields

		private readonly TcpClient baseClient;
		private readonly NetworkStream mainNetworkStream;

		private readonly Queue<SendQueueItem> queuedData;

		internal event EventHandler<TCPResponse>? OnResponse;

		internal readonly TCPClientInfo myInfo;
		internal readonly TCPClientInfo infoAboutOtherSide;

		internal readonly RequestCreator requestCreator;

		internal readonly DataIDs dataIDs;

		internal readonly IValueProvider valueProvider;

		internal readonly SerializationConfiguration serializationConfig;

		#endregion

		internal TCPConnection(TcpClient baseClient, TCPClientInfo myInfo, TCPClientInfo infoAboutOtherSide,
							   IValueProvider valueProvider, SerializationConfiguration serializationConfig,
							   EventHandler<PacketReceivedEventArgs<string>> strReceived,
							   EventHandler<PacketReceivedEventArgs<long>> int64Received, 
							   EventHandler<UndefinedPacketEventArgs> undefinedReceived) {
			this.infoAboutOtherSide = infoAboutOtherSide;
			this.myInfo = myInfo;
			this.baseClient = baseClient;
			this.valueProvider = valueProvider;
			this.serializationConfig = serializationConfig;
			OnStringReceived += strReceived;
			OnInt64Received += int64Received;
			OnUndefinedPacketReceived += undefinedReceived;
			
			queuedData = new Queue<SendQueueItem>();

			mainNetworkStream = baseClient.GetStream();
			dataIDs = new DataIDs(this);

			new Thread(SendDataFromQueue) { Name = $"Owner:{myInfo.Name}, IsClient={myInfo.IsClient} Sender Thread" }.Start();
			new Thread(DataReception) { Name = $"Owner:{myInfo.Name}, IsClient={myInfo.IsClient} Receiver Thread" }.Start();

			requestCreator = new RequestCreator(this);
			ResponseGenerator = new RequestHandler(this);
		}

		#region Send Primitives (string and long)

		/// <summary>
		/// Send UTF-8 encoded string to connected device
		/// </summary>
		public void SendData(string data) {
			byte[] bytes = Encoding.UTF8.GetBytes(data);
#if DEBUG
			Console.WriteLine("Sending data of type string of length {0}", bytes.Length + DataIDs.PACKET_ID_COMPLEXITY + DataIDs.PACKET_TOTAL_HEADER_SIZE_COMPLEXITY);
#endif
			SendData(DataIDs.STRING_ID, myInfo.ID, bytes);
		}

		/// <summary>
		/// Send Int64 (long) to connected device
		/// </summary>
		public void SendData(Int64 data) {
			byte[] bytes = BitConverter.GetBytes(data);
#if DEBUG
			Console.WriteLine("Sending data of type Int64 of length {0}", bytes.Length + DataIDs.PACKET_ID_COMPLEXITY + DataIDs.PACKET_TOTAL_HEADER_SIZE_COMPLEXITY);
#endif
			SendData(DataIDs.INT64_ID, myInfo.ID, bytes);
		}

		#endregion

		/// <summary>
		/// Send a single byte to the connected device
		/// </summary>
		internal void SendData(byte packetID, byte requestID) {
			SendData(packetID, myInfo.ID, new[] { requestID });
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
		/// <exception cref="InvalidOperationException">The packet ID is not defined</exception>
		public void SendData(byte packetID, byte[] data) {
			if (!dataIDs.customIDs.ContainsKey(packetID)) {
				throw new InvalidOperationException("Trying to send data with " + packetID + ", but this data was not defined!");
			}
			SendData(packetID, myInfo.ID, data);
		}

		/// <summary>
		/// Send custom data type using internal serialization/deserialization mechanisms
		/// </summary>
		/// <exception cref="InvalidOperationException">The data is not marked as [Serializable]</exception>
		/// <exception cref="System.Runtime.Serialization.SerializationException">When data fails to serialize</exception>
		/// <exception cref="ArgumentNullException">The data to send is <see langword="null"/></exception>
		public void SendData<TData>(byte packetID, TData data) {
			if (data == null) throw new ArgumentNullException(nameof(data), "Cannot send null!");
			
			if (!typeof(TData).IsSerializable && !serializationConfig.ContainsSerializationRule(typeof(TData))) {
				throw new InvalidOperationException("Trying to send data that is not marked as [Serializable]");
			}
			SendData(packetID, myInfo.ID, SimpleTCPHelper.GetBytesFromObject(data, serializationConfig));
		}

		/// <summary>
		/// Send data immediately on current thread
		/// </summary>
		internal void SendDataImmediate(byte packetID, byte[] data) {
			SendData(new SendQueueItem(packetID, myInfo.ID, data));
		}

		#region Queue sending

		protected readonly ManualResetEventSlim evnt = new();

		private void SendDataFromQueue() {
			while (SendingData) {
				evnt.Reset();
				if (queuedData.Count > 0) {
					SendData(queuedData.Dequeue());
				}
				else {
					evnt.Wait();
				}
			}
		}

		private void SendData(SendQueueItem item) {
			byte[] packetSize = BitConverter.GetBytes(item.RawData.LongLength);
			byte[] merged = new byte[item.RawData.Length + DataIDs.PACKET_ID_COMPLEXITY + DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY + packetSize.Length];
			packetSize.CopyTo(merged, 0); //Add data size to the beginning of the packet
			merged[packetSize.Length] = item.PacketID; //Append packetID
			merged[packetSize.Length + DataIDs.PACKET_ID_COMPLEXITY] = item.OriginClientID; //Append senderID
			item.RawData.CopyTo(merged, packetSize.Length + DataIDs.PACKET_ID_COMPLEXITY + DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY); //Append the data
			mainNetworkStream.Write(merged, 0, merged.Length);
		}

		#endregion

		#region Data reception

		internal void DataReception() {
			while (ListeningForData) {
				ReceivedData data;
				try {
					data = ReceiveData();
				}
				catch (ObjectDisposedException) {
					ListeningForData = false;
					return;
				}
				catch (IOException e) {
					if (e.InnerException is SocketException socketE) {
						if (socketE.SocketErrorCode == SocketError.Interrupted) {
							ListeningForData = false;
							return;
						}
					}
					continue;
				}

				if (!ListeningForData) {
					return;
				}

				if (data.DataType == typeof(SocketException)) {
					ListeningForData = false;
					return;
				}

				if (data.DataType == typeof(UndefinedPacketEventArgs)) {
					OnUndefinedPacketReceived?.Invoke(this, new UndefinedPacketEventArgs(infoAboutOtherSide.ID, data.DataID, (byte[])data.ReceivedObject));
				}

				#region Primitives

				if (data.DataID == 0) {
					if (OnStringReceived == null) {
						throw new NullReferenceException("Received a string packet, but the OnStringReceived event is not consumed!");
					}
					OnStringReceived(this, new PacketReceivedEventArgs<string>((string)data.ReceivedObject, data.SenderID));
				}
				else if (data.DataID == 1) {
					if (OnInt64Received == null) {
						throw new NullReferenceException("Received an Int64(long) packet, but the OnInt64Received event is not consumed!");
					}
					OnInt64Received(this, new PacketReceivedEventArgs<Int64>((Int64)data.ReceivedObject, data.SenderID));
				}

				#endregion

				if (dataIDs.customIDs.ContainsKey(data.DataID)) {
					dataIDs.customIDs[data.DataID].ActionCallback.Invoke(data.SenderID, data.ReceivedObject);
					continue;
				}

				if (data.DataID == DataIDs.RESPONSE_RECEPTION_ID) {
					OnResponse?.Invoke(this, (TCPResponse)data.ReceivedObject);
				}
				else if (data.DataID != DataIDs.REQUEST_RECEPTION_ID) {
					HigherLevelDataReceived(data);
				}
			}
		}

		/// <summary>
		/// Override in specific connection to handle higher level data
		/// </summary>
		/// <param name="data">The received data</param>
		protected virtual void HigherLevelDataReceived(ReceivedData data) { }

		private ReceivedData ReceiveData() {

			#region Working with the stream

#if DEBUG
			Console.WriteLine("Waiting for next packet...");
#endif

			byte[] packetSize = new byte[DataIDs.PACKET_TOTAL_HEADER_SIZE_COMPLEXITY];
			byte[] packetIDBytes = new byte[DataIDs.PACKET_ID_COMPLEXITY];
			byte[] fromClient = new byte[DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY];

			Int64 totalReceived = 0;

			while (totalReceived < packetSize.Length && ListeningForData) {
				totalReceived += mainNetworkStream.Read(packetSize, 0, packetSize.Length);
				if (totalReceived == 0) {
#if DEBUG
					Console.WriteLine("Closing...");
#endif
					return new ReceivedData(typeof(SocketException), 0, 0, 0);
				}
			}
			totalReceived = 0;
			Int64 toReceive = BitConverter.ToInt64(packetSize, 0);
			while (totalReceived < DataIDs.PACKET_ID_COMPLEXITY) {
				totalReceived += mainNetworkStream.Read(packetIDBytes, 0, DataIDs.PACKET_ID_COMPLEXITY);
			}
			totalReceived = 0;
			while (totalReceived < DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY && ListeningForData) {
				totalReceived += mainNetworkStream.Read(fromClient, 0, DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY);
			}
#if DEBUG
			Console.WriteLine("Waiting for Data 0/" + toReceive + " bytes");
#endif

			byte[] data = new byte[toReceive];
			totalReceived = 0;
			while (totalReceived < toReceive && ListeningForData) {
				totalReceived += mainNetworkStream.Read(data, (int)totalReceived, (int)(toReceive - totalReceived));
#if DEBUG
				Console.WriteLine("Waiting for Data " + totalReceived + "/" + toReceive + " bytes");
#endif
			}

			#endregion

			byte senderID = fromClient[0];
			byte packetID = packetIDBytes[0];

			Type dataType = dataIDs.IdentifyID(packetID, senderID, data);
			object dataObject;

			switch (packetID) {
				case DataIDs.STRING_ID:
					dataObject = Encoding.UTF8.GetString(data);
					break;
				case DataIDs.INT64_ID:
					dataObject = BitConverter.ToInt64(data, 0);
					break;
				default: {
					if (dataType == typeof(TCPResponse)) {
						TCPResponse resp;
						dataObject = resp = new TCPResponse(data[0], new byte[data.Length - 1], ResponseGenerator.GetResponseType(data[0]), serializationConfig);
						Array.Copy(data, 1, resp.RawData, 0, resp.RawData.Length);
					}
					else if (dataType == typeof(TCPRequest)) {
						dataObject = data[0];
					}
					else if (dataType == typeof(OnPropertySynchronizationEventArgs) || dataType == typeof(UndefinedPacketEventArgs)) {
						dataObject = data;
					}
					else if (dataType == typeof(TCPClientInfo)) {
						dataObject = SimpleTCPHelper.GetObject(dataType, data, serializationConfig);
					}
					else {
						dataObject = SimpleTCPHelper.GetObject(dataIDs.customIDs[packetID].DataType, data, serializationConfig);
					}
					break;
				}
			}

			return new ReceivedData(dataType, senderID, packetID, dataObject);
		}

		#endregion

		#region IDisposable Support

		private bool disposedValue;

		public void Dispose() {
			if (disposedValue) return;

			requestCreator.Dispose();
			SendingData = false;
			evnt.Set();
			ListeningForData = false;
			baseClient.Close();
			baseClient.Dispose();
			disposedValue = true;
		}

		#endregion
	}
}