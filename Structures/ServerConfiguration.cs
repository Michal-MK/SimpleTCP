namespace Igor.TCP {
	/// <summary>
	/// Server configuration class
	/// </summary>
	public class ServerConfiguration {
		/// <summary>
		/// Allows clients to send a '255' packet to shutdown the server
		/// </summary>
		public bool clientCanShutdownServer { get; private set; }
		
		/// <summary>
		/// Allows clients to send a '255' packet to shutdown the server
		/// </summary>
		public bool clientCanRequestFromServer { get; private set; }

		/// <summary>
		/// Default constructor, select which values you want to modify, the rest is set to defaults
		/// </summary>
		public ServerConfiguration(	bool allowClientsToShutdownTheServer	 = false,
									bool allowClientsToRaiseRequestsToServer = false) {
			clientCanShutdownServer = allowClientsToShutdownTheServer;
			clientCanRequestFromServer = allowClientsToRaiseRequestsToServer;
		}
	}
}
