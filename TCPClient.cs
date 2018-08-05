using System;
using System.Net;
using System.Net.Sockets;


namespace Igor.TCP {
	public class TCPClient : TCPConnection {
		private readonly IPAddress address;
		private readonly ushort port;

		private TcpClient server;

		public TCPRequest requestHandler { get; }


		public bool isListeningForData { get { return listeningForData; } }

		public TCPClient(string ipAddress, ushort port) : this(
			new ConnectionData(ipAddress, port)) {
		}


		public TCPClient(ConnectionData data): base(false) {
			this.port = data.port;
			bf.Binder = new MyBinder();
			if (IPAddress.TryParse(data.ipAddress, out address)) {
				server = new TcpClient();
				server.Connect(address, port);
				stream = server.GetStream();
				requestHandler = new TCPRequest(this);
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


		public async void RaiseRequest<T>(byte ID) {
			T data = await requestHandler.Request<T>(ID);
			OnRequestFullfilled(ID, data);
		}

		private void OnRequestFullfilled<T>(object sender, T e) {
			int id = (int)sender;
			switch (id) {
				case 128: {
					Console.WriteLine(e.ToString());
					break;
				}
			}
		}


		public void ListenForData() {
			listeningForData = true;
			DataReception();
		}

		public void StopListening() {
			listeningForData = false;
		}

		public void ListenForRequests() {

		}
	}
}