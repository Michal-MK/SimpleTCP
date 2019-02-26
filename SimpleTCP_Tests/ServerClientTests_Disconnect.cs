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
			client.Connect(null);

			await Task.Delay(100);

			client.Disconnect();

			await Task.Delay(100);
			Assert.IsTrue(server.getConnectedClients.Length == 0);
			Assert.ThrowsException<NullReferenceException>(() => { server.GetConnection(1); });
			Assert.IsNull(client.getConnection);
			Assert.IsTrue(disconnectEvent);

			await server.Stop();
		}

		private void Server_OnClientDisconnected(object sender, ClientDisconnectedEventArgs e) {
			disconnectEvent = true;
		}
	}
}
