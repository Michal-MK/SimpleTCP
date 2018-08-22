namespace Igor.TCP {
	/// <summary>
	/// Client Disconnected event data
	/// </summary>
	public class ClientDisconnectedEventArgs {

		internal ClientDisconnectedEventArgs(byte clientID) {
			this.clientID = clientID;
		}
		/// <summary>
		/// ID of the disconnected client
		/// </summary>
		public byte clientID { get; }
	}

}