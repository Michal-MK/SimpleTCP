using System;
using System.Net.Sockets;

namespace Igor.TCP {
	/// <summary>
	/// Event data provided when the server reaches its capacity
	/// </summary>
	public class FullCapacityEventArgs : EventArgs {

		internal FullCapacityEventArgs(TCPClientInfo[] clients, TCPServer server, TcpClient newClient) {
			ConnectedClients = clients;
			Server = server;
			IncomingClient = newClient;
		}
		
		/// <summary>
		/// Array of all clients currently connected to the server
		/// </summary>
		public TCPClientInfo[] ConnectedClients { get; }
		
		/// <summary>
		/// Reference to the server instance
		/// </summary>
		public TCPServer Server { get; }
		
		/// <summary>
		/// The incoming client information
		/// </summary>
		public TcpClient IncomingClient { get; }
	}
}