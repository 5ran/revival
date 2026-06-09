using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

internal sealed class ProcessMemory : IDisposable
{
	private const uint ProcessVmRead = 16u;

	private const uint ProcessQueryInformation = 1024u;

	private const uint ProcessQueryLimitedInformation = 4096u;

	private readonly IntPtr handle;

	private ProcessMemory(IntPtr handle)
	{
		this.handle = handle;
	}

	public static ProcessMemory Open(int pid)
	{
		IntPtr intPtr = OpenProcess(5136u, inheritHandle: false, pid);
		if (intPtr == IntPtr.Zero)
		{
			throw new InvalidOperationException("OpenProcess failed. Win32 error: " + Marshal.GetLastWin32Error().ToString(CultureInfo.InvariantCulture));
		}
		return new ProcessMemory(intPtr);
	}

	public ulong ReadPtr(ulong address)
	{
		return ReadUInt64(address);
	}

	public ulong ReadUInt64(ulong address)
	{
		byte[] array = ReadBytes(address, 8);
		if (array == null)
		{
			return 0uL;
		}
		return BitConverter.ToUInt64(array, 0);
	}

	public string ReadRobloxString(ulong stringAddress)
	{
		if (!IsLikelyUserModeAddress(stringAddress))
		{
			return string.Empty;
		}
		ulong num = ReadUInt64(stringAddress + 16);
		if (num == 0 || num > 512)
		{
			return string.Empty;
		}
		ulong address = ((num >= 16) ? ReadPtr(stringAddress) : stringAddress);
		if (!IsLikelyUserModeAddress(address))
		{
			return string.Empty;
		}
		byte[] array = ReadBytes(address, (int)Math.Min(num, 512uL));
		if (array == null || array.Length == 0)
		{
			return string.Empty;
		}
		int num2 = Array.IndexOf(array, (byte)0);
		int count = ((num2 >= 0) ? num2 : array.Length);
		return Encoding.UTF8.GetString(array, 0, count);
	}

	public byte[] ReadBytes(ulong address, int count)
	{
		if (!IsLikelyUserModeAddress(address) || count <= 0)
		{
			return null;
		}
		byte[] array = new byte[count];
		if (!ReadProcessMemory(handle, new IntPtr((long)address), array, array.Length, out var bytesRead) || bytesRead.ToInt64() != count)
		{
			return null;
		}
		return array;
	}

	public static bool IsLikelyUserModeAddress(ulong address)
	{
		return address >= 65536 && address < 140737488355328L;
	}

	public void Dispose()
	{
		if (handle != IntPtr.Zero)
		{
			CloseHandle(handle);
		}
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern IntPtr OpenProcess(uint desiredAccess, bool inheritHandle, int processId);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool ReadProcessMemory(IntPtr process, IntPtr baseAddress, byte[] buffer, int size, out IntPtr bytesRead);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool CloseHandle(IntPtr handle);
}
