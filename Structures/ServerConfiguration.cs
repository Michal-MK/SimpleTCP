namespace Igor.TCP {
	/// <summary>
	/// Server configuration class
	/// </summary>
	public class ServerConfiguration {
		
		/// <summary>
		/// Default constructor, select which values you want to modify, the rest is set to defaults
		/// </summary>
		public ServerConfiguration(bool allowClientsToRaiseRequestsToServer = false) {
			ClientCanRequestFromServer = allowClientsToRaiseRequestsToServer;
		}

		/// <summary>
		/// Allows clients to make requests to the server
		/// </summary>
		public bool ClientCanRequestFromServer { get; }
	}
}
