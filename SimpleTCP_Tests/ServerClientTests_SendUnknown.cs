using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Igor.TCP {
	partial class ServerClientTests {

		public bool SendUnknownEventSuccess;

		[TestMethod]
		public async Task SendUnknownData() {
			TCPServer server = new TCPServer(new ServerConfiguration());
			server.OnClientConnected += Server_OnClientConnected;
			TCPClient client = new TCPClient(SimpleTCPHelper.GetActiveIPv4Address(), 55550);

			await server.Start(55550);
			await client.ConnectAsync(1000);

			client.Connection.SendData(64, (byte)50);

			await Task.Delay(200);

			Assert.IsTrue(SendUnknownEventSuccess);
		}

		private void Server_OnClientConnected(object sender, ClientConnectedEventArgs e) {
			e.Server.GetConnection(e.ClientInfo.ID).OnUndefinedPacketReceived += ServerClientTests_OnUndefinedPacketReceived;
		}

		private void ServerClientTests_OnUndefinedPacketReceived(object sender, UndefinedPacketEventArgs e) {
			SendUnknownEventSuccess = e.PacketID == 64 && e.UnknownData[0] == 50;
		}
	}
}
