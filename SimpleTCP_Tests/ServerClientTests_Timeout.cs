using Igor.TCP;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Igor.TCP {
	[TestClass]
	public class ServerClientTests_Timeout : TestBase {
		[TestMethod]
		public async Task Timeout() {
			using TCPClient client = new(new ConnectionData("192.168.1.222", 6544));
			bool res = await client.ConnectAsync(2000);

			if (res) {
				Assert.Fail();
			}
		}
	}
}