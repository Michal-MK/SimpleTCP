using System;
using SimpleTCP.Structures;

namespace SimpleTCP.Events {
	/// <summary>
	/// Event data provided when a client successfully connects
	/// </summary>
	public class ClientConnectedEventArgs : EventArgs {

		internal ClientConnectedEventArgs(TCPServer myServer, TCPClientInfo clientInfo) {
			Server = myServer;
			ClientInfo = clientInfo;
		}

		/// <summary>
		/// Reference to the server that accepted this connection
		/// </summary>
		public TCPServer Server { get; }

		/// <summary>
		/// Basic Information about client
		/// </summary>
		public TCPClientInfo ClientInfo { get; }
	}
}
