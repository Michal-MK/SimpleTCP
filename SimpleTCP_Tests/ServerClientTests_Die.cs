using Igor.TCP;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Igor.TCP {
	[TestClass]
	public class ServerClientTests_Die : TestBase {

		// [TestMethod] Was not working as intended, a rewrite is needed with proper dropped connection detection
		public async Task Die() {
			using TCPServer s = new (new ServerConfiguration());
			await s.Start(6544);
			using TCPClient client = new (new ConnectionData(SimpleTCPHelper.GetActiveIPv4Address().ToString(), 6544));

			bool res = await client.ConnectAsync(1000);

			if (!res) {
				Assert.Fail();
			}

			await Task.Delay(500);
			
			client.Dispose();

			await Task.Delay(500);

			s.SendToAll(50);

			s.Stop();
		}
	}
}
