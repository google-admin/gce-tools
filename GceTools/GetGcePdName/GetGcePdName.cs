using System;

namespace GceTools
{
  public class GetGcePdName
  {
#if false
    private static void GetArgs(string[] args, ref long driveNumber)
    {
      if (args.Length != 1)
      {
        Console.WriteLine("Usage: GetGcePdName.exe <physical drive number>");
        Environment.Exit(1);
      }
      driveNumber = Convert.ToInt64(args[0]);
      if (driveNumber < 0)
      {
        Console.WriteLine("Please enter a positive drive number");
        Environment.Exit(1);
      }
    }

    static void Main(string[] args)
    {
      long driveNumber = -1;
      GetArgs(args, ref driveNumber);
      // https://stackoverflow.com/a/18074777/1230197 suggests that
      // string should work for LPCTSTR.
      string physicalDrive = @"\\.\PHYSICALDRIVE" + driveNumber;

      string name = GcePdLib.Get_GcePdName(physicalDrive);
      // TODO(pjh): handle null return value here.
      Console.WriteLine(name);
    }

#endif
  }
}
