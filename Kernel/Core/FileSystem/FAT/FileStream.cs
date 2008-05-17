﻿// (C) 2006-2007 The SharpOS Project Team (http://www.sharpos.org)
//
// Authors:
//	Phil Garcia <phil@thinkedge.com>
//
// Licensed under the terms of the GNU GPL v3,
//  with Classpath Linking Exception for Libraries
//

using System;
//using System.IO;
using InternalSystem.IO;
using SharpOS.Kernel.DriverSystem;

namespace SharpOS.Kernel.FileSystem.Fat
{
	public class FileStream : Stream
	{
		protected uint startCluster;
		protected uint currentCluster;
		protected uint nthCluster;
		protected long position;
		protected long length;
		protected long lengthOnDisk;

		protected bool read;
		protected bool write;

		protected MemoryBlock data;
		protected bool dirty;

		protected uint clusterSize;

		FileSystem fs;

		public FileStream (FileSystem fs, uint startCluster, uint clusterSize)	// TODO pass in directory info. 
		{
			this.clusterSize = clusterSize;
			this.data = new MemoryBlock (clusterSize);
			this.fs = fs;
			this.startCluster = startCluster;
			this.read = true;
			this.write = false;
			this.position = -1;
			this.dirty = false;
			this.length = -1;	// TODO
			this.lengthOnDisk = -1; // TODO
			this.nthCluster = UInt32.MaxValue; // Not positioned yet 

			if (length != 0)
				ReadCluster (startCluster);
		}

		public override bool CanRead
		{
			get
			{
				return read;
			}
		}

		public override bool CanSeek
		{
			get
			{
				return true;
			}
		}

		public override bool CanWrite
		{
			get
			{
				return write;
			}
		}

		public override bool CanTimeout
		{
			get
			{
				return false;
			}
		}

		public override long Length
		{
			get
			{
				return length;
			}
		}

		public override long Position
		{
			get
			{
				return position;
			}
			set
			{
				Seek ((long)value, SeekOrigin.Begin);
			}
		}

		public override void Flush ()
		{
			if (!dirty)
				return;

			fs.WriteCluster (data, currentCluster);

			SetLength (length);

			dirty = false;
		}

		public override int Read (byte[] buffer, int offset, int count)
		{
			if (position >= length)
				return -1;	// EOF

			int index = 0;

			// very slow
			for (; (position < length) && (index < count); index++)
				buffer[offset + index] = (byte)ReadByte ();

			return index;
		}

		public override int ReadByte ()
		{
			if (position >= length)
				return -1;	// EOF

			uint index = (uint)((uint)position % clusterSize); // BUG WORKAROUND: inner (uint) is because long drive is not supported yet

			position++;

			byte b = data.GetByte (index);

			if (index == clusterSize) {
				if (position < length)
					NextCluster ();
			}

			return b;
		}

		public override long Seek (long offset, SeekOrigin origin)
		{
			long newposition = position;

			switch (origin) {
				case SeekOrigin.Begin: newposition = offset; break;
				case SeekOrigin.Current: newposition = position + offset; break;
				case SeekOrigin.End: newposition = length + offset; break;
			}

			// find cluster number of new position
			uint newNthCluster = (uint)((uint)newposition / clusterSize);		// BUG WORKAROUND: inner (uint) is because long drive is not supported yet
			uint currentNthCluster = (uint)((uint)position / clusterSize);		// BUG WORKAROUND: inner (uint) is because long drive is not supported yet
			int diff = (int)(newNthCluster - currentNthCluster);

			uint newcluster = 0;

			if (newNthCluster == currentNthCluster) {
				newcluster = currentCluster;
			}
			else
				if (newNthCluster > currentNthCluster) {
					newcluster = fs.FindNthCluster (currentCluster, (uint)diff);
					currentNthCluster = currentNthCluster + (uint)diff;
				}
				else
					if (newNthCluster < currentNthCluster) {
						newcluster = fs.FindNthCluster (this.startCluster, newNthCluster);
						currentNthCluster = newNthCluster;
					}

			ReadCluster (newcluster);
			position = newposition;
			return position;
		}

		protected void NextCluster ()
		{
			uint newcluster = fs.NextCluster (currentCluster);
			ReadCluster (newcluster);
		}

		protected void ReadCluster (uint cluster)
		{
			if (currentCluster == cluster)
				return;

			Flush ();

			currentCluster = cluster;
			fs.ReadCluster (data, cluster);
			dirty = false;
		}

		public override void SetLength (long value)
		{
			// TODO: incomplete

			if (value == lengthOnDisk)
				return;

			// incomplete here

			lengthOnDisk = value;

			return;
		}

		public override void Write (byte[] buffer, int offset, int count)
		{
			// TODO
		}

		public override void WriteByte (byte value)
		{
			// TODO
		}
	}
}