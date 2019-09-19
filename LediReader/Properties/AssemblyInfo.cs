using System;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: CLSCompliant(true)]
[assembly: StringFreezing()]

[assembly: AssemblyTitle("LediReader")]
[assembly: AssemblyDescription("LediReader main executable")]
[assembly: AssemblyConfiguration("REVID: $REVID$, BRANCH: $BRANCH$, DATE: $REVDATE$")]
[assembly: AssemblyCompany("https://github.com/lellid/LediReader")]
[assembly: AssemblyProduct("LediReader")]
[assembly: AssemblyCopyright("(C) Dr. Dirk Lellinger 2019-$YEAR$")]
[assembly: AssemblyTrademark("(C) Dr. Dirk Lellinger 2019-$YEAR$")]
[assembly: AssemblyCulture("")]

[assembly: AssemblyVersion("$MAJORVERSION$.$MINORVERSION$.$REVNUM$.$DIRTY$")]
[assembly: AssemblyFileVersion("$MAJORVERSION$.$MINORVERSION$.$REVNUM$.$DIRTY$")]
[assembly: AssemblyInformationalVersion("$MAJORVERSION$.$MINORVERSION$.$REVNUM$.$DIRTY$ $REVIDSHORT$ $REVDATE$")]

[assembly: AssemblyDelaySign(false)]

internal static class RevisionClass
{
  public const string Major = "$MAJORVERSION$";
  public const string Minor = "$MINORVERSION$";
  public const string Build = "$REVNUM$";
  public const string Revision = "$DIRTY$";

  public const string MainVersion = Major + "." + Minor;
  public const string FullVersion = Major + "." + Minor + "." + Build + "." + Revision;

  public const string BranchName = "$BRANCH$";
  public const string RevisionID = "$REVID$";
}
