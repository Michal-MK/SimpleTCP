using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Igor.TCP {

	public partial class ServerClientTests {

		private bool kickedCalled = false;

		[TestMethod]
		public async Task KickClient() {
			TCPServer server = new TCPServer(new ServerConfiguration());
			server.OnClientConnected += Server_OnConnectionEstablished;
			TCPClient client = new TCPClient(SimpleTCPHelper.GetActiveIPv4Address(), 55550);

			await server.Start(55550);

			client.OnClientDisconnected += Client_OnDisconnectedByTheServer;
			bool res = await client.ConnectAsync(1000);

			if (!res) {
				Assert.Fail();
				return;
			}

			Assert.IsTrue(server.ConnectedClients.Length == 1);
			Assert.IsNotNull(client.Connection);

			Assert.IsTrue(server.ConnectedClients[0].IsServer == false);

			Assert.IsTrue(client.Connection.ListeningForData);
			Assert.IsTrue(client.Connection.SendingData);
			Assert.IsTrue(client.Info.ID == 1);

			await Task.Delay(100);

			server.DisconnectClient(client.Info.ID);

			await Task.Delay(100);

			Assert.IsTrue(kickedCalled);
			server.Stop();
		}

		private void Client_OnDisconnectedByTheServer(object sender, EventArgs e) {
			kickedCalled = true;
			(sender as TCPClient).Dispose();
		}
	}
}
