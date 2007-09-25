//
// (C) 2006-2007 The SharpOS Project Team (http://www.sharpos.org)
//
// Authors:
//	Sander van Rossen <sander.vanrossen@gmail.com>
//
// Licensed under the terms of the GNU GPL License version 2.
//

using System;
using AOTAttr = SharpOS.AOT.Attributes;

namespace SharpOS.ADC
{
	public static class Memory
	{
		[AOTAttr.ADCStub]
		public static unsafe void MemSet32(uint value, uint dst, uint count)
		{
			Kernel.Error("Unimplemented - Memory.MemSet32");
		}

		[AOTAttr.ADCStub]
		public static unsafe void MemCopy(uint src, uint dst, uint count)
		{
			Kernel.Error("Unimplemented - Memory.MemCopy");
		}

		[AOTAttr.ADCStub]
		public static unsafe void MemCopy32(uint src, uint dst, uint count)
		{
			Kernel.Error("Unimplemented - Memory.MemCopy32");
		}

		[AOTAttr.ADCStub]
		public unsafe static void Call(uint address, uint value)
		{
			Kernel.Error("Unimplemented - Memory.Call");
		}
	}
}