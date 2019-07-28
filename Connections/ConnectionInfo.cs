using System.Net;
using System.Net.Sockets;

namespace Igor.TCP {
	internal class ConnectionInfo {
		internal ConnectionInfo(IPAddress connectedAddress, byte connectionID, TcpClient baseClient, TCPConnection connection) {
			BaseClient = baseClient;
			ConnectionID = connectionID;
			ConnectedAddress = connectedAddress;
			DataStream = baseClient.GetStream();
			Connection = connection;
		}

		internal IPAddress ConnectedAddress { get; }
		internal byte ConnectionID { get; }
		internal TcpClient BaseClient { get; }
		internal NetworkStream DataStream { get; }
		internal TCPConnection Connection { get; }
	}
}
