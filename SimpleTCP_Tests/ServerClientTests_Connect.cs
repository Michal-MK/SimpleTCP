using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Igor.TCP {

	[TestClass]
	public partial class ServerClientTests {

		private bool clientConnectedEventFired = false;

		[TestMethod]
		public async Task Connect() {
			TCPServer server = new TCPServer(new ServerConfiguration());
			server.OnClientConnected += Server_OnConnectionEstablished;
			TCPClient client = new TCPClient(SimpleTCPHelper.GetActiveIPv4Address(), 55550);

			await server.Start(55550);
			client.Connect(null);

			await Task.Delay(100);

			Assert.IsTrue(clientConnectedEventFired);
			Assert.IsTrue(server.getConnectedClients.Length == 1);
			Assert.IsNotNull(client.getConnection);

			Assert.IsTrue(server.getConnectedClients[0].isServer == false);

			Assert.IsTrue(Equals(server.getConnectedClients[0].clientAddress, SimpleTCPHelper.GetActiveIPv4Address()));
			Assert.IsTrue(server.getConnectedClients[0].computerName == Environment.UserName);
			Assert.IsTrue(server.getConnectedClients[0].clientID == 1);

			Assert.IsTrue(client.getConnection.listeningForData);
			Assert.IsTrue(client.getConnection.sendingData);
			Assert.IsTrue(client.clientInfo.clientID == 1);

			await server.Stop();
		}

		private void Server_OnConnectionEstablished(object sender, ClientConnectedEventArgs e) {
			clientConnectedEventFired = true;
		}
	}
}
