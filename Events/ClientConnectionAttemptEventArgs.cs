using SimpleTCP.Structures;

namespace SimpleTCP.Events {
	/// <summary>
	/// Event data provided when client attempts to connect to the server
	/// </summary>
	public class ClientConnectionAttemptEventArgs {
		public ClientConnectionAttemptEventArgs(TCPClientInfo connectedClientInfo) {
			Info = connectedClientInfo;
		}
		
		public bool Allow { get; set; } = true;
		
		public TCPClientInfo Info { get; }
	}
}