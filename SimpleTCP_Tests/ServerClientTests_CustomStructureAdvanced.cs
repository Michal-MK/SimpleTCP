﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleTCP.Structures;
using SimpleTCP.Tests.Base;

namespace SimpleTCP.Tests {
	public enum StateEnum {
		Running,
		Idle,
		Broken
	}

	[Serializable]
	public class Machine {
		public Guid GUID { get; set; }
		public StateEnum State { get; set; }

		public override bool Equals(object obj) {
			return obj is Machine m && m.State == State & m.GUID == GUID;
		}

		public override int GetHashCode() {
			unchecked {
				return (GUID.GetHashCode() * 397) ^ (int)State;
			}
		}
	}

	[Serializable]
	public struct TestDataStructAdvanced {
		public Dictionary<byte, Machine> Machines { get; set; }
		public Machine[,] MachineGrid { get; set; }
	}

	[TestClass]
	public class ServerClientTests_CustomStructureAdvanced : TestBase {
		[TestMethod]
		public async Task CustomStructurePacketAdvanced() {
			using TCPServer server = new (new ServerConfiguration());

			using TCPClient client = new (SimpleTCPHelper.GetActiveIPv4Address(), 55550);
			
			await server.Start(55550);
			const byte PACKET_ID = 4;

			await client.ConnectAsync(1000);

			TestDataStructAdvanced toSend = new() {
				MachineGrid = new[,] {
					{ new Machine { State = StateEnum.Running, GUID = Guid.NewGuid() }, new Machine { State = StateEnum.Idle, GUID = Guid.NewGuid() } },
					{ new Machine { State = StateEnum.Broken, GUID = Guid.NewGuid() }, new Machine { State = StateEnum.Broken, GUID = Guid.NewGuid() } }
				},
				Machines = new Dictionary<byte, Machine>() {
					{ 0, new Machine { State = StateEnum.Running, GUID = Guid.NewGuid() } },
					{ 1, new Machine { State = StateEnum.Running, GUID = Guid.NewGuid() } },
					{ 2, new Machine { State = StateEnum.Idle, GUID = Guid.NewGuid() } },
					{ 3, new Machine { State = StateEnum.Broken, GUID = Guid.NewGuid() } },
				}
			};

			bool result = false;

			server.DefineCustomPacket(1, PACKET_ID, (byte _, TestDataStructAdvanced value) => {
				try {
					Assert.IsTrue(value.Machines.Values
									   .Select(s => s.GUID)
									   .SequenceEqual(toSend.Machines.Values
															.Select(s => s.GUID)));
					Assert.IsTrue(value.MachineGrid
									   .Cast<Machine>().ToArray()
									   .SequenceEqual(toSend.MachineGrid
															.Cast<Machine>().ToArray()));
					result = true;
				}
				finally {
					evnt.Set();
				}
			});

			client.Connection.SendData(PACKET_ID, toSend);

			await Task.Run(Wait);

			client.Disconnect();
			server.Stop();

			Assert.IsTrue(result);
		}
	}
}