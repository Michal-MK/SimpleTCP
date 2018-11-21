using System;
using System.Net.Sockets;

namespace Igor.TCP {
	/// <summary>
	/// Client to server connection holder, processes higher level data
	/// </summary>
	public sealed class ClientToServerConnection : TCPConnection {
		internal event EventHandler _OnClientKickedFromServer;

		internal TCPClient client;

		internal ClientToServerConnection(TcpClient server, TCPClientInfo myInfo, TCPClientInfo serverInfo, TCPClient client)
			: base(server, myInfo, serverInfo) {
			this.client = client;
		}

		/// <summary>
		/// Disconnect and let server know that this client is disconnecting
		/// </summary>
		public void DisconnectFromServer(byte myID) {
			SendDataImmediate(DataIDs.ClientDisconnected, new byte[] { myID });
			Dispose();
		}

		/// <summary>
		/// Handle higher level data that only a Client can react to
		/// </summary>
		/// <param name="data"></param>
		public override void HigherLevelDataReceived(ReceivedData data) {
			if (data.dataID == DataIDs.ClientDisconnected) {
				_OnClientKickedFromServer?.Invoke(this, EventArgs.Empty);
			}
		}
	}
}
