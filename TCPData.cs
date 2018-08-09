﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Igor.TCP {
	/// <summary>
	/// Data used for Purchse transmission
	/// </summary>
	[Serializable, StructLayout(LayoutKind.Sequential)]
	public struct TCPData {
		/// <summary>
		/// Get new TCPData
		/// </summary>
		public TCPData(List<Item> items, PurchaseMeta meta) {
			this.items = items.ToArray();
			this.meta = meta;
		}

		/// <summary>
		/// Get new TCPData
		/// </summary>
		public TCPData(Item[] items, PurchaseMeta meta) {
			this.items = items;
			this.meta = meta;
		}

		/// <summary>
		/// Items purchased
		/// </summary>
		public Item[] items { get; }

		/// <summary>
		/// Purchase information
		/// </summary>
		public PurchaseMeta meta { get; }
	}

	/// <summary>
	/// Simgle Item from a shop
	/// </summary>
	[Serializable]
	public struct Item {
		/// <summary>
		/// Define Item
		/// </summary>
		public Item(string name, int amount) {
			this.name = name;
			this.amount = amount;
		}

		/// <summary>
		/// Items name
		/// </summary>
		public string name { get; }

		/// <summary>
		/// How many were bought
		/// </summary>
		public int amount { get; }
	}

	/// <summary>
	/// NIY
	/// </summary>
	[Serializable]
	public struct ItemMeta {
		/// <summary>
		/// Create general Item information
		/// </summary>
		/// <param name="item"></param>
		public ItemMeta(Item item) {
			this.item = item;
		}

		/// <summary>
		/// Item this information is for
		/// </summary>
		public Item item;
	}

	/// <summary>
	/// Purchase information
	/// </summary>
	[Serializable]
	public struct PurchaseMeta {
		/// <summary>
		/// Create general purchase information
		/// </summary>
		public PurchaseMeta(DateTime date, string shopName, int amountItemsPruchased) {
			purchasedAt = date;
			this.shopName = shopName;
			itemsPurchased = amountItemsPruchased;
		}
		/// <summary>
		/// Purchase preformed at
		/// </summary>
		public DateTime purchasedAt { get; }

		/// <summary>
		/// Total items bought
		/// </summary>
		public int itemsPurchased { get; }

		/// <summary>
		/// Shop where items were bought
		/// </summary>
		public string shopName { get; }
	}
}