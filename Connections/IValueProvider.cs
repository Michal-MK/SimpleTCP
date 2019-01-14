using System;
using System.Collections.Generic;

namespace Igor.TCP {
	internal interface IValueProvider {
		Dictionary<byte, Delegate> providedValues { get; }
	}
}
