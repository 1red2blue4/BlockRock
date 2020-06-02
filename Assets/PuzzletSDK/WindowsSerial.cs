using UnityEngine;
using System.Collections;
using System;
using System.Runtime.InteropServices;

namespace Puzzlets {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
	public class WindowsSerial : ISerialPort {

		//Select error constants (from WINERROR.H)
		internal const UInt32	ERROR_FILE_NOT_FOUND = 2;
		internal const UInt32	ERROR_ACCESS_DENIED = 5;
		
		//Select File constants (from WINBASE.H)
		internal const Int32	INVALID_HANDLE_VALUE = -1;
		internal const UInt32	OPEN_EXISTING = 3;
		
		//Access constants (from WINNT.H)
		internal const UInt32 GENERIC_READ = 0x80000000;
		internal const UInt32 GENERIC_WRITE	= 0x40000000;
		internal const UInt32 READ_WRITE = GENERIC_READ | GENERIC_WRITE;

		/// http://msdn.microsoft.com/en-us/library/windows/desktop/aa363190(v=vs.85).aspx
		/// <summary>
		/// Contains the time-out parameters for a communications device. The parameters determine 
		/// the behavior of ReadFile, WriteFile, ReadFileEx, and WriteFileEx operations on the device.
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct COMMTIMEOUTS {
			public UInt32 ReadIntervalTimeout;
			public UInt32 ReadTotalTimeoutMultiplier;
			public UInt32 ReadTotalTimeoutConstant;
			public UInt32 WriteTotalTimeoutMultiplier;
			public UInt32 WriteTotalTimeoutConstant;

			public COMMTIMEOUTS (UInt32 readInterval, UInt32 readMult, UInt32 readConstant, 
			                     UInt32 writeMult, UInt32 writeConstant){
				ReadIntervalTimeout = readInterval;
				ReadTotalTimeoutMultiplier = readMult;
				ReadTotalTimeoutConstant = readConstant;
				WriteTotalTimeoutMultiplier = readMult;
				WriteTotalTimeoutConstant = readConstant;
			}
		}

		 
		/// http://msdn.microsoft.com/en-us/library/windows/desktop/aa363858(v=vs.85).aspx
		/// <summary>
		/// Creates or opens a file or I/O device. 
		/// The most commonly used I/O devices are as follows: 
		/// file, file stream, directory, physical disk, volume, 
		/// console buffer, tape drive, communications resource, 
		/// mailslot, and pipe. The function returns a handle that 
		/// can be used to access the file or device for various types 
		/// of I/O depending on the file or device and 
		/// the flags and attributes specified.
		/// </summary>
		[DllImport("kernel32.dll", SetLastError=true)]
		private static extern IntPtr CreateFile(String lpFileName, 
		                                        UInt32 dwDesiredAccess, 
		                                        UInt32 dwShareMode,
		                                        IntPtr lpSecurityAttributes,
		                                        UInt32 dwCreationDisposition,
		                                        UInt32 dwFlagsAndAttributes,
		                                        IntPtr hTemplateFile
		                                        );
		
		/// http://msdn.microsoft.com/en-us/library/windows/desktop/ms724211(v=vs.85).aspx
		/// <summary>
		/// Closes an object handle (such as one returned by CreateFile).
		/// </summary>
		[DllImport("kernel32.dll")]
		private static extern Boolean CloseHandle(IntPtr hObject);
		
		/// http://msdn.microsoft.com/en-us/library/windows/desktop/aa365467(v=vs.85).aspx
		/// <summary>
		/// Reads data from the specified file or input/output (I/O) device. 
		/// Reads occur at the position specified by the file pointer 
		/// if supported by the device.
		/// 
		/// This function is designed for both synchronous and asynchronous operations. 
		/// For a similar function designed solely for asynchronous operation, see ReadFileEx.
		/// 
		/// asynchronous operations use lpOverlapped and set nNumberOfBytesRead to NULL
		/// synchronous operations use nNumberOfBytesRead and set lpOverlapped to NULL
		/// </summary>
		[DllImport("kernel32.dll", SetLastError=true)]
		private static extern Boolean ReadFile(IntPtr hFile,
		                                       [Out] Byte[] lpBuffer,
		                                       UInt32 nNumberOfBytesToRead,
		                                       out UInt32 nNumberOfBytesRead,
		                                       IntPtr lpOverlapped);
		
		/// http://msdn.microsoft.com/en-us/library/windows/desktop/aa365747(v=vs.85).aspx
		/// <summary>
		/// Writes data to the specified file or input/output (I/O) device.	
		/// This function is designed for both synchronous and asynchronous operation. 
		/// For a similar function designed solely for asynchronous operation, see WriteFileEx.
		/// 
		/// asynchronous operations use lpOverlapped and set nNumberOfBytesRead to NULL
		/// synchronous operations use nNumberOfBytesRead and set lpOverlapped to NULL
		/// </summary>
		[DllImport("kernel32.dll", SetLastError=true)]
		private static extern Boolean WriteFile(IntPtr fFile,
		                                        Byte[] lpBuffer,
		                                        UInt32 nNumberOfBytesToWrite,
		                                        out UInt32 lpNumberOfBytesWritten,
		                                        IntPtr lpOverlapped);
		
