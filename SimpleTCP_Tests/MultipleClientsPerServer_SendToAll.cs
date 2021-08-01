using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleTCP.Structures;
using SimpleTCP.Tests.Base;

namespace SimpleTCP.Tests {
	[TestClass]
	public class MultipleClientsPerServer_SendToAll : TestBase {
		private int receivedStringDataCount;
		private int receivedLongDataCount;
		private int receivedCustomDataCount;

		[TestInitialize]
		public void Setup() {
			evnt.Reset();
		}

		[TestMethod]
		public async Task SendingToAllPrimitive() {
			using TCPServer server = new(new ServerConfiguration());

			using TCPClient client1 = new(SimpleTCPHelper.GetActiveIPv4Address(), 55550);
			client1.SetUpClientInfo("Client 1");
			using TCPClient client2 = new(SimpleTCPHelper.GetActiveIPv4Address(), 55550);
			client2.SetUpClientInfo("Client 2");

			await server.Start(55550);

			const string DATA_STR = "Hello World";
			const long DATA_LONG = 123456789;


			await client1.ConnectAsync(1000);

			client1.OnStringReceived += (_, e) => {
				if (e.Data == DATA_STR) receivedStringDataCount++;
				if (HasAll()) evnt.Set();
			};

			client1.OnInt64Received += (_, e) => {
				if (e.Data == DATA_LONG) receivedLongDataCount++;
				if (HasAll()) evnt.Set();
			};

			await client2.ConnectAsync(1000);
			
			client2.OnStringReceived += (_, e) => {
				if (e.Data == DATA_STR) receivedStringDataCount++;
				if (HasAll()) evnt.Set();
			};

			client2.OnInt64Received += (_, e) => {
				if (e.Data == DATA_LONG) receivedLongDataCount++;
				if (HasAll()) evnt.Set();
			};

			server.SendToAll(DATA_STR);
			server.SendToAll(DATA_LONG);

			await Task.Run(Wait);

			server.Stop();

			bool HasAll() {
				return receivedStringDataCount == 2 && receivedLongDataCount == 2;
			}
		}

		[TestMethod]
		public async Task SendingToAllCustom() {
			using TCPServer server = new(new ServerConfiguration());

			using TCPClient client1 = new(SimpleTCPHelper.GetActiveIPv4Address(), 55550);
			client1.SetUpClientInfo("Client 1");
			using TCPClient client2 = new(SimpleTCPHelper.GetActiveIPv4Address(), 55550);
			client2.SetUpClientInfo("Client 2");

			await server.Start(55550);
			
			const byte PACKET_ID = 4;
			TestDataClass dataCustom = new() {
				Data = new List<int> { 0, 1, 2, 3, 4 }
			};
			
			await client1.ConnectAsync(1000);

			client1.DefineCustomPacket<TestDataClass>(PACKET_ID, Test);
			
			await client2.ConnectAsync(1000);

			client2.DefineCustomPacket<TestDataClass>(PACKET_ID, Test);

			server.SendToAll(PACKET_ID, dataCustom);

			await Task.Run(Wait);

			server.Stop();

			void Test(byte sender, TestDataClass e) {
				if (e.Data.Count == 5 && e.Data[0] == 0 && e.Data[4] == 4) receivedCustomDataCount++;
				if (receivedCustomDataCount == 2) evnt.Set();
			}
		}
	}

	[Serializable]
	class TestDataClass {
		public List<int> Data { get; set; } = new();
	}
}