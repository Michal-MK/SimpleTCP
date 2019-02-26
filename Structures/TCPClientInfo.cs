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
		public string clientAddress { get; }

		/// <summary>
		/// The name of connected client, if not set up users computer name is used.
		/// </summary>
		public string computerName { get; }

		/// <summary>
		/// The ID assigned from server
		/// </summary>
		/// <exception cref="InvalidOperationException"></exception>
		public byte clientID {
			get {
				if (isValid) {
					return _clientID;
				}
				throw new InvalidOperationException("Client has not been given an ID yet");
			}
			internal set {
				_clientID = value;
				isValid = true;
			}
		}
		private byte _clientID = 255;

		internal bool isValid { get; set; } = false;
		/// <summary>
		/// Initialize new <see cref="TCPClientInfo"/>
		/// </summary>
		public TCPClientInfo(string computerName, bool isServer, IPAddress clientAddress) {
			this.isServer = isServer;
			this.clientAddress = clientAddress.ToString();
			this.computerName = computerName;
		}
	}
}