using System;
using System.Net.Sockets;
using SimpleTCP.Events;
using SimpleTCP.Structures;

namespace SimpleTCP.Connections {
	/// <summary>
	/// Server to client connection holder, processes higher level data
	/// </summary>
	public sealed class ServerToClientConnection : TCPConnection {
		internal event EventHandler<ClientDisconnectedEventArgs> _OnClientDisconnected;

		private readonly TCPServer server;

		internal ServerToClientConnection(TcpClient client, TCPClientInfo serverInfo,
										  TCPClientInfo clientInfo, TCPServer server,
										  SerializationConfiguration config, EventHandler<ClientDisconnectedEventArgs> onDisconnect)
			: base(client, serverInfo, clientInfo, server, config,
				   (_, _) => { }, (_, _) => { }, (_, _) => { }) {
			this.server = server;
			_OnClientDisconnected += onDisconnect;
		}

		/// <summary>
		/// Handle higher level data that only server can receive and react to
		/// </summary>
		protected override void HigherLevelDataReceived(ReceivedData data) {
			if (data.DataType == typeof(TCPClient)) {
				DisconnectClient(data.SenderID);
				server.connectedClients.Remove(data.SenderID);
			}
			if (data.DataID == DataIDs.CLIENT_DISCONNECTED) {
				SendingData = false;
				evnt.Set();
				ListeningForData = false;
				server.connectedClients.Remove(data.SenderID);
				_OnClientDisconnected.Invoke(this, new ClientDisconnectedEventArgs(infoAboutOtherSide, Enums.DisconnectType.Success));
				Dispose();
			}
			if (data.DataType == typeof(SocketException)) {
				server.connectedClients.Remove(data.SenderID);
				_OnClientDisconnected.Invoke(this, new ClientDisconnectedEventArgs((TCPClientInfo)data.ReceivedObject, Enums.DisconnectType.Interrupted));
			}
		}

		/// <summary>
		/// Send a disconnect packet to the client and stop all communication with said client
		/// </summary>
		public void DisconnectClient(byte clientID) {
			if (ListeningForData) {
				SendDataImmediate(DataIDs.CLIENT_DISCONNECTED, new[] { clientID });
				SendingData = false;
				evnt.Set();
				_OnClientDisconnected.Invoke(this, new ClientDisconnectedEventArgs(infoAboutOtherSide, Enums.DisconnectType.Kicked));
				ListeningForData = false;
				Dispose();
			}
		}
	}
}