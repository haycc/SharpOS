//
// (C) 2006-2007 The SharpOS Project Team (http://www.sharpos.org)
//
// Authors:
//	Sander van Rossen <sander.vanrossen@gmail.com>
//	William Lahti <xfurious@gmail.com>
//
// Licensed under the terms of the GNU GPL v3,
//  with Classpath Linking Exception for Libraries
//

//#define VERBOSE_KeyMap_INIT

using System;
using SharpOS.Kernel;
using SharpOS.Kernel.ADC;
using SharpOS.Kernel.Foundation;

namespace SharpOS.Kernel {
	public unsafe class KeyMap {
		#region Global fields

		static PString8* userKeyMap = PString8.Wrap (Stubs.StaticAlloc (EntryModule.MaxKeyMapNameLength),
			EntryModule.MaxKeyMapNameLength);
		static byte* getBuiltinKeyMapBuffer = Stubs.StaticAlloc (EntryModule.MaxKeyMapNameLength);
		static byte* stringConvBuffer = Stubs.StaticAlloc (EntryModule.MaxKeyMapNameLength);
		static void* keymapArchive;
		static PString8* keymapName = PString8.Wrap (Stubs.StaticAlloc (EntryModule.MaxKeyMapNameLength),
			EntryModule.MaxKeyMapNameLength);
		static int keymapEntries;
		static void* keymapAddr;

		#endregion
		#region Setup

		/// <summary>
		/// Locates the archive of built-in keymaps, parses the
		/// user-specified keymap from the kernel command line,
		/// and installs a default keymap.
		/// </summary>
		public static void Setup ()
		{
			// look for the -keymap option, find a
			// matching keymap from the archive, and
			// use the Keyboard class to set it as
			// the installed keymap.


			if (!CommandLine.GetArgument ("-keymap", userKeyMap)) {
				// pick a default
				TextMode.WriteLine ("No keymap selected, choosing default (US)");

				userKeyMap->Clear ();
				userKeyMap->Concat ("US");
			}

			keymapArchive = (void*) Stubs.GetLabelAddress
				("SharpOS.Kernel/Resources/BuiltinKeyMaps.ska");

			Diagnostics.Assert (keymapArchive != null, "KeyMap.Setup(): keymap archive is null");

			keymapName->Clear ();
			keymapName->Concat (userKeyMap);
			keymapEntries = *(int*) keymapArchive;
			keymapAddr = GetBuiltinKeyMap (userKeyMap);

#if VERBOSE_KeyMap_INIT
			// print some info
			TextMode.WriteLine ("KeyMap archive: installed at 0x", (int)keymapArchive, true);
			TextMode.WriteLine ("                ", keymapEntries, " entries");
			TextMode.WriteLine ("");
#endif

			if (keymapAddr == null) {
				Diagnostics.Warning ("Failed to install an initial keymap");
				return;
			}

			SetDirectKeyMap (keymapAddr);
		}

		#endregion
		#region Internal

		static void* GetBuiltinKeyMap (byte* name, int nameLen)
		{
#if VERBOSE_KeyMap_INIT
			TextMode.Write ("Key Map Name: ");
			TextMode.Write (name);
			TextMode.WriteLine ();

			TextMode.Write ("Key Map Name Length: ");
			TextMode.Write (nameLen);
			TextMode.WriteLine ();
#endif

			byte* table = (byte*) keymapArchive + 4;
			byte* ret_table;
			byte* buf = getBuiltinKeyMapBuffer;

			Diagnostics.Assert (nameLen > 0,
				"KeyMap.GetBuiltinKeyMap(): key map name is too small");
			Diagnostics.Assert (nameLen <= EntryModule.MaxKeyMapNameLength,
				"KeyMap.GetBuiltinKeyMap(): key map name is too large");

			for (int x = 0; x < keymapEntries; ++x) {
				int nSize = 0;
				int tSize = 0;
				int error = 0;
				int strSize = 0;

				strSize = BinaryTool.ReadPrefixedString (table, buf,
					EntryModule.MaxKeyMapNameLength, &error);

				table += strSize;
				nSize = ByteString.Length (buf);

#if VERBOSE_KeyMap_INIT
				TextMode.Write ("nsize: ");
				TextMode.Write (nSize);
				TextMode.WriteLine ();

				TextMode.Write ("found keymap: ");
				TextMode.WriteLine (buf);
#endif

				ret_table = table;

				table += 2; // keymask/statebit

				// default table

				tSize = *(int*) table;
#if VERBOSE_KeyMap_INIT
				TextMode.Write("Default-table size:");
				TextMode.Write(tSize);
				TextMode.WriteLine("");
#endif
				table += 4;
				table += tSize;

				// shifted table

				tSize = *(int*) table;
#if VERBOSE_KeyMap_INIT
				TextMode.Write("Shifted-table size:");
				TextMode.Write(tSize);
				TextMode.WriteLine("");
#endif
				table += 4;
				table += tSize;

				if (nSize == nameLen && ByteString.Compare (name, buf, nameLen) == 0)
					return ret_table;
			}

			return null;
		}

