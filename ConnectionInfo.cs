using System.Net;
using System.Net.Sockets;

namespace Igor.TCP {
	internal class ConnectionInfo {

		internal ConnectionInfo(IPAddress connectedAddress, byte connectionID, TcpClient baseClient, TCPConnection connection) {
			this.baseClient = baseClient;
			this.connectionID = connectionID;
			this.connectedAddress = connectedAddress;
			dataStream = baseClient.GetStream();
			this.connection = connection;
		}

		internal IPAddress connectedAddress { get; }
		internal byte connectionID { get; }
		internal TcpClient baseClient { get; }
		internal NetworkStream dataStream { get; }
		internal TCPConnection connection { get; }
	}
}