		/// http://msdn.microsoft.com/en-us/library/windows/desktop/aa363791(v=vs.85).aspx
		/// <summary>
		/// Cancels all pending input and output (I/O) operations 
		/// that are issued by the calling thread for the specified file. 
		/// The function does not cancel I/O operations 
		/// that other threads issue for a file handle.
		/// </summary>
		[DllImport("kernel32.dll")]
		private static extern Boolean CancelIo(IntPtr hFile);
		
		/// http://msdn.microsoft.com/en-us/library/windows/desktop/aa363428(v=vs.85).aspx
		/// <summary>
		/// Discards all characters from the output or input buffer of a specified 
		/// communications resource. It can also terminate pending 
		/// read or write operations on the resource.
		/// </summary>
		[DllImport("kernel32.dll")]
		private static extern Boolean PurgeComm(IntPtr hFile, uint flags);
		
		/// http://msdn.microsoft.com/en-us/library/windows/desktop/aa363473(v=vs.85).aspx
		/// <summary>
		/// Transmits a specified character ahead of any pending data in the output buffer 
		/// of the specified communications device.
		/// </summary>
		[DllImport("kernel32.dll")]
		private static extern Boolean TransmitCommChar(IntPtr hFile, Byte cChar);

		/// http://msdn.microsoft.com/en-us/library/windows/desktop/aa363437(v=vs.85).aspx
		/// <summary>
		/// Sets the time-out parameters for all read and write operations 
		/// on a specified communications device.
		/// </summary>
		[DllImport("kernel32.dll")]
		private static extern Boolean SetCommTimeouts(IntPtr hFile, IntPtr lpCommTimeouts);


		//the handle for the serial device
		private IntPtr handle;


		public bool OpenSerial(string portName, int baudRate){
			handle = CreateFile (portName, READ_WRITE, 0, IntPtr.Zero, OPEN_EXISTING, 0x0, IntPtr.Zero);
			if(handle == (IntPtr) INVALID_HANDLE_VALUE){
				int error = Marshal.GetLastWin32Error();
				switch((uint)error){
				case ERROR_ACCESS_DENIED:
					Debug.LogError("Could not open serial port. Access Denied to " + portName);
					return false;
				case ERROR_FILE_NOT_FOUND:
					Debug.LogError("Could not open serial port. Could not find " + portName);
					return false;
				default:
					Debug.LogError("Could not open serial port. Unknown error: " + error);
					return false;
				}
			}
			
			//generate the COMMTIMEOUT struct
			//this struct marks an immediate return read and
			//10ms per character + 250ms write timeout
			COMMTIMEOUTS timeouts = new COMMTIMEOUTS(UInt32.MaxValue, 0, 0, 10, 250);
			//allocate the unmanaged memory for the struct
			IntPtr lpTimeouts = Marshal.AllocCoTaskMem(Marshal.SizeOf(timeouts));
			//push the struct data over to the unmanaged side
			Marshal.StructureToPtr(timeouts, lpTimeouts, false);

			if(!SetCommTimeouts(handle, lpTimeouts)){
				int error = Marshal.GetLastWin32Error();
				Debug.LogError ("SetCommTimeouts Failed. Error: " + error);

				Marshal.FreeCoTaskMem(lpTimeouts);
				return false;
			}

			Marshal.FreeCoTaskMem(lpTimeouts);
			return true;
		}

		public bool CloseSerial(){
			//don't try to close a null handle
			if(handle == (IntPtr) INVALID_HANDLE_VALUE){
				Debug.LogWarning("nothing to close");
				return false;
			}
			if(!CloseHandle(handle)){
				Debug.LogWarning("Close Serial Failed");

				handle = (IntPtr) INVALID_HANDLE_VALUE;
				return false;
			}
			handle = (IntPtr) INVALID_HANDLE_VALUE;
			return true;
		}

		public bool ReadChar(out char characterRead, out uint numRead){
			if(handle == (IntPtr) INVALID_HANDLE_VALUE){
				characterRead = '\n';
				numRead = 0;
				return false;
			}
			byte[] buf = new byte[1];
			bool readSuccessful = ReadFile (handle, buf, 1, out numRead, IntPtr.Zero);
			if(!readSuccessful){
				int error = Marshal.GetLastWin32Error();
				Debug.LogError ("ReadFile failed, error number: " + error);
				characterRead = '\n';
				return false;
			}
			if(numRead == 0){
				characterRead = '\n';
				return true;
			}
			characterRead = (char)buf[0];
			return true;
		}

		public bool WriteLine(string line){
			if(handle == (IntPtr) INVALID_HANDLE_VALUE){
				return false;
			}

			//translate the string into a byte array and append the new line
			char[] charBuf = line.ToCharArray();
			byte[] buf = new byte[line.Length + 1];
			for(int ii = 0; ii < line.Length; ii++){
				buf[ii] = (byte)charBuf[ii];
			}
			buf[line.Length] = (byte)'\r';

			uint numWritten;
			bool writeSuccessful = WriteFile(handle, buf, (uint)buf.Length, out numWritten, IntPtr.Zero);
			if(!writeSuccessful){
				int error = Marshal.GetLastWin32Error();
				Debug.LogError ("WriteFile failed, error number: " + error);
				return false;
			}
			if(numWritten < buf.Length){
				Debug.LogWarning("WriteFile failed, insufficient buffer space (partial write)");
				return false;
			}
			return true;
		}

		public bool PortOpen(){
			return (handle != (IntPtr)INVALID_HANDLE_VALUE);
		}

	}
#endif
}
