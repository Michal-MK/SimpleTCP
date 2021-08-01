using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleTCP.Events;
using SimpleTCP.Structures;
using SimpleTCP.Tests.Base;

namespace SimpleTCP.Tests {
	[TestClass]
	public class ServerClientTests_Disconnect : TestBase {
		
		private bool disconnectEvent;

		[TestMethod]
		public async Task Disconnect() {
			using TCPServer server = new (new ServerConfiguration());
			using TCPClient client = new (SimpleTCPHelper.GetActiveIPv4Address(), 55550);

			client.SetUpClientInfo(nameof(ServerClientTests_Disconnect));
			
			server.OnClientDisconnected += Server_OnClientDisconnected;

			await server.Start(55550);
			await client.ConnectAsync(1000);

			client.Disconnect();

			await Task.Run(Wait);

			Assert.IsTrue(server.ConnectedClients.Length == 0);
			Assert.ThrowsException<InvalidOperationException>(() => { server.GetConnection(1); });
			Assert.IsNull(client.Connection);
			Assert.IsTrue(disconnectEvent);

			server.Stop();
		}

		private void Server_OnClientDisconnected(object sender, ClientDisconnectedEventArgs e) {
			disconnectEvent = true;
			evnt.Set();
		}
	}
}
