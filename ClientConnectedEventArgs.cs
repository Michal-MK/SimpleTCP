using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Igor.TCP {
	/// <summary>
	/// OnConnectionReceived event arguments
	/// </summary>
	public class ClientConnectedEventArgs : EventArgs {

		internal ClientConnectedEventArgs(TCPServer myServer, TCPClientInfo clientInfo) {
			this.myServer = myServer;
			this.clientInfo = clientInfo;
		}

		/// <summary>
		/// Reference to the server that accepted this connection
		/// </summary>
		public TCPServer myServer { get; }

		/// <summary>
		/// Basic Information about client
		/// </summary>
		public TCPClientInfo clientInfo { get; }
	}
}
