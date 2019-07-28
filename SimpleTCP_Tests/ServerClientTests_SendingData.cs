using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;


namespace Igor.TCP {
	public partial class ServerClientTests {

		private byte sentByte = 128;

		[TestMethod]
		public async Task SendSimpleData() {
			TCPServer server = new TCPServer(new ServerConfiguration());

			TCPClient client = new TCPClient(SimpleTCPHelper.GetActiveIPv4Address(), 55550);

			await server.Start(55550);
			const byte PACKET_ID = 4;

			client.Connect(() => {
				client.DefineCustomPacket(PACKET_ID, (byte sender, byte value) => {
					Assert.IsTrue(value == sentByte);
				});
			});

			await Task.Delay(100);

			server.DefineCustomPacket(1, PACKET_ID, (byte sender, byte value) => 
			{ Assert.IsTrue(value == sentByte); });

			client.Connection.SendData(PACKET_ID, sentByte);

			server.GetConnection(1).SendData(PACKET_ID,sentByte);

			await Task.Delay(100);

			await server.Stop();
		}
	}
}
