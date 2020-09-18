using System;
using System.Linq;
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

			client1.Connect(OnClient1Connect);
			client2.Connect(OnClient2Connect);

			void OnClient1Connect() {
				Assert.IsTrue(client1.Connection != null);
				Assert.IsTrue(server.ConnectedClients.Where(w => w.ClientID == client1.ClientInfo.ClientID).Single().IsServer == false);
				Assert.IsTrue(server.ConnectedClients.Where(w => w.ClientID == client1.ClientInfo.ClientID).Single().Name == "Client 1");

			}
			void OnClient2Connect() {
				Assert.IsTrue(client2.Connection != null);
				Assert.IsTrue(server.ConnectedClients.Where(w => w.ClientID == client2.ClientInfo.ClientID).Single().IsServer == false);
				Assert.IsTrue(server.ConnectedClients.Where(w => w.ClientID == client2.ClientInfo.ClientID).Single().Name == "Client 2");
			}

			await Task.Delay(200);
			Assert.IsTrue(server.ConnectedClients.Length == 2);
			Assert.ThrowsException<InvalidOperationException>(() => { server.GetConnection(0); });
			Assert.ThrowsException<NullReferenceException>(() => { server.GetConnection(3); });
			Assert.IsNotNull(server.GetConnection(1));
			Assert.IsNotNull(server.GetConnection(2));

			server.Stop();
		}
	}
}
