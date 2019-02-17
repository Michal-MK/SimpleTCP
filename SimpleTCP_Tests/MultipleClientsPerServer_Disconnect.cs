using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Igor.TCP {

	public partial class MultipleClientsPerServer {

		[TestMethod]
		public async Task DisconnectingMultipleClients() {

			TCPServer server = new TCPServer(new ServerConfiguration());

			TCPClient client1 = new TCPClient(SimpleTCPHelper.GetActiveIPv4Address(), 55550);
			client1.SetUpClientInfo("Client 1");
			TCPClient client2 = new TCPClient(SimpleTCPHelper.GetActiveIPv4Address(), 55550);
			client2.SetUpClientInfo("Client 2");

			await server.Start(55550);

			client1.Connect(null);
			client2.Connect(null);

			await Task.Delay(100);

			client1.Disconnect();
			await Task.Delay(400);
			client2.Disconnect();

			await Task.Delay(200);
			await server.Stop();
		}
	}
}
