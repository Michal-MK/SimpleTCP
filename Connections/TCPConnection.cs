using System;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Igor.TCP {
	/// <summary>
	/// Base class for all client and server
	/// </summary>
	public class TCPConnection : IDisposable {

		#region Public Properties and fields
		/// <summary>
		/// Is client listening for incoming data
		/// </summary>
		public bool ListeningForData { get; internal set; } = true;

		/// <summary>
		/// Is client actively trying to send data
		/// </summary>
		public bool SendingData { get; internal set; } = true;

		/// <summary>
		/// Called when successfully received data by <see cref="DataIDs.StringID"></see> marked packet
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

		#region Internal and private properties/fields

		internal TcpClient baseClient;
		internal NetworkStream mainNetworkStream;

		internal event EventHandler<TCPResponse> _OnResponse;

		internal Thread senderThread;
		internal Thread receiverThread;

		internal TCPClientInfo myInfo;
		internal TCPClientInfo infoAboutOtherSide;

		internal RequestCreator requestCreator;
		internal Queue<SendQueueItem> queuedData = new Queue<SendQueueItem>();

		internal readonly DataIDs dataIDs;

		internal IValueProvider valueProvider;
		#endregion

		internal TCPConnection(TcpClient baseClient, TCPClientInfo myInfo, TCPClientInfo infoAboutOtherSide, IValueProvider valueProvider) {
			this.infoAboutOtherSide = infoAboutOtherSide;
			this.myInfo = myInfo;
			this.baseClient = baseClient;

			mainNetworkStream = baseClient.GetStream();
			dataIDs = new DataIDs(this);

			senderThread = new Thread(new ThreadStart(SendDataFromQueue)) { Name = $"Owner:{myInfo.Name}, IsClient={myInfo.IsClient} Sender Thread" };
			senderThread.Start();
			receiverThread = new Thread(new ThreadStart(DataReception)) { Name = $"Owner:{myInfo.Name}, IsClient={myInfo.IsClient} Receiver Thread" };
			receiverThread.Start();

			requestCreator = new RequestCreator(this);
			ResponseGenerator = new RequestHandler(this);

			this.valueProvider = valueProvider;
		}

		#region Send Primitives (string and long)

		/// <summary>
		/// Send UTF-8 encoded string to connected device
		/// </summary>
		public void SendData(string data) {
			byte[] bytes = System.Text.Encoding.UTF8.GetBytes(data);
#if DEBUG
			Console.WriteLine("Sending data of type string of length {0}", bytes.Length + DataIDs.PACKET_ID_COMPLEXITY + DataIDs.PACKET_TOTAL_HEADER_SIZE_COMPLEXITY);
#endif
			SendData(DataIDs.StringID, myInfo.ClientID, bytes);
		}

		/// <summary>
		/// Send Int64 (long) to connected device
		/// </summary>
		public void SendData(Int64 data) {
			byte[] bytes = BitConverter.GetBytes(data);
#if DEBUG
			Console.WriteLine("Sending data of type Int64 of length {0}", bytes.Length + DataIDs.PACKET_ID_COMPLEXITY + DataIDs.PACKET_TOTAL_HEADER_SIZE_COMPLEXITY);
#endif
			SendData(DataIDs.Int64ID, myInfo.ClientID, bytes);
		}

		#endregion

		/// <summary>
		/// Send a single byte to the connected device
		/// </summary>
		internal void SendData(byte packetID, byte requestID) {
			SendData(packetID, myInfo.ClientID, new byte[1] { requestID });
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
			SendData(packetID, myInfo.ClientID, data);
		}

		/// <summary>
		/// Send custom data type using internal serialization/deserialization mechanisms
		/// </summary>
		/// <exception cref="UndefinedPacketEventArgs"></exception>
		/// <exception cref="InvalidOperationException"></exception>
		public void SendData<TData>(byte packetID, TData data) {
			if (!typeof(TData).IsSerializable) {
				throw new InvalidOperationException("Trying to send data that is not marked as [Serializable]");
			}
			SendData(packetID, myInfo.ClientID, SimpleTCPHelper.GetBytesFromObject(data));
		}

		/// <summary>
		/// Send data immediately on current thread
		/// </summary>
		internal void SendDataImmediate(byte packetID, byte[] data) {
			SendData(new SendQueueItem(packetID, myInfo.ClientID, data));
		}

		#region Queue sending

		private ManualResetEventSlim evnt;

		internal void SendDataFromQueue() {
			using (evnt = new ManualResetEventSlim()) {
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
		}

		private void SendData(SendQueueItem item) {
			byte[] packetSize = BitConverter.GetBytes(item.RawData.LongLength);
			byte[] merged = new byte[item.RawData.Length + DataIDs.PACKET_ID_COMPLEXITY + DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY + packetSize.Length];
			packetSize.CopyTo(merged, 0); //Add data size to the beginning of the packet
			merged[packetSize.Length] = item.PacketID; //Append packetID
			merged[packetSize.Length + DataIDs.PACKET_ID_COMPLEXITY] = item.OriginClientID; //Append senderID
			item.RawData.CopyTo(merged, packetSize.Length + DataIDs.PACKET_ID_COMPLEXITY + DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY); //Append the data
			mainNetworkStream.Write(merged, 0, merged.Length);
			//TODO socket shutdown
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
					OnUndefinedPacketReceived?.Invoke(this, new UndefinedPacketEventArgs(data.DataID, (byte[])data.ReceivedObject));
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

				if (data.DataID == DataIDs.ResponseReceptionID) {
					_OnResponse(this, (TCPResponse)data.ReceivedObject);
				}
				else if (data.DataID == DataIDs.RequestReceptionID) {
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

#if DEBUG
			Console.WriteLine("Waiting for next packet...");
#endif

			byte[] packetSize = new byte[DataIDs.PACKET_TOTAL_HEADER_SIZE_COMPLEXITY];
			byte[] packetID = new byte[DataIDs.PACKET_ID_COMPLEXITY];
			byte[] fromClient = new byte[DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY];

			Int64 totalReceived = 0;

			while (totalReceived < packetSize.Length && ListeningForData) {
				totalReceived += mainNetworkStream.Read(packetSize, 0, packetSize.Length);
				if (totalReceived == 0) {
					return new ReceivedData(typeof(SocketException), 0, 0, 0);
				}
			}
			totalReceived = 0;
			Int64 toReceive = BitConverter.ToInt64(packetSize, 0);
			while (totalReceived < DataIDs.PACKET_ID_COMPLEXITY) {
				totalReceived += mainNetworkStream.Read(packetID, 0, DataIDs.PACKET_ID_COMPLEXITY);
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

			byte senderID = GetSenderID(fromClient);
			byte packetIDSingle = GetPacketID(packetID);

			Type dataType = dataIDs.IdentifyID(packetIDSingle, senderID, data);
			object dataObject;

			if (packetIDSingle == DataIDs.StringID) {
				dataObject = Encoding.UTF8.GetString(data);
			}
			else if (packetIDSingle == DataIDs.Int64ID) {
				dataObject = BitConverter.ToInt64(data, 0);
			}
			else if (dataType == typeof(TCPResponse)) {
				dataObject = new TCPResponse(data[0], new byte[data.Length - 1], ResponseGenerator.GetResponseType(data[0]));
				Array.Copy(data, 1, (dataObject as TCPResponse).RawData, 0, (dataObject as TCPResponse).RawData.Length);
			}
			else if (dataType == typeof(TCPRequest)) {
				dataObject = data[0];
			}
			else if (dataType == typeof(OnPropertySynchronizationEventArgs) || dataType == typeof(UndefinedPacketEventArgs)) {
				dataObject = data;
			}
			else if (dataType == typeof(TCPClientInfo)) {
				dataObject = SimpleTCPHelper.GetObject(dataType, data);
			}
			else {
				dataObject = SimpleTCPHelper.GetObject(dataIDs.customIDs[packetIDSingle].DataType, data);
			}

			return new ReceivedData(dataType, senderID, packetIDSingle, dataObject);
		}

		#region Helpers for getting senderID and packetID from byte[]

		internal static byte GetSenderID(byte[] fromClient) {
			if (DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY != 1) {
#pragma warning disable CS0162 // Unreachable code detected
				Debugger.Break();
#pragma warning restore CS0162 // Unreachable code detected	
				throw new Exception();
			}
			return fromClient[0];
		}

		internal static byte GetPacketID(byte[] packetID) {
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
				SendingData = false;
				evnt.Set();
				ListeningForData = false;
				baseClient.Close();
				baseClient.Dispose();
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

