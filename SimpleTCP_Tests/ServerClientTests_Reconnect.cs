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
			client.Connect();

			await Task.Delay(100);;
			client.Disconnect();

			await Task.Delay(100);;
			Assert.IsTrue(client.getConnection == null);

			client.Connect();

			await Task.Delay(100);;
			Assert.IsTrue(client.clientInfo.clientID == 1);
			Assert.IsTrue(client.getConnection != null);
			Assert.IsTrue(client.getConnection.listeningForData);
			Assert.IsTrue(client.getConnection.sendingData);
			Assert.IsTrue(server.getConnectedClients.Length == 1);
			Assert.IsTrue(server.getConnectedClients[0].clientID == 1);
			Assert.IsTrue(server.GetConnection(1).sendingData);
			Assert.IsTrue(server.GetConnection(1).listeningForData);

			await server.Stop();
		}
	}
}
