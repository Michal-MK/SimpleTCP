using Igor.TCP;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Igor.TCP {

	public partial class ServerClientTests {

		[TestMethod]
		public async Task Timeout() {
			TCPClient client = new TCPClient(new ConnectionData("192.168.1.222", 6544));
			bool res = await client.ConnectAsync(2000);

			if (res) { Assert.Fail(); }
		}
	}
}
