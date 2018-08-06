using System;

namespace Igor.TCP {
	[Serializable]
	public class ConnectionData {
		public string ipAddress { get; private set; }
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