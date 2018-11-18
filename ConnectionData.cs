using System;

namespace Igor.TCP {
	/// <summary>
	/// Holds IP address and port for connection
	/// </summary>
	[Serializable]
	public class ConnectionData {
		/// <summary>
		/// IP address to connect to
		/// </summary>
		public string ipAddress { get; private set; }
		/// <summary>
		/// Port to which to listen
		/// </summary>
		public ushort port { get; private set; }

		/// <summary>
		/// Initialize new ConnectionData, used for connecting to the server
		/// </summary>
		public ConnectionData(string ipAddress, ushort port) {
			this.ipAddress = ipAddress;
			this.port = port;
		}
	}
}