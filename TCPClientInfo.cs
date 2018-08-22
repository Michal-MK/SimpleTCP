using System;
using System.Net;

namespace Igor.TCP {
	/// <summary>
	/// INY
	/// </summary>
	[Serializable]
	public class TCPClientInfo {
		/// <summary>
		/// Is current client playing the role of the server
		/// </summary>
		public bool isServer { get; }
		/// <summary>
		/// Address of current instance
		/// </summary>
		public IPAddress clientAddress { get; }

		/// <summary>
		/// The name of connected client, if not set up users computer name is used.
		/// </summary>
		public string computerName { get; }

		/// <summary>
		/// Initialize new ClientInfo
		/// </summary>
		public TCPClientInfo(string computerName, bool isServer, IPAddress clientAddress) {
			this.isServer = isServer;
			this.clientAddress = clientAddress;
			this.computerName = computerName;
		}
	}
}