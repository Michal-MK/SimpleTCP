using System;

namespace Igor.TCP {
	[Serializable]
	public class ConnectionData {
		public string ipAddress { get; private set; }
		public ushort port { get; private set; }

		public event EventHandler<ConnectionData> OnConnectionDataParsed;

		public ConnectionData(string ipAddress, ushort port) {
			this.ipAddress = ipAddress;
			this.port = port;
			OnConnectionDataParsed(this, this);
		}
	}
}