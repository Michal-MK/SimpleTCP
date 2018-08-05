using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Igor.TCP {
	public class TCPServer : TCPConnection {

		private TcpClient connected;
		public TCPRequest requestHandler { get; }

		public event EventHandler<TCPServer> OnConnectionEstablished;

		public TCPServer() {
			requestHandler = new TCPRequest(this);
		}

		public void Start(ushort port) {
			Thread t = new Thread(() => { StartServer(port); });
			t.Start();
		}

		public void StopListening() {
			listeningForData = false;
		}

		private void StartServer(ushort port) {
			IPAddress addr;
			using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0)) {
				socket.Connect("8.8.8.8", 65530);
				IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
				addr = endPoint.Address;
			}
			bf.Binder = new MyBinder();

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
	}
}
