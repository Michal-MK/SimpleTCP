using System;
using System.Collections.Generic;

namespace SimpleTCP.Connections.Interaces {
	internal interface IValueProvider {
		Dictionary<byte, Delegate> ProvidedValues { get; }
	}
}
