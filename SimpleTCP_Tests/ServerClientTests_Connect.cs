using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Igor.TCP {

	[TestClass]
	public class ServerClientTests_Connect : TestBase {

		private bool clientConnectedEventFired = false;

		[TestMethod]
		public async Task Connect() {
			using TCPServer server = new (new ServerConfiguration());
			server.OnClientConnected += Server_OnConnectionEstablished;
			using TCPClient client = new (SimpleTCPHelper.GetActiveIPv4Address(), 55550);

			await server.Start(55550);
			bool res = await client.ConnectAsync(1000);

			if (!res) {
				Assert.Fail();
				return;
			}

			await Task.Run(Wait);

			Assert.IsTrue(clientConnectedEventFired);
			Assert.IsTrue(server.ConnectedClients.Length == 1);
			Assert.IsNotNull(client.Connection);

			Assert.IsTrue(server.ConnectedClients[0].IsServer == false);

			Assert.IsTrue(server.ConnectedClients[0].Address == SimpleTCPHelper.GetActiveIPv4Address().ToString());
			Assert.IsTrue(server.ConnectedClients[0].Name == Environment.UserName);
			Assert.IsTrue(server.ConnectedClients[0].ID == 1);

			Assert.IsTrue(client.Connection.ListeningForData);
			Assert.IsTrue(client.Connection.SendingData);
			Assert.IsTrue(client.Info.ID == 1);

			server.Stop();
		}

		private void Server_OnConnectionEstablished(object sender, ClientConnectedEventArgs e) {
			clientConnectedEventFired = true;
			evnt.Set();
		}
	}
}
