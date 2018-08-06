﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
#if UNITY_STANDALONE || UNITY_ANDROID
using UnityEngine;
#endif


namespace Igor.TCP {
	public class TCPClient : TCPConnection {
		private readonly IPAddress address;
		private readonly ushort port;

		private TcpClient server;

		public event EventHandler<TCPResponse> OnRequestHandeled;


		public RequestManager requestHandler { get; }
		public ResponseManager responseHandler { get; }


		public bool isListeningForData { get { return listeningForData; } }

		public TCPClient(string ipAddress, ushort port) : this(
			new ConnectionData(ipAddress, port)) {
		}


		public TCPClient(ConnectionData data) : base(false) {
			this.port = data.port;
			if (IPAddress.TryParse(data.ipAddress, out address)) {
				server = new TcpClient();
				server.Connect(address, port);
				stream = server.GetStream();
				requestHandler = new RequestManager(this);
				responseHandler = new ResponseManager(dataIDs);
#if UNITY_ANDROID || UNITY_STANDALONE
				Debug.Log("Connection Established");
#else
				Console.WriteLine("Connection Established");
#endif
			}
			else {
				throw new Exception("Entered Invalid IP Address!");
			}
		}


		public void DefineRequestResponseEntry<TData>(byte ID, Func<TData> function) {
			dataIDs.requestDict.Add(ID, typeof(TData));
			dataIDs.responseDict.Add(ID, function);
		}

		public void CancelRequestResponseID(byte ID) {
			dataIDs.requestDict.Remove(ID);
			dataIDs.responseDict.Remove(ID);
		}

		public void DefineRequestEntry<TData>(byte ID) {
			dataIDs.requestDict.Add(ID, typeof(TData));
		}

		public void CancelRequestID(byte ID) {
			dataIDs.requestDict.Remove(ID);
		}

		public void DefineResponseEntry<TData>(byte ID, Func<TData> function) {
			dataIDs.responseDict.Add(ID, function);
		}

		public void CancelResponseID(byte ID) {
			dataIDs.responseDict.Remove(ID);
		}


		public async Task RaiseRequestAsync(byte ID) {
			TCPResponse data = await requestHandler.Request(ID);
			OnRequestHandeled?.Invoke(ID, data);
		}


		public void ListenForData() {
			listeningForData = true;
			DataReception();
		}

		public void StopListening() {
			listeningForData = false;
		}
	}
}