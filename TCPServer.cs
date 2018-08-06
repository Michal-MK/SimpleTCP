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

		internal RequestManager requestHandler { get; }
		public ResponseManager responseHandler { get; }

		public event EventHandler<TCPServer> OnConnectionEstablished;
		public event EventHandler<TCPResponse> OnRequestHandeled;

		/// <summary>
		/// Initialize new Server
		/// </summary>
		public TCPServer() : base(true) {
			requestHandler = new RequestManager(this);
			responseHandler = new ResponseManager(dataIDs);
		}

		/// <summary>
		/// Start server using specified 'port' and internally found IP
		/// </summary>
		public void Start(ushort port) {
			Thread t = new Thread(() => { StartServer(Helper.GetActivePIv4Address(), port); }) { Name = "Actual server" };
			t.Start();
		}

		/// <summary>
		/// Start server using specified 'port' and explicitly specified 'ipAddress'
		/// </summary>
		public void Start(string ipAddress, ushort port) {
			Thread t = new Thread(() => { StartServer(IPAddress.Parse(ipAddress), port); }) { Name = "Actual server" };
			t.Start();
		}

		/// <summary>
		/// Stops listening for incomming data
		/// </summary>
		public void StopListening() {
			listeningForData = false;
		}


		private void StartServer(IPAddress address, ushort port) {
			Console.WriteLine(address);
			TcpListener listener = new TcpListener(address, port);
			listener.Start();
			connected = listener.AcceptTcpClient();
			stream = connected.GetStream();
			Console.WriteLine("Client connected");
			listeningForData = true;
			OnConnectionEstablished?.Invoke(this, this);
			new Thread(new ThreadStart(DataReception)) { Name = "DataReception" }.Start();
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

		/// <summary>
		/// Raises a new request with 'ID' and sends response via 'OnRequestHandeled' event
		/// </summary>
		public async Task RaiseRequestAsync(byte ID) {
			TCPResponse data = await requestHandler.Request(ID);
			OnRequestHandeled?.Invoke(ID, data);
		}
	}
}
