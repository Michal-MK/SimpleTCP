using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Igor.TCP {

	[TestClass]
	public partial class MultipleClientsPerServer {

		[TestMethod]
		public async Task ConnectingMultipleClients() {
			TCPServer server = new TCPServer(new ServerConfiguration());

			TCPClient client1 = new TCPClient(SimpleTCPHelper.GetActiveIPv4Address(), 55550);
			client1.SetUpClientInfo("Client 1");
			TCPClient client2 = new TCPClient(SimpleTCPHelper.GetActiveIPv4Address(), 55550);
			client2.SetUpClientInfo("Client 2");

			await server.Start(55550);

			client1.Connect(null);
			client2.Connect(null);

			await Task.Delay(100);
			Assert.IsTrue(client1.getConnection != null);
			Assert.IsTrue(client2.getConnection != null);
			Assert.IsTrue(server.getConnectedClients.Length == 2);
			Assert.IsTrue(server.getConnectedClients[0].isServer == false);
			Assert.IsTrue(server.getConnectedClients[0].clientID == 1);
			Assert.IsTrue(server.getConnectedClients[0].computerName == "Client 1");
			Assert.IsTrue(server.getConnectedClients[1].isServer == false);
			Assert.IsTrue(server.getConnectedClients[1].clientID == 2);
			Assert.IsTrue(server.getConnectedClients[1].computerName == "Client 2");

			Assert.ThrowsException<InvalidOperationException>(() => { server.GetConnection(0); });
			Assert.ThrowsException<NullReferenceException>(() => { server.GetConnection(3); });
			Assert.IsNotNull(server.GetConnection(1));
			Assert.IsNotNull(server.GetConnection(2));

			await server.Stop();
		}
	}
}
