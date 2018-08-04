using System;
using System.Net;
using System.Net.Sockets;


namespace Igor.TCP {
	class TCPClient : TCPConnection {
		private readonly IPAddress address;
		private readonly ushort port;

		private TcpClient server;


		public TCPClient(string ipAddress, ushort port) : this(
			new ConnectionData(ipAddress, port)) {
			ConnectionData data = new ConnectionData("",0);
			
		}



		public TCPClient(ConnectionData data) {
			this.port = data.port;
			bf.Binder = new MyBinder();
			if (IPAddress.TryParse(data.ipAddress, out address)) {
				server = new TcpClient();
				server.Connect(address, port);
				stream = server.GetStream();
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
	}
}