using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Igor.TCP {
	public partial class ServerClientTests {

		[TestMethod]
		public async Task RequestResponse() {
			TCPServer server = new TCPServer(new ServerConfiguration());

			TCPClient client = new TCPClient(SimpleTCPHelper.GetActiveIPv4Address(), 5656);

			await server.Start(5656);
			client.Connect();

			await Task.Delay(100);

			const byte PACKET_ID = 4;
			//TODO fix tests
			server.DefineTwoWayComunication<string>(1, PACKET_ID, ServerString);
			client.DefineTwoWayComunication<string>(PACKET_ID, ClientString);

			TCPResponse resp = await server.RaiseRequestAsync(1, PACKET_ID);

			Assert.IsTrue(resp.dataType == typeof(string));
			Assert.IsTrue(resp.packetID == PACKET_ID);
			Assert.IsTrue(resp.getObject.ToString() == ClientString());

			TCPResponse resp2 = await client.RaiseRequestAsync(PACKET_ID);

			Assert.IsTrue(resp2.dataType == typeof(string));
			Assert.IsTrue(resp2.packetID == PACKET_ID);
			Assert.IsTrue(resp2.getObject.ToString() == ServerString());

			await server.Stop();
		}

		private string ClientString() {
			return "Clients string";
		}

		private string ServerString() {
			return "Servers string";
		}
	}
}
