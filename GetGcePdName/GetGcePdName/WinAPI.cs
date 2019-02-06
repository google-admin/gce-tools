// Copied and adapted from
// https://github.com/DKorablin/DeviceIoControl/blob/master/DeviceIoControl/Native/WinAPI.cs

using System;

namespace AlphaOmega.Debug
{
  /// <summary>Native structures</summary>
  public struct WinAPI
	{
    /// <summary>Define the method codes for how buffers are passed for I/O and FS controls</summary>
    public enum METHOD : byte
    {
      /// <summary>Specifies the buffered I/O method, which is typically used for transferring small amounts of data per request.</summary>
      BUFFERED = 0,
      /// <summary>Specifies the direct I/O method, which is typically used for reading or writing large amounts of data, using DMA or PIO, that must be transferred quickly.</summary>
      IN_DIRECT = 1,
      /// <summary>Specifies the direct I/O method, which is typically used for reading or writing large amounts of data, using DMA or PIO, that must be transferred quickly.</summary>
      OUT_DIRECT = 2,
      /// <summary>
      /// Specifies neither buffered nor direct I/O.
      /// The I/O manager does not provide any system buffers or MDLs.
      /// The IRP supplies the user-mode virtual addresses of the input and output buffers that were specified to DeviceIoControl or IoBuildDeviceIoControlRequest, without validating or mapping them.
      /// </summary>
      NEITHER = 3,
    }
    [Flags]
		public enum FILE_ACCESS_FLAGS : uint
		{
			/// <summary>Read</summary>
			GENERIC_READ = 0x80000000,
			/// <summary>Write</summary>
			GENERIC_WRITE = 0x40000000,
			/// <summary>Execute</summary>
			GENERIC_EXECUTE = 0x20000000,
			/// <summary>All</summary>
			GENERIC_ALL = 0x10000000,
		}
		/// <summary>Share</summary>
		[Flags]
		public enum FILE_SHARE : uint
		{
			/// <summary>
			/// Enables subsequent open operations on a file or device to request read access.
			/// Otherwise, other processes cannot open the file or device if they request read access.
			/// </summary>
			READ = 0x00000001,
			/// <summary>
			/// Enables subsequent open operations on a file or device to request write access.
			/// Otherwise, other processes cannot open the file or device if they request write access.
			/// </summary>
			WRITE = 0x00000002,
			/// <summary>
			/// Enables subsequent open operations on a file or device to request delete access.
			/// Otherwise, other processes cannot open the file or device if they request delete access.
			/// If this flag is not specified, but the file or device has been opened for delete access, the function fails.
			/// </summary>
			DELETE = 0x00000004,
		}
    /// <summary>Disposition</summary>
    public enum CreateDisposition : uint
    {
      /// <summary>Create new</summary>
      CREATE_NEW = 1,
      /// <summary>Create always</summary>
      CREATE_ALWAYS = 2,
      /// <summary>Open exising</summary>
      OPEN_EXISTING = 3,
      /// <summary>Open always</summary>
      OPEN_ALWAYS = 4,
      /// <summary>Truncate existing</summary>
      TRUNCATE_EXISTING = 5,
    }
  }
}