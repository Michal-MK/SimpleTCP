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
			server.ProvideValue(1, PACKET_ID, Get);
			client.ProvideValue(PACKET_ID, AlsoGet);

			SameTest.TestDataStruct resp = await server.GetValue<SameTest.TestDataStruct>(1, PACKET_ID);

			Assert.IsTrue(resp.data[0] == "Ahoj");
			Assert.IsTrue(resp.dataTest[0].ID == 0);
			Assert.IsTrue(resp.dataTest[0].moreData[0] == "Hello");

			Test.TestDataStruct resp2 = await client.GetValue<Test.TestDataStruct>(PACKET_ID);

			Assert.IsTrue(resp2.GetType() == typeof(Test.TestDataStruct));
			Assert.IsTrue(resp2.data[0] == "Ahoooooooooooj");
			Assert.IsTrue(resp2.dataTest[0].ID == 0);
			Assert.IsTrue(resp2.dataTest[0].moreData[0] == "Hell00000000000");

			
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
