using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleTCP.Events;
using SimpleTCP.Structures;

namespace SimpleTCP.Tests {
	[TestClass]
	public class ServerClientTests_RejectClient {
		[TestMethod]
		public async Task Reject() {
			using TCPServer server = new(new ServerConfiguration());
			server.OnClientConnectionAttempt += ServerOnClientConnectionAttempt;

			using TCPClient client = new(SimpleTCPHelper.GetActiveIPv4Address(), 55550);

			await server.Start(55550);

			bool res = await client.ConnectAsync(1000);

			if (res) {
				Assert.Fail();
			}

			server.Stop();
		}

		private void ServerOnClientConnectionAttempt(object sender, ClientConnectionAttemptEventArgs e) {
			e.Allow = false;
		}
	}
}