using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleTCP.Structures;
using SimpleTCP.Tests.Base;

namespace SimpleTCP.Tests {
	[Serializable]
	public struct TestDataStruct {
		public string[] data;
		public TestDataTwo[] dataTest;
	}

	[Serializable]
	public struct TestDataTwo {
		public string[] moreData;
		public int ID;
	}

	[TestClass]
	public class ServerClientTests_CustomStructurePacket : TestBase {
		[TestMethod]
		public async Task CustomStructurePacket() {
			using TCPServer server = new(new ServerConfiguration());

			using TCPClient client = new(SimpleTCPHelper.GetActiveIPv4Address(), 55550);

			await server.Start(55550);
			const byte PACKET_ID = 4;

			await client.ConnectAsync(1000);

			client.ProvideValue(PACKET_ID, AlsoGet);

			server.ProvideValue(1, PACKET_ID, Get);

			TestDataStruct resp = await server.GetValue<TestDataStruct>(1, PACKET_ID);

			Assert.IsTrue(resp.data[0] == "Ahoj");
			Assert.IsTrue(resp.dataTest[0].ID == 0);
			Assert.IsTrue(resp.dataTest[0].moreData[0] == "Hello");

			TestDataStruct resp2 = await client.GetValue<TestDataStruct>(PACKET_ID);

			Assert.IsTrue(resp2.data[0] == "Ahoooooooooooj");
			Assert.IsTrue(resp2.dataTest[0].ID == 0);
			Assert.IsTrue(resp2.dataTest[0].moreData[0] == "Hell00000000000");
			
			server.Stop();
		}

		private TestDataStruct AlsoGet() {
			return new() {
				data = new[] { "Ahoj" },
				dataTest = new[] {
					new TestDataTwo {
						ID = 0, moreData = new[] { "Hello" }
					}
				}
			};
		}

		private TestDataStruct Get() {
			return new() {
				data = new[] { "Ahoooooooooooj" },
				dataTest = new[] {
					new TestDataTwo {
						ID = 0, moreData = new[] { "Hell00000000000" }
					}
				}
			};
		}
	}
}