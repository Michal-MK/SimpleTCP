using System;
using System.Net.Sockets;

namespace Igor.TCP {
	/// <summary>
	/// Server to client connection holder, processes higher level data
	/// </summary>
	public sealed class ServerToClientConnection : TCPConnection {

		internal event EventHandler<ClientDisconnectedEventArgs> _OnClientDisconnected;

		internal TCPServer server;

		internal ServerToClientConnection(TcpClient client, TCPClientInfo serverInfo, TCPClientInfo clientInfo, TCPServer server)
			: base(client, serverInfo, clientInfo, server) {
			this.server = server;
		}

		/// <summary>
		/// Handle higher level data that only server can receive and react to
		/// </summary>
		protected override void HigherLevelDataReceived(ReceivedData data) {
			if (data.dataType == typeof(TCPClient)) {
				DisconnectClient(data.senderID);
			}
			if (data.dataID == DataIDs.ClientDisconnected) {
				_OnClientDisconnected.Invoke(this, new ClientDisconnectedEventArgs(data.senderID, Enums.DisconnectType.Success));
			}
			if (data.dataType == typeof(SocketException)) {
				_OnClientDisconnected.Invoke(this, new ClientDisconnectedEventArgs(data.senderID, Enums.DisconnectType.Interrupted));
			}
		}

		/// <summary>
		/// Send a disconnect packet to the client and stop all communication to said client
		/// </summary>
		public void DisconnectClient(byte clientID) {
			if (listeningForData) {
				SendDataImmediate(DataIDs.ClientDisconnected, new byte[] { clientID });
				sendingData = false;
				_OnClientDisconnected.Invoke(this, new ClientDisconnectedEventArgs(clientID, Enums.DisconnectType.Kicked));
				listeningForData = false;
				Dispose();
			}
		}
	}
}
