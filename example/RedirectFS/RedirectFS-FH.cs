//
// RedirectFS.cs: Port of
// http://fuse.cvs.sourceforge.net/fuse/fuse/example/fusexmp_fh.c?view=log
//
// Authors:
//   Jonathan Pryor (jonpryor@vt.edu)
//
// (C) 2006 Jonathan Pryor
//
// Mono.Fuse example program
//

//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Mono.Fuse;
using Mono.Unix.Native;

namespace Mono.Fuse.Samples {
	class RedirectFHFS : FileSystem {

		private string basedir;

		public RedirectFHFS ()
		{
		}

		protected override Errno OnGetPathStatus (string path, out Stat buf)
		{
			int r = Syscall.lstat (basedir+path, out buf);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnGetHandleStatus (string path, OpenedPathInfo info, out Stat buf)
		{
			int r = Syscall.fstat ((int) info.Handle, out buf);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnAccessPath (string path, AccessModes mask)
		{
			int r = Syscall.access (basedir+path, mask);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnReadSymbolicLink (string path, out string target)
		{
			target = null;
			StringBuilder buf = new StringBuilder (256);
			do {
				int r = Syscall.readlink (basedir+path, buf);
				if (r < 0) {
					return Stdlib.GetLastError ();
				}
				else if (r == buf.Capacity) {
					buf.Capacity *= 2;
				}
				else {
					target = buf.ToString (0, r);
					return 0;
				}
			} while (true);
		}

		protected override Errno OnOpenDirectory (string path, OpenedPathInfo info)
		{
			IntPtr dp = Syscall.opendir (basedir+path);
			if (dp == IntPtr.Zero)
				return Stdlib.GetLastError ();

			info.Handle = dp;
			return 0;
		}

		protected override Errno OnReadDirectory (string path, OpenedPathInfo fi,
				[Out] out string[] paths)
		{
			IntPtr dp = (IntPtr) fi.Handle;

			Dirent de;
			List<string> entries = new List<string> ();
			while ((de = Syscall.readdir (dp)) != null) {
				entries.Add (de.d_name);
			}

			paths = entries.ToArray ();
			return 0;
		}

		protected override Errno OnCloseDirectory (string path, OpenedPathInfo info)
		{
			IntPtr dp = (IntPtr) info.Handle;
			Syscall.closedir (dp);
			return 0;
		}

		protected override Errno OnCreateSpecialFile (string path, FilePermissions mode, ulong rdev)
		{
			int r;

			// On Linux, this could just be `mknod(basedir+path, mode, rdev)' but 
			// this is more portable.
			if ((mode & FilePermissions.S_IFMT) == FilePermissions.S_IFREG) {
				r = Syscall.open (basedir+path, OpenFlags.O_CREAT | OpenFlags.O_EXCL |
						OpenFlags.O_WRONLY, mode);
				if (r >= 0)
					r = Syscall.close (r);
			}
			else if ((mode & FilePermissions.S_IFMT) == FilePermissions.S_IFIFO) {
				r = Syscall.mkfifo (basedir+path, mode);
			}
			else {
				r = Syscall.mknod (basedir+path, mode, rdev);
			}

			if (r == -1)
				return Stdlib.GetLastError ();

			return 0;
		}

		protected override Errno OnCreateDirectory (string path, FilePermissions mode)
		{
			int r = Syscall.mkdir (basedir+path, mode);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnRemoveFile (string path)
		{
			int r = Syscall.unlink (basedir+path);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnRemoveDirectory (string path)
		{
			int r = Syscall.rmdir (basedir+path);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnCreateSymbolicLink (string from, string to)
		{
			int r = Syscall.symlink (from, basedir+to);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnRenamePath (string from, string to)
		{
			int r = Syscall.rename (basedir+from, basedir+to);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnCreateHardLink (string from, string to)
		{
			int r = Syscall.link (basedir+from, basedir+to);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnChangePathPermissions (string path, FilePermissions mode)
		{
			int r = Syscall.chmod (basedir+path, mode);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnChangePathOwner (string path, long uid, long gid)
		{
			int r = Syscall.lchown (basedir+path, (uint) uid, (uint) gid);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnTruncateFile (string path, long size)
		{
			int r = Syscall.truncate (basedir+path, size);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnTruncateHandle (string path, OpenedPathInfo info, long size)
		{
			int r = Syscall.ftruncate ((int) info.Handle, size);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnChangePathTimes (string path, ref Utimbuf buf)
		{
			int r = Syscall.utime (basedir+path, ref buf);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnCreateHandle (string path, OpenedPathInfo info, FilePermissions mode)
		{
			int fd = Syscall.open (basedir+path, info.OpenFlags, mode);
			if (fd == -1)
				return Stdlib.GetLastError ();
			info.Handle = (IntPtr) fd;
			return 0;
		}

		protected override Errno OnOpenHandle (string path, OpenedPathInfo info)
		{
			int fd = Syscall.open (basedir+path, info.OpenFlags);
			if (fd == -1)
				return Stdlib.GetLastError ();
			info.Handle = (IntPtr) fd;
			return 0;
		}

		protected override unsafe Errno OnReadHandle (string path, OpenedPathInfo info, byte[] buf, 
				long offset, out int bytesRead)
		{
			int r;
			fixed (byte *pb = buf) {
				r = bytesRead = (int) Syscall.pread ((int) info.Handle, 
						pb, (ulong) buf.Length, offset);
			}
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override unsafe Errno OnWriteHandle (string path, OpenedPathInfo info,
				byte[] buf, long offset, out int bytesWritten)
		{
			int r;
			fixed (byte *pb = buf) {
				r = bytesWritten = (int) Syscall.pwrite ((int) info.Handle, 
						pb, (ulong) buf.Length, offset);
			}
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnGetFileSystemStatus (string path, out Statvfs stbuf)
		{
			int r = Syscall.statvfs (basedir+path, out stbuf);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnFlushHandle (string path, OpenedPathInfo info)
		{
			/* This is called from every close on an open file, so call the
			   close on the underlying filesystem.  But since flush may be
			   called multiple times for an open file, this must not really
			   close the file.  This is important if used on a network
			   filesystem like NFS which flush the data/metadata on close() */
			int r = Syscall.close (Syscall.dup ((int) info.Handle));
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnReleaseHandle (string path, OpenedPathInfo info)
		{
			int r = Syscall .close ((int) info.Handle);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnSynchronizeHandle (string path, OpenedPathInfo info, bool onlyUserData)
		{
			int r;
			if (onlyUserData)
				r = Syscall.fdatasync ((int) info.Handle);
			else
				r = Syscall.fsync ((int) info.Handle);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnSetPathExtendedAttributes (string path, string name, byte[] value, XattrFlags flags)
		{
			int r = Syscall.lsetxattr (basedir+path, name, value, (ulong) value.Length, flags);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnGetPathExtendedAttributes (string path, string name, byte[] value, out int bytesWritten)
		{
			int r = bytesWritten = (int) Syscall.lgetxattr (basedir+path, name, value, (ulong) value.Length);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnListPathExtendedAttributes (string path, byte[] value, out int bytesWritten)
		{
			int r = bytesWritten = (int) Syscall.llistxattr (basedir+path, value, (ulong) value.Length);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnRemovePathExtendedAttribute (string path, string name)
		{
			int r = Syscall.lremovexattr (basedir+path, name);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		private bool ParseArguments (string[] args)
		{
			for (int i = 0; i < args.Length; ++i) {
				switch (args [i]) {
					case "--help":
						Console.WriteLine ("{0} basedir mountpoint",
								Environment.GetCommandLineArgs () [0]);
						return false;
					default:
						if (basedir == null)
							basedir = args [i];
						else
							base.MountPoint = args [i];
						break;
				}
			}
			if (basedir == null || base.MountPoint == null) {
				Console.WriteLine ("{0} basedir mountpoint",
						Environment.GetCommandLineArgs () [0]);
				return false;
			}
			return true;
		}

		public static void Main (string[] args)
		{
			using (RedirectFHFS fs = new RedirectFHFS ()) {
				string[] unhandled = fs.ParseFuseArguments (args);
				if (!fs.ParseArguments (unhandled))
					return;
				fs.Start ();
			}
		}
	}
}

