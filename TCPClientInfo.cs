using System;
using System.Net;

namespace Igor.TCP {
	[Serializable]
	public class TCPClientInfo {

		public bool isServer;
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