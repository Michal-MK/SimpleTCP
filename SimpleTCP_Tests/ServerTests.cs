using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Igor.TCP;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleTCP_Tests {

	[TestClass]
	public class ServerTests {

		#region Server construction default configuration

		private bool eventFired = false;

		[TestMethod]
		public async Task ServerStart() {
			TCPServer server = new TCPServer(new ServerConfiguration(true));
			server.OnServerStarted += Server_OnServerStarted;
			Assert.IsTrue(server.serverConfiguration.clientCanShutdownServer);

			await server.Start(65000);

			Assert.IsTrue(server.isListeningForClients);
			Assert.IsTrue(server.getConnectedClients.Length == 0);
			Assert.IsTrue(eventFired);

			Assert.ThrowsException<NullReferenceException>(() => { server.GetConnection(1); });

			await server.Stop();

			Assert.IsFalse(server.isListeningForClients);
			Assert.IsTrue(server.getConnectedClients.Length == 0);
		}

		private void Server_OnServerStarted(object sender, EventArgs e) {
			eventFired = true;
		}

		#endregion
	}
}