		public static void WriteKeymaps ()
		{
			byte* table = (byte*) keymapArchive + 4;
			byte* ret_table;
			byte* buf = getBuiltinKeyMapBuffer;

			for (int x = 0; x < keymapEntries; ++x) {
				int nSize = 0;
				int tSize = 0;
				int error = 0;
				int strSize = 0;

				strSize = BinaryTool.ReadPrefixedString (table, buf,
				    EntryModule.MaxKeyMapNameLength, &error);

				table += strSize;
				nSize = ByteString.Length (buf);

				ret_table = table;

				table += 2; // keymask/statebit

				// default table

				tSize = *(int*) table;
				table += 4;
				table += tSize;

				// shifted table

				tSize = *(int*) table;
				table += 4;
				table += tSize;

				// Write it out.
				TextMode.WriteLine (buf);
			}
		}

		#endregion
		#region GetKeyMap() family

		/// <summary>
		/// Gets the address of a builtin keymap included in the kernel
		/// via the keymap archive resource in SharpOS.Kernel.dll. The
		/// archive is generated by the SharpOS keymap compiler.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="nameLen">The name len.</param>
		/// <returns></returns>
		public static void* GetBuiltinKeyMap (byte* name)
		{
			return GetBuiltinKeyMap (name, ByteString.Length (name));
		}

		/// <summary>
		/// Gets the address of a builtin keymap included in the kernel
		/// via the keymap archive resource in SharpOS.Kernel.dll. The
		/// archive is generated by the SharpOS keymap compiler.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="nameLen">The name len.</param>
		/// <returns></returns>
		public static void* GetBuiltinKeyMap (CString8* name)
		{
			return GetBuiltinKeyMap (name->Pointer, name->Length);
		}

		/// <summary>
		/// Gets the address of a builtin keymap included in the kernel
		/// via the keymap archive resource in SharpOS.Kernel.dll. The
		/// archive is generated by the SharpOS keymap compiler.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="nameLen">The name len.</param>
		/// <returns></returns>
		public static void* GetBuiltinKeyMap (PString8* name)
		{
			return GetBuiltinKeyMap (name->Pointer, name->Length);
		}

		/// <summary>
		/// Gets the address of a builtin keymap included in the kernel
		/// via the keymap archive resource in SharpOS.Kernel.dll. The
		/// archive is generated by the SharpOS keymap compiler.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="nameLen">The name len.</param>
		/// <returns></returns>
		public static void* GetBuiltinKeyMap (string name)
		{
			ByteString.GetBytes (name, stringConvBuffer, EntryModule.MaxKeyMapNameLength);

			return GetBuiltinKeyMap (stringConvBuffer, name.Length);
		}

		/// <summary>
		/// Gets the count of all builtin key maps.
		/// </summary>
		public static int GetBuiltinKeyMapsCount ()
		{
			return keymapEntries;
		}

		/// <summary>
		/// Gets the address of a builtin key map, by it's numeric ID. Good
		/// for iterating through the list of builtin key maps.
		/// </summary>
		public static void* GetBuiltinKeyMap (int id)
		{
			byte* table = (byte*) keymapArchive + 4;
			byte* buf = stackalloc byte [EntryModule.MaxKeyMapNameLength];
			int error = 0;

			for (int x = 0; x < keymapEntries; ++x) {

				if (x == id)
					return (void*) table;

				// name-size (x), name string (x), keymask and statebit (2)

				table += 2 + BinaryTool.ReadPrefixedString (table, buf,
					EntryModule.MaxKeyMapNameLength, &error);

				// table size (4), default table (x)

				table += 4 + *(int*) table;

				// table size (4), shifted table (x)

				table += 4 + *(int*) table;
			}

			return null;
		}

		public static PString8* GetCurrentKeyMapName ()
		{
			return keymapName;
		}

		/// <summary>
		/// Gets the keymap currently in use.
		/// </summary>
		public static void* GetCurrentKeyMap ()
		{
			return keymapAddr;
		}

