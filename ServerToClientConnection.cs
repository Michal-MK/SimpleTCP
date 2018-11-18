using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Igor.TCP {
	/// <summary>
	/// Server to client connection holder, processes higher level data
	/// </summary>
	public sealed class ServerToClientConnection : TCPConnection {

		internal event EventHandler<byte> _OnClientDisconnected;

		internal TCPServer server;

		internal ServerToClientConnection(TcpClient client, TCPClientInfo clientInfo, TCPServer server) : base(client, clientInfo) {
			this.server = server;
		}

		/// <summary>
		/// Handle higher level data that only server can receive and react to
		/// </summary>
		public override void HigherLevelDataReceived(ReceivedData data) {
			if (data.dataType == typeof(TCPClient)) {
				DisconnectClient(data.senderID);
			}
			if (data.dataID == DataIDs.ClientDisconnected) {
				_OnClientDisconnected.Invoke(this, data.senderID);
			}
			if(data.dataID == DataIDs.ServerStop && server.serverConfiguration.clientCanShutdownServer) {
				Task.Run(server.Stop);
			}
		}

		/// <summary>
		/// Send a disconnect packet to the client and stop all communication to said client
		/// </summary>
		public void DisconnectClient(byte clientID) {
			if (listeningForData) {
				SendDataImmediate(DataIDs.ClientDisconnected, new byte[] { clientID });
				sendingData = false;
				_OnClientDisconnected.Invoke(this, clientID);
				listeningForData = false;
				Dispose();
			}
		}
	}
}
