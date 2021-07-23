using Igor.TCP;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Igor.TCP {

	public partial class ServerClientTests {

		[TestMethod]
		public async Task Die() {
			TCPServer s = new TCPServer(new ServerConfiguration());
			await s.Start(6544);
			TCPClient client = new TCPClient(new ConnectionData(SimpleTCPHelper.GetActiveIPv4Address().ToString(), 6544));

			bool res = await client.ConnectAsync(1000);

			if (!res) { Assert.Fail(); }

			await Task.Delay(500);
			byte clientID = client.Info.ID;
			client.Dispose();

			await Task.Delay(500);

			s.SendToAll(50);
			await Task.Delay(2000);
			s.GetConnection(clientID).SendData(50);
			await Task.Delay(2000);
			s.GetConnection(clientID).SendData(50);
			await Task.Delay(2000);
			s.SendToAll(50);
			await Task.Delay(100);

			s.Stop();
		}
	}
}
