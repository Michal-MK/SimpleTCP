using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Igor.TCP {
	/// <summary>
	/// OnConnectionReceived event args.
	/// </summary>
	public class ClientConnectedEventArgs : EventArgs {

		internal ClientConnectedEventArgs(TCPServer myServer, ConnectionInfo connInfo) {
			this.myServer = myServer;
			this.connInfo = connInfo;
		}

		/// <summary>
		/// Reference to the server that accepted this connection
		/// </summary>
		public TCPServer myServer { get; }

		/// <summary>
		/// Basic information about client
		/// </summary>
		public ConnectionInfo connInfo { get; }
	}
}
