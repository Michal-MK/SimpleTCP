using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleTCP.Connections;
using SimpleTCP.Structures;
using SimpleTCP.Tests.Base;

namespace SimpleTCP.Tests {
	[TestClass]
	public class ServerClientTests_Timeout : TestBase {
		[TestMethod]
		public async Task Timeout() {
			using TCPClient client = new(new ConnectionData("192.168.1.222", 6544), new ClientConfiguration());
			bool res = await client.ConnectAsync(2000);

			if (res) {
				Assert.Fail();
			}
		}
	}
}