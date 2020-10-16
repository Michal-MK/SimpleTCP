using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Igor.TCP {
	public partial class ServerClientTests {
		bool disconnectEvent = false;

		[TestMethod]
		public async Task Disconnect() {
			TCPServer server = new TCPServer(new ServerConfiguration());
			TCPClient client = new TCPClient(SimpleTCPHelper.GetActiveIPv4Address(), 55550);

			server.OnClientDisconnected += Server_OnClientDisconnected;

			await server.Start(55550);
			await client.ConnectAsync(1000);

			client.Disconnect();

			await Task.Delay(100);
			Assert.IsTrue(server.ConnectedClients.Length == 0);
			Assert.ThrowsException<NullReferenceException>(() => { server.GetConnection(1); });
			Assert.IsNull(client.Connection);
			Assert.IsTrue(disconnectEvent);

			server.Stop();
		}

		private void Server_OnClientDisconnected(object sender, ClientDisconnectedEventArgs e) {
			disconnectEvent = true;
		}
	}
}
