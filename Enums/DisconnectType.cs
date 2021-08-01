namespace SimpleTCP.Enums {
	/// <summary>
	/// Reasons for the disconnect
	/// </summary>
	public enum DisconnectType {
		/// <summary>
		/// Client disconnected successfully
		/// </summary>
		Success,
		/// <summary>
		/// Client dropped connection to the server
		/// </summary>
		Interrupted,
		/// <summary>
		/// Client disconnected by server (kicked)
		/// </summary>
		Kicked
	}
}