using Igor.SameTest;
using Igor.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;


namespace Igor.Test {
	[Serializable]
	public struct TestDataStruct {
		public string[] data;
		public TestData.TestDataTwo[] dataTest;
	}
}

namespace Igor.SameTest {
	[Serializable]
	public struct TestDataStruct {
		public string[] data;
		public TestData.TestDataTwo[] dataTest;
	}
}

namespace Igor.TestData {
	[Serializable]
	public struct TestDataTwo {
		public string[] moreData;
		public int ID;
	}
}

namespace Igor.TCP {
	public partial class ServerClientTests {
		[TestMethod]
		public async Task CustomStructurePacket() {
			TCPServer server = new TCPServer(new ServerConfiguration());

			TCPClient client = new TCPClient(SimpleTCPHelper.GetActiveIPv4Address(), 55550);

			await server.Start(55550);

			client.Connect();

			await Task.Delay(100);

			const byte PACKET_ID = 4;
			//TODO fix tests
			server.DefineTwoWayComunication<Test.TestDataStruct>(1, PACKET_ID, Get);
			client.DefineTwoWayComunication<SameTest.TestDataStruct>(PACKET_ID, AlsoGet);

			TCPResponse resp = await server.RaiseRequestAsync(1, PACKET_ID);

			Assert.IsTrue(resp.dataType == typeof(Test.TestDataStruct));
			Assert.IsTrue(resp.packetID == PACKET_ID);

			TCPResponse resp2 = await client.RaiseRequestAsync(PACKET_ID);

			Assert.IsTrue(resp2.dataType == typeof(SameTest.TestDataStruct));
			Assert.IsTrue(resp2.packetID == PACKET_ID);

			await server.Stop();
		}

		private SameTest.TestDataStruct AlsoGet() {
			return new SameTest.TestDataStruct() {
				data = new[] { "Ahoj" },
				dataTest = new TestData.TestDataTwo[] {
					new TestData.TestDataTwo() {
						ID = 0, moreData = new[] { "Hello" } } }
			};
		}

		private Test.TestDataStruct Get() {
			return new Test.TestDataStruct() {
				data = new[] { "Ahoooooooooooj" },
				dataTest = new TestData.TestDataTwo[] {
					new TestData.TestDataTwo() {
						ID = 0, moreData = new[] { "Hell00000000000" } } }
			};
		}
	}
}
