using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleTCP.Structures;
using SimpleTCP.Tests.Base;

namespace SimpleTCP.Tests {
	[TestClass]
	public class ServerClientTests_Kick : TestBase {

		private bool kickedCalled;

		[TestMethod]
		public async Task KickClient() {
			using TCPServer server = new (new ServerConfiguration());

			using TCPClient client = new (SimpleTCPHelper.GetActiveIPv4Address(), 55550);

			await server.Start(55550);

			client.OnClientDisconnected += Client_OnDisconnectedByTheServer;
			bool res = await client.ConnectAsync(1000);

			if (!res) {
				Assert.Fail();
			}

			Assert.IsTrue(server.ConnectedClients.Length == 1);
			Assert.IsNotNull(client.Connection);

			Assert.IsTrue(server.ConnectedClients[0].IsServer == false);

			Assert.IsTrue(client.Connection.ListeningForData);
			Assert.IsTrue(client.Connection.SendingData);
			Assert.IsTrue(client.Info.ID == 1);

			server.DisconnectClient(client.Info.ID);

			await Task.Run(Wait);

			Assert.IsTrue(kickedCalled);
			server.Stop();
		}

		private void Client_OnDisconnectedByTheServer(object sender, EventArgs e) {
			kickedCalled = true;
			evnt.Set();
		}
	}
}
