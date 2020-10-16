using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Igor.TCP {
	public partial class ServerClientTests {

		[TestMethod]
		public async Task ReconnectToServer() {
			TCPServer server = new TCPServer(new ServerConfiguration());

			TCPClient client = new TCPClient(SimpleTCPHelper.GetActiveIPv4Address(), 55550);

			await server.Start(55550);
			await client.ConnectAsync(1000);

			await Task.Delay(100);;
			client.Disconnect();

			await Task.Delay(100);;
			Assert.IsTrue(client.Connection == null);

			await client.ConnectAsync(1000);

			await Task.Delay(100);;
			Assert.IsTrue(client.ClientInfo.ClientID == 1);
			Assert.IsTrue(client.Connection != null);
			Assert.IsTrue(client.Connection.ListeningForData);
			Assert.IsTrue(client.Connection.SendingData);
			Assert.IsTrue(server.ConnectedClients.Length == 1);
			Assert.IsTrue(server.ConnectedClients[0].ClientID == 1);
			Assert.IsTrue(server.GetConnection(1).SendingData);
			Assert.IsTrue(server.GetConnection(1).ListeningForData);

			server.Stop();
		}
	}
}
