namespace Story65.ReceiverPhase;
using System.Runtime.CompilerServices;
internal static class Check
{
    internal static void Require(bool condition,string closed,[CallerFilePath]string file="",[CallerLineNumber]int line=0)
    { if(!condition)throw new DriverFailure(closed,file,line); }
}
