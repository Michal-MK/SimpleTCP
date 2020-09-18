using Igor.TCP;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Igor.TCP {

	public partial class ServerClientTests {

		private bool timedOut = false;

		[TestMethod]
		public async Task Timeout() {
			TCPClient client = new TCPClient(new ConnectionData("192.168.1.222", 6544));
			client.Connect(OnConnected, TimeSpan.FromSeconds(0.5), TimedOut);

			void OnConnected() {
				Assert.Fail();
			}
			await Task.Delay(1000);
			Assert.IsTrue(timedOut);
		}

		private void TimedOut() {
			timedOut = true;
		}
	}
}
