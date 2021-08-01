using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Igor.TCP {
	[TestClass]
	public class ServerTests : TestBase {
		#region Server construction default configuration

		private bool eventFired;

		[TestMethod]
		public async Task ServerStart() {
			TCPServer server = new(new ServerConfiguration(true));
			server.OnServerStarted += Server_OnServerStarted;
			Assert.IsTrue(server.ServerConfiguration.ClientCanRequestFromServer);

			await server.Start(65000);

			Assert.IsTrue(server.IsListeningForClients);
			Assert.IsTrue(server.ConnectedClients.Length == 0);
			Assert.IsTrue(eventFired);

			Assert.ThrowsException<InvalidOperationException>(() => { server.GetConnection(1); });

			server.Stop();

			Assert.IsFalse(server.IsListeningForClients);
			Assert.IsTrue(server.ConnectedClients.Length == 0);
		}

		private void Server_OnServerStarted(object sender, EventArgs e) {
			eventFired = true;
		}

		#endregion
	}
}