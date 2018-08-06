using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

#if UNITY_STANDALONE || UNITY_ANDROID
using UnityEngine;
#endif

namespace Igor.TCP {
	public class TCPServer : TCPConnection {

		private TcpClient connected;
		public RequestManager requestHandler { get; }
		public ResponseManager responseHandler { get; }

		public event EventHandler<TCPServer> OnConnectionEstablished;
		public event EventHandler<TCPResponse> OnRequestHandeled;

		public TCPServer(): base(true) {
			requestHandler = new RequestManager(this);
			responseHandler = new ResponseManager(dataIDs);
		}

		public void Start(ushort port) {
			Thread t = new Thread(() => { StartServer(port); });
			t.Start();
		}

		public void StopListening() {
			listeningForData = false;
		}

		private void StartServer(ushort port) {
			IPAddress addr = Helper.GetActivePIv4Address();
			Console.WriteLine(addr);
			TcpListener listener = new TcpListener(addr, port);
			listener.Start();
			connected = listener.AcceptTcpClient();
			stream = connected.GetStream();
			Console.WriteLine("Client connected");
			listeningForData = true;
			OnConnectionEstablished?.Invoke(this, this);
			DataReception();
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
	}
}
