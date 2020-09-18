using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Igor.TCP {
	public partial class ServerClientTests {

		[TestMethod]
		public async Task RequestResponse() {
			TCPServer server = new TCPServer(new ServerConfiguration());

			TCPClient client = new TCPClient(SimpleTCPHelper.GetActiveIPv4Address(), 5656);

			await server.Start(5656);
			const byte PACKET_ID = 4;

			client.Connect(() => {
				client.ProvideValue(PACKET_ID, ClientString);
			});

			await Task.Delay(100);


			server.ProvideValue(1, PACKET_ID, ServerString);

			string resp = await server.GetValue<string>(1, PACKET_ID);

			Assert.IsTrue(resp.GetType() == typeof(string));
			Assert.IsTrue(resp == ClientString());

			string resp2 = await client.GetValue<string>(PACKET_ID);

			Assert.IsTrue(resp2.GetType() == typeof(string));
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
