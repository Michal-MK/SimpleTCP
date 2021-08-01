using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleTCP.Structures;
using SimpleTCP.Tests.Base;

namespace SimpleTCP.Tests {
	[TestClass]
	public class ServerClientTests_Requests : TestBase {
		[TestMethod]
		public async Task RequestResponse() {
			using TCPServer server = new(new ServerConfiguration());

			using TCPClient client = new(SimpleTCPHelper.GetActiveIPv4Address(), 5656);

			await server.Start(5656);
			const byte PACKET_ID = 4;

			await client.ConnectAsync(1000);
			client.ProvideValue(PACKET_ID, ClientString);

			server.ProvideValue(1, PACKET_ID, ServerString);

			string resp = await server.GetValue<string>(1, PACKET_ID);

			Assert.IsTrue(resp != null);
			Assert.IsTrue(resp == ClientString());

			string resp2 = await client.GetValue<string>(PACKET_ID);

			Assert.IsTrue(resp2 != null);
			Assert.IsTrue(resp2 == ServerString());

			server.Stop();
		}

		private string ClientString() {
			return "Clients string";
		}

		private string ServerString() {
			return "Servers string";
		}
	}
}