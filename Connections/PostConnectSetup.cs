using System;
using System.Collections.Generic;
using System.Reflection;

namespace SimpleTCP.Connections {
	internal class PostConnectSetup {
		private readonly TCPClient client;

		private readonly List<PropSyncData> propertySyncData = new();
		private readonly List<ValueProvideData> valueProvideData = new();
		private readonly List<PacketDefData> customPacketDefData = new();

		internal PostConnectSetup(TCPClient tcpClient) {
			client = tcpClient;
		}

		internal void Setup() {
			foreach (PropSyncData data in propertySyncData) {
				client.SyncProperty(data.instance, data.propName, data.propPacketID);
			}

			MethodInfo provideVal = client.GetType().GetMethod(nameof(client.ProvideValue))!;

			foreach (ValueProvideData data in valueProvideData) {
				provideVal.Invoke(client, new[] { data.packetID, data.function });
			}

			MethodInfo defineCustomPacket = client.GetType().GetMethod(nameof(client.DefineCustomPacket))!;

			foreach (PacketDefData data in customPacketDefData) {
				defineCustomPacket.Invoke(client, new[] { data.packetID, data.callback });
			}
		}

		internal void Clear() {
			propertySyncData.Clear();
			valueProvideData.Clear();
			customPacketDefData.Clear();
		}

		internal void AddPropSync(object instance, string propertyName, byte propertyPacketID) {
			propertySyncData.Add(new PropSyncData(instance, propertyName, propertyPacketID));
		}

		internal void AddProvideValue<T>(byte packetID, Func<T> function) {
			valueProvideData.Add(new ValueProvideData(packetID, function));
		}

		internal void AddCustomPacketDef<TData>(byte packetID, Action<byte, TData> callback) {
			customPacketDefData.Add(new PacketDefData(packetID, callback));
		}

		private readonly struct PropSyncData {
			public PropSyncData(object instance, string propName, byte propPacketID) {
				this.instance = instance;
				this.propName = propName;
				this.propPacketID = propPacketID;
			}

			public readonly object instance;
			public readonly string propName;
			public readonly byte propPacketID;
		}

		private readonly struct ValueProvideData {
			public ValueProvideData(byte packetID, object function) {
				this.packetID = packetID;
				this.function = function;
			}

			public readonly byte packetID;
			public readonly object function;
		}

		private readonly struct PacketDefData {
			public PacketDefData(byte packetID, object callback) {
				this.packetID = packetID;
				this.callback = callback;
			}

			public readonly byte packetID;
			public readonly object callback;
		}
	}
}