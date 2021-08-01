using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleTCP.DataTransfer.Serialization;
using SimpleTCP.Structures;
using SimpleTCP.Tests.Base;

namespace SimpleTCP.Tests {
	[TestClass]
	public class ServerClientTests_CustomSerialization : TestBase {
		private Func<int, MyComplexClass> DATA => i => new MyComplexClass {
			ID = i,
			Text = "Hello World!",
			State = true,
			Data = new List<int> { 0, 1, 2, 3, 4 }
		};

		private bool serialization1Passed;
		private bool serialization2Passed;
		private bool deserialization1Passed;
		private bool deserialization2Passed;
		private bool idOnePassed;
		private bool idTwoPassed;

		[TestMethod]
		public async Task SerializeCustomClass() {

			SimpleSerializer<MyComplexClass> format = new(Deserialization, Serialization);

			using TCPServer server = new(ServerConfiguration.Builder
															.Create()
															.AddSerializer(format)
															.Build());

			using TCPClient client = new(SimpleTCPHelper.GetActiveIPv4Address(), 55550, ClientConfiguration.Builder
																										   .Create()
																										   .AddSerializer(format)
																										   .Build());

			const byte PACKET_ID = 4;

			await server.Start(55550);
			await client.ConnectAsync(1000);

			client.DefineCustomPacket<MyComplexClass>(PACKET_ID, (se, ea) => {
				if (ea.ID == 1) {
					idOnePassed = true;
				}
				if (ea.ID == 2) {
					idTwoPassed = true;
				}
				if (AllPassed()) {
					evnt.Set();
				};
			});

			server.GetConnection(1).SendData(PACKET_ID, DATA(1));
			server.GetConnection(1).SendData(PACKET_ID, DATA(2));

			await Task.Run(Wait);

			server.Stop();
		}

		private MyComplexClass Deserialization(byte[] data) {
			if (data[0] == 1) {
				serialization1Passed = true;
				return DATA(1);
			}
			serialization2Passed = true;
			return DATA(2);
		}

		private byte[] Serialization(MyComplexClass data) {
			if (data.ID == 1) {
				deserialization1Passed = true;
				return new byte[] { 1 };
			}
			deserialization2Passed = true;
			return new byte[] { 2 };
		}

		private bool AllPassed() => serialization1Passed && deserialization1Passed &&
									serialization2Passed && deserialization2Passed &&
									idOnePassed && idTwoPassed;
		
		private bool serialization1PassedStruct;
		private bool serialization2PassedStruct;
		private bool deserialization1PassedStruct;
		private bool deserialization2PassedStruct;
		private bool idOnePassedStruct;
		private bool idTwoPassedStruct;
		
		[TestMethod]
		public async Task SerializeCustomStruct() {

			SimpleSerializer<float> format = new(DeserializationStruct, SerializationStruct);

			using TCPServer server = new(ServerConfiguration.Builder
															.Create()
															.AddSerializer(format)
															.Build());

			using TCPClient client = new(SimpleTCPHelper.GetActiveIPv4Address(), 55550, ClientConfiguration.Builder
																										   .Create()
																										   .AddSerializer(format)
																										   .Build());

			const byte PACKET_ID = 4;

			await server.Start(55550);
			await client.ConnectAsync(1000);

			client.DefineCustomPacket<float>(PACKET_ID, (se, ea) => {
				if (ea == 1) {
					idOnePassedStruct = true;
				}
				if (ea == 2) {
					idTwoPassedStruct = true;
				}
				if (AllPassedStruct()) {
					evnt.Set();
				};
			});

			server.GetConnection(1).SendData(PACKET_ID, 5f);
			server.GetConnection(1).SendData(PACKET_ID, 5f);

			await Task.Run(Wait);

			server.Stop();
		}
		
		private float DeserializationStruct(byte[] data) {
			if (data[0] == 1) {
				serialization1PassedStruct = true;
				return 1f;
			}
			serialization2PassedStruct = true;
			return 2f;
		}

		private byte[] SerializationStruct(float data) {
			if (data == 1f) {
				deserialization1PassedStruct = true;
				return new byte[] { 1 };
			}
			deserialization2PassedStruct = true;
			return new byte[] { 2 };
		}
		
		private bool AllPassedStruct() => serialization1PassedStruct && deserialization1PassedStruct &&
										  serialization2PassedStruct && deserialization2PassedStruct &&
										  idOnePassedStruct && idTwoPassedStruct;

	}

	class MyComplexClass {
		public int ID { get; set; }

		public List<int> Data { get; set; }

		public bool State { get; set; }

		public string Text { get; set; }
	}
}