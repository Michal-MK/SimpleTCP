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
		public bool isServer;
		/// <summary>
		/// Address of current instance
		/// </summary>
		public IPAddress clientAddress;

		/// <summary>
		/// Initialize new ClientInfo
		/// </summary>
		public TCPClientInfo(bool isServer, IPAddress clientAddress) {
			this.isServer = isServer;
			this.clientAddress = clientAddress;
		}
	}
}