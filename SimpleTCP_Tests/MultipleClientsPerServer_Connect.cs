using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Igor.TCP {

	[TestClass]
	public class MultipleClientsPerServer_Connect : TestBase {

		[TestMethod]
		public async Task ConnectingMultipleClients() {
			using TCPServer server = new (new ServerConfiguration());

			using TCPClient client1 = new (SimpleTCPHelper.GetActiveIPv4Address(), 55550);
			const string CLIENT1_NAME = "Client 1 " + nameof(MultipleClientsPerServer_Connect);
			client1.SetUpClientInfo(CLIENT1_NAME);
			using TCPClient client2 = new (SimpleTCPHelper.GetActiveIPv4Address(), 55550);
			const string CLIENT2_NAME = "Client 2 " + nameof(MultipleClientsPerServer_Connect);
			client2.SetUpClientInfo(CLIENT2_NAME);

			await server.Start(55550);

			await client1.ConnectAsync(1000);
			Assert.IsTrue(client1.Connection != null);

			Assert.IsTrue(server.ConnectedClients.Single(w => w.ID == client1.Info.ID).IsServer == false);
			Assert.IsTrue(server.ConnectedClients.Single(w => w.ID == client1.Info.ID).Name == CLIENT1_NAME);

			await client2.ConnectAsync(1000);
			Assert.IsTrue(client2.Connection != null);

			Assert.IsTrue(server.ConnectedClients.Single(w => w.ID == client2.Info.ID).IsServer == false);
			Assert.IsTrue(server.ConnectedClients.Single(w => w.ID == client2.Info.ID).Name == CLIENT2_NAME);

			Assert.IsTrue(server.ConnectedClients.Length == 2);
			Assert.ThrowsException<InvalidOperationException>(() => { server.GetConnection(0); });
			Assert.ThrowsException<InvalidOperationException>(() => { server.GetConnection(3); });
			Assert.IsNotNull(server.GetConnection(client1.Info.ID));
			Assert.IsNotNull(server.GetConnection(client2.Info.ID));

			server.Stop();
		}
	}
}