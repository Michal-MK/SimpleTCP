using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Igor.TCP {
	[TestClass]
	public class ServerClientTests_SendUnknown : TestBase {
		private bool sendUnknownEventSuccess;

		[TestMethod]
		public async Task SendUnknownData() {
			using TCPServer server = new (new ServerConfiguration());
			server.OnClientConnected += Server_OnClientConnected;
			using TCPClient client = new (SimpleTCPHelper.GetActiveIPv4Address(), 55550);

			await server.Start(55550);
			await client.ConnectAsync(1000);

			client.Connection.SendData(64, (byte)50);

			await Task.Run(Wait);

			Assert.IsTrue(sendUnknownEventSuccess);
		}

		private void Server_OnClientConnected(object sender, ClientConnectedEventArgs e) {
			e.Server.GetConnection(e.ClientInfo.ID).OnUndefinedPacketReceived += ServerClientTests_OnUndefinedPacketReceived;
		}

		private void ServerClientTests_OnUndefinedPacketReceived(object sender, UndefinedPacketEventArgs e) {
			sendUnknownEventSuccess = e.PacketID == 64 && e.UnknownData[0] == 50;
			evnt.Set();
		}
	}
}
