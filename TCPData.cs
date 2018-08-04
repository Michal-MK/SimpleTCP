using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Igor.TCP {
	[Serializable, StructLayout(LayoutKind.Sequential)]
	public struct TCPData {
		public TCPData(List<Item> items) {
			this.items = items.ToArray();
		}
		public TCPData(Item[] items) {
			this.items = items;
		}

		public Item[] items { get; }
	}
	[Serializable]
	public struct Item {
		public Item(string name, int amount) {
			this.name = name;
			this.amount = amount;
		}

		public string name { get; }
		public int amount { get; }
	}
	[Serializable]
	public struct ItemMeta {
		public ItemMeta(Item item) {
			this.item = item;
		}

		public Item item;
	}
}