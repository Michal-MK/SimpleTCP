using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Igor.TCP {
	class TCPServer : TCPConnection {

		private TcpClient connected;

		public void Start() {
			Thread t = new Thread(StartServer);
			t.Start();
		}

		public void StopListening() {
			listeningForData = false;
		}

		private void StartServer() {
			IPAddress addr;
			using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0)) {
				socket.Connect("8.8.8.8", 65530);
				IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
				addr = endPoint.Address;
			}
			bf.Binder = new MyBinder();

			Console.WriteLine(addr);
			TcpListener listener = new TcpListener(addr, 7890);
			listener.Start();
			connected = listener.AcceptTcpClient();
			stream = connected.GetStream();
			Console.WriteLine("Client connected");
			listeningForData = true;
			DataReception();
		}
	}
}
