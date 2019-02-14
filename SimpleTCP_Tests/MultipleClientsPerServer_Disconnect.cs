using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Igor.TCP {

	public partial class MultipleClientsPerServer {

		private ManualResetEventSlim evnt = new ManualResetEventSlim();

		[TestMethod]
		public async Task DisconnectingMultipleClients() {

			TCPServer server = new TCPServer(new ServerConfiguration());

			TCPClient client1 = new TCPClient(SimpleTCPHelper.GetActiveIPv4Address(), 55550);
			client1.SetUpClientInfo("Client 1");
			TCPClient client2 = new TCPClient(SimpleTCPHelper.GetActiveIPv4Address(), 55550);
			client2.SetUpClientInfo("Client 2");

			await server.Start(55550);

			client1.Connect();
			client2.Connect();

			await Task.Delay(100);

			client1.Disconnect();
			Task.Run(Continue);
			evnt.Wait();
			await server.Stop();
		}

		private async Task Continue() {
			await Task.Delay(8000);
			evnt.Set();
		}
	}
}
