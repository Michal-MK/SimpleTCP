﻿using System;
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
		/// Stream on which all transimssion happens
		/// </summary>
		internal NetworkStream stream;

		/// <summary>
		/// Is client listening for incomming data
		/// </summary>
		public bool listeningForData { get; internal set; } = true;


		public bool sendingData { get; internal set; } = true;

		/// <summary>
		/// Print debug information to console
		/// </summary>
		public bool debugPrints { get; set; }

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
		internal event EventHandler<byte> _OnClientDisconnected;


		internal RequestManager requestHandler { get; }

		/// <summary>
		/// Access to datatypes for custom packets
		/// </summary>
		public ResponseManager responseHandler { get; }


		internal Thread senderThread;
		internal Thread receiverThread;

		/// <summary>
		/// Define simple data packets and get internal/externaly defined packet IDs
		/// </summary>
		public DataIDs dataIDs { get; }

		private Queue<Tuple<byte, byte[]>> queuedBytes = new Queue<Tuple<byte, byte[]>>();

		internal TCPConnection(TcpClient baseClient) {
			stream = baseClient.GetStream();
			dataIDs = new DataIDs(this);
			bf.Binder = new MyBinder();

			senderThread = new Thread(new ThreadStart(SendDataFromQueue)) { Name = "Sender Thread" };
			senderThread.Start();
			receiverThread = new Thread(new ThreadStart(DataReception)) { Name = "Data Reception" };
			receiverThread.Start();

			requestHandler = new RequestManager(this);
			responseHandler = new ResponseManager(dataIDs);
		}

		/// <summary>
		/// Send TCPData to connected device
		/// </summary>
		[Obsolete("TCPData is deprecated due to being too specific, useless to the public and generic functinality already exists, will be removed in future release")]
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
		///  Sends packet with id 'packetID' and additional byte 'subPacketID' as a first elemtent of inner array of 'subPacketID' and 'data'
		/// </summary>
		public void SendData(byte packetID, byte subPacketID, byte[] data) {
			byte[] merged = new byte[data.Length + 1];
			merged[0] = subPacketID;
			data.CopyTo(merged, 1);
			SendData(packetID, merged);
		}

		/// <summary>
		/// Main Send fuction, send byte[] of 'data' with packet ID 'packetID'
		/// </summary>
		internal void SendData(byte packetID, byte[] data) {
			queuedBytes.Enqueue(new Tuple<byte, byte[]>(packetID, data));
			evnt.Set();
		}

		/// <summary>
		/// Send user defined byte[] 'data' with 'dataID' as the first element, sent via <see cref="DataIDs.UserDefined"/> packet ID
		/// </summary>
		public void SendUserDefinedData(byte dataID, byte[] data) {
			byte[] ready = new byte[data.Length + DataIDs.PACKET_ID_COMPLEXITY];
			ready[0] = dataID;
			Array.Copy(data, 0, ready, 1, data.Length);
			SendData(DataIDs.UserDefined, ready);
		}


		#region Queue sending

		private ManualResetEventSlim evnt;
		internal void SendDataFromQueue() {
			evnt = new ManualResetEventSlim();
			while (sendingData) {
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
			if (data == new byte[] { DataIDs.ClientDisconnected }) {
				stream.Close();
				stream.Dispose();
				_OnClientDisconnected?.Invoke(this, 0);
				sendingData = false;
				evnt.Set();
			}
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
					if (OnStringReceived == null) {
						throw new NullReferenceException("Other client is sending a string packet, but the OnStringReceived event is not consumed!");
					}
					OnStringReceived(this, (string)receivedData);
				}
				else if (data == typeof(Int64)) {
					if (OnInt64Received == null) {
						throw new NullReferenceException("Other client is sending a Int64(long) packet, but the OnInt64Received event is not consumed!");
					}
					OnInt64Received(this, (Int64)receivedData);
				}
				else if (data == typeof(TCPResponse)) {
					_OnResponse(this, (TCPResponse)receivedData);
				}
				else if (data == typeof(TCPRequest)) {
					continue;
				}
				else if (data == typeof(TCPClient)) {
					byte clientID = ((byte[])receivedData)[0];
					_OnClientDisconnected(this, ((byte[])receivedData)[0]);
					listeningForData = false;
					sendingData = false;
					Dispose(clientID);
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
			return dataIDs.IndetifyID(packetID[0], out dataObj, data);
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
				disposedValue = true;
			}
		}

		/// <summary>
		/// Dispose of the object
		/// </summary>
		public void Dispose() {
			Dispose(true);
		}

		internal void Dispose(byte clientID) {
			SendData(DataIDs.ClientDisconnected, clientID);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}

