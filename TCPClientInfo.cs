using System;
using System.Net;

namespace Igor.TCP {
	/// <summary>
	/// Basic information about this client, can be sent to sever
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
		/// The ID assigned from server<para>Value 255 is invalid/not yet assigned!</para> 
		/// </summary>
		public byte clientID { get; internal set; } = 255;

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