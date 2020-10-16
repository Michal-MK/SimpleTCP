using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Igor.TCP {
	[TestClass]
	public class FreeTests {

		//[TestMethod]
		public async Task FreeTestOne() {
			TCPServer server = new TCPServer(new ServerConfiguration());

			server.OnClientConnected += (s,e) => {
				server.DefineCustomPacket<(int, string)>(e.ClientInfo.ClientID, 80, OnPacketReceived);
			};

			await server.Start(IPAddress.Loopback,8998); 


			TCPClient client = new TCPClient(IPAddress.Loopback, 8998);
			await client.ConnectAsync(1000);

			await Task.Delay(200);

			client.Connection.SendData(80, (20, "test"));
			await Task.Delay(200);

			client.Connection.SendData(81, 50d);

			server.Stop();
		}

		private void OnPacketReceived(byte sender, (int,string) packet) {
			Debugger.Break();
		}
	}
}
