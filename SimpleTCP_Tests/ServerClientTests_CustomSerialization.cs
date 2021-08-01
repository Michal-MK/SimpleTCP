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
		private Func<int, int, MyComplexClass> DATA => (i1, i2) => new MyComplexClass {
			ID = i1,
			Text = "Hello World! " + i1 + i2,
			Data = new List<int> { 0, 1, 2, 3, 4, i2 }
		};

		private const float TOLERANCE = 0.0001f;

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

			client.DefineCustomPacket<MyComplexClass>(PACKET_ID, (_, ea) => {
				if (ea.ID == 1 && ea.Data[ea.Data.Count - 1] == 100 && ea.Text.EndsWith("101")) {
					idOnePassed = true;
				}
				if (ea.ID == 2 && ea.Data[ea.Data.Count - 1] == 200 && ea.Text.EndsWith("202")) {
					idTwoPassed = true;
				}
				if (AllPassed()) {
					evnt.Set();
				}
			});

			server.GetConnection(1).SendData(PACKET_ID, DATA(1, 100));
			server.GetConnection(1).SendData(PACKET_ID, DATA(2, 200));

			await Task.Run(Wait);

			server.Stop();
		}

		private MyComplexClass Deserialization(byte[] data) {
			if (data[0] == 1) {
				serialization1Passed = true;
				return DATA(1, 100);
			}
			serialization2Passed = true;
			return DATA(2, 200);
		}

		private byte[] Serialization(MyComplexClass data) {
			if (data.ID == 1 && data.Data[data.Data.Count - 1] == 100 && data.Text.EndsWith("101")) {
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

			client.DefineCustomPacket<float>(PACKET_ID, (_, ea) => {
				if (Math.Abs(ea - 1) < TOLERANCE) {
					idOnePassedStruct = true;
				}
				if (Math.Abs(ea - 2) < TOLERANCE) {
					idTwoPassedStruct = true;
				}
				if (AllPassedStruct()) {
					evnt.Set();
				}
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
			if (Math.Abs(data - 1f) < TOLERANCE) {
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

		public List<int> Data { get; set; } = new();

		public string Text { get; set; } = "";
	}
}