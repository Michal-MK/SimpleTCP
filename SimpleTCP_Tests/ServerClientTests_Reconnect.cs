using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleTCP.Structures;
using SimpleTCP.Tests.Base;

namespace SimpleTCP.Tests {
	[TestClass]
	public class ServerClientTests_Reconnect : TestBase {

		[TestMethod]
		public async Task ReconnectToServer() {
			using TCPServer server = new (new ServerConfiguration());

			using TCPClient client = new (SimpleTCPHelper.GetActiveIPv4Address(), 55550);

			await server.Start(55550);
			await client.ConnectAsync(1000);
			
			client.Disconnect();

			await Task.Delay(100);
			
			Assert.IsTrue(client.Connection == null);

			await client.ConnectAsync(1000);
			
			Assert.IsTrue(client.Info.ID == 1);
			Assert.IsTrue(client.Connection != null);
			Assert.IsTrue(client.Connection!.ListeningForData);
			Assert.IsTrue(client.Connection.SendingData);
			Assert.IsTrue(server.ConnectedClients.Length == 1);
			Assert.IsTrue(server.ConnectedClients[0].ID == 1);
			Assert.IsTrue(server.GetConnection(1).SendingData);
			Assert.IsTrue(server.GetConnection(1).ListeningForData);

			server.Stop();
		}
	}
}
