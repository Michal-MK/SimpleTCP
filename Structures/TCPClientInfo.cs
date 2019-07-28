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
		public bool IsServer { get; }

		/// <summary>
		/// Is current client playing the role of the client
		/// </summary>
		public bool IsClient => !IsServer;

		/// <summary>
		/// Address of current instance
		/// </summary>
		public IPAddress Address { get; }

		/// <summary>
		/// The name of connected client, if not set up users computer name is used.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// The ID assigned from server<para>Value 255 is invalid/not yet assigned!</para> 
		/// </summary>
		public byte ClientID { get; internal set; } = 255;

		/// <summary>
		/// Initialize new <see cref="TCPClientInfo"/>
		/// </summary>
		public TCPClientInfo(string computerName, bool isServer, IPAddress clientAddress) {
			IsServer = isServer;
			Address = clientAddress;
			Name = computerName;
		}
	}
}