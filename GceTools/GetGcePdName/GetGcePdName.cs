// Code for .exe version of Get-GcePdName. This code may be or become broken now
// that the Powershell module version (GetGcePdNameCommand.cs) is working.

using System;

namespace GceTools
{
  public class GetGcePdName
  {
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

      string name = GcePdLib.Get_GcePdName(String.Format("{0}", driveNumber));

      // TODO(pjh): handle null return value here.
      Console.WriteLine(name);
    }
  }
}
