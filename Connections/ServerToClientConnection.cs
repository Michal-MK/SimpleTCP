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
			if (data.DataType == typeof(TCPClient)) {
				DisconnectClient(data.SenderID);
			}
			if (data.DataID == DataIDs.ClientDisconnected) {
				_OnClientDisconnected.Invoke(this, new ClientDisconnectedEventArgs((TCPClientInfo)data.ReceivedObject, Enums.DisconnectType.Success));
			}
			if (data.DataType == typeof(SocketException)) {
				_OnClientDisconnected.Invoke(this, new ClientDisconnectedEventArgs((TCPClientInfo)data.ReceivedObject, Enums.DisconnectType.Interrupted));
			}
		}

		/// <summary>
		/// Send a disconnect packet to the client and stop all communication with said client
		/// </summary>
		public void DisconnectClient(byte clientID) {
			if (ListeningForData) {
				SendDataImmediate(DataIDs.ClientDisconnected, new byte[] { clientID });
				SendingData = false;
				_OnClientDisconnected.Invoke(this, new ClientDisconnectedEventArgs(infoAboutOtherSide, Enums.DisconnectType.Kicked));
				ListeningForData = false;
				Dispose();
			}
		}
	}
}
