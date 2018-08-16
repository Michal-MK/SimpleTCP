using System.Net;
using System.Net.Sockets;

namespace Igor.TCP {
	/// <summary>
	/// Basic information about connected client
	/// </summary>
	public class ConnectionInfo {

		internal ConnectionInfo(IPAddress connectedAddress, byte connectionID, TcpClient baseClient, TCPConnection connection) {
			this.baseClient = baseClient;
			this.connectionID = connectionID;
			this.connectedAddress = connectedAddress;
			dataStream = baseClient.GetStream();
			this.connection = connection;
		}

		/// <summary>
		/// Origin of incomming connection
		/// </summary>
		public IPAddress connectedAddress { get; }
		/// <summary>
		/// Server-assigned ID for client
		/// </summary>
		public byte connectionID { get; }

		internal TcpClient baseClient { get; }
		internal NetworkStream dataStream { get; }
		internal TCPConnection connection { get; }
	}
}