		#endregion
		#region SetKeyMap() family

		public static void SetKeyMapName (byte* str, int len)
		{
			keymapName->Clear ();
			keymapName->Concat (str, len);
		}

		/// <summary>
		/// Installs the default and shifted key tables of the given
		/// keymap, so that all further keyboard scancodes are
		/// converted using the new mapping.
		/// </summary>
		public static void SetDirectKeyMap (void* keymap)
		{
			byte* keymapAddress = (byte*) keymap;
			byte* defmap = null, shiftmap = null;
			int defmapLen = 0, shiftmapLen = 0;

			//TODO: what to do with these bits?
			byte keymask = *(keymapAddress + 0);
			byte statebit = *(keymapAddress + 1);

			keymapAddress += 2;


			defmap = GetDefaultTable (keymapAddress, &defmapLen);
			shiftmap = GetShiftedTable (keymapAddress, &shiftmapLen);

			Keyboard.SetKeyMap (defmap, defmapLen, shiftmap, shiftmapLen);
		}

		public static void SetKeyMap (byte* name)
		{
			keymapName->Clear ();
			keymapName->Concat (name);

			SetDirectKeyMap (GetBuiltinKeyMap (name, ByteString.Length (name)));
		}

		/// <summary>
		/// Sets the current keymap to a built-in one specified by
		/// <paramref name="name" />.
		/// </summary>
		public static void SetKeyMap (byte* name, int len)
		{
			keymapName->Clear ();
			keymapName->Concat (name, len);

			SetDirectKeyMap (GetBuiltinKeyMap (name, len));
		}

		/// <summary>
		/// Sets the current keymap to a built-in one specified by
		/// <paramref name="name" />.
		/// </summary>
		public static void SetKeyMap (CString8* name)
		{
			SetKeyMap (name->Pointer);
		}

		/// <summary>
		/// Sets the current keymap to a built-in one specified by
		/// <paramref name="name" />.
		/// </summary>
		public static void SetKeyMap (PString8* name)
		{
			SetKeyMap (name->Pointer, name->Length);
		}

		#endregion
		#region [Get/Set][Default/Shifted]Table() family

		/// <summary>
		/// Gets the `default' table of the given keymap.
		/// </summary>
		public static byte* GetDefaultTable (void* keymap, int* ret_len)
		{
			*ret_len = *(int*) keymap;

			return (byte*) keymap + 4;
		}

		/// <summary>
		/// Gets the `shifted' table of the given keymap.
		/// </summary>
		public static byte* GetShiftedTable (void* keymap, int* ret_len)
		{
			int dLen = 0;
			byte* ptr = GetDefaultTable (keymap, &dLen);

			ptr += dLen;
			*ret_len = *(int*) ptr;

			return ptr + 4;
		}

		/// <summary>
		/// Gets the `default' table of the installed keymap.
		/// </summary>
		public static byte* GetDefaultTable (int* ret_len)
		{
			Diagnostics.Assert (keymapAddr != null, "No keymap is installed!");

			return GetDefaultTable (keymapAddr, ret_len);
		}

		/// <summary>
		/// Gets the `shifted' table of the installed keymap.
		/// </summary>
		public static byte* GetShiftedTable (int* ret_len)
		{
			Diagnostics.Assert (keymapAddr != null, "No keymap is installed!");

			return GetShiftedTable (keymapAddr, ret_len);
		}

		#endregion
		#region GetKeyMask/StateBit() family

		/// <summary>
		/// Gets the keymask specified in the given keymap.
		/// </summary>
		public static byte GetKeyMask (void* keymap)
		{
			int nlen = *(int*) keymap;

			return *((byte*) keymap + 4 + nlen);
		}

		/// <summary>
		/// Gets the state bit specified in the given keymap.
		/// </summary>
		public static byte GetStateBit (void* keymap)
		{
			int nlen = *(int*) keymap;

			return *((byte*) keymap + 5 + nlen);
		}

		/// <summary>
		/// Gets the keymask of the installed keymap.
		/// </summary>
		public static byte GetKeyMask ()
		{
			Diagnostics.Assert (keymapAddr != null, "No keymap is installed!");

			return GetKeyMask (keymapAddr);
		}

		/// <summary>
		/// Gets the state bit of the installed keymap.
		/// </summary>
		public static byte GetStateBit ()
		{
			Diagnostics.Assert (keymapAddr != null, "No keymap is installed!");

			return GetStateBit (keymapAddr);
		}

		#endregion

	}
}

