using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleTCP.Structures;
using SimpleTCP.Tests.Base;

namespace SimpleTCP.Tests {
	[TestClass]
	public class ServerClientTests_SendingData : TestBase {
		private byte sentByte = 128;

		[TestMethod]
		public async Task SendSimpleData() {
			using TCPServer server = new(new ServerConfiguration());

			using TCPClient client = new(SimpleTCPHelper.GetActiveIPv4Address(), 55550);

			await server.Start(55550);
			const byte PACKET_ID = 4;

			await client.ConnectAsync(1000);
			client.DefineCustomPacket(PACKET_ID, (byte _, byte value) => { Assert.IsTrue(value == sentByte); evnt.Set(); });

			server.DefineCustomPacket(1, PACKET_ID, (byte _, byte value) => { Assert.IsTrue(value == sentByte); });

			client.Connection.SendData(PACKET_ID, sentByte);

			server.GetConnection(1).SendData(PACKET_ID, sentByte);

			await Task.Run(Wait);

			server.Stop();
		}
	}
}