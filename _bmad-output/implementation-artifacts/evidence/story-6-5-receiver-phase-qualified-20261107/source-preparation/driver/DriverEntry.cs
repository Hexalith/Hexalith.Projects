namespace Story65.ReceiverPhase;
using System.Text.Json;
public static class DriverEntry
{
    public static async Task<int> RunAsync(string[] args)
    {
        string? receipt=args.Length>=5?args[4]:null;
        try
        {
            Check.Require(args.Length>=5,"closed-driver-cli");
            string mode=args[0],group=args[1];RuntimeInputs inputs=RuntimeInputs.Load(args[2],args[3]);
            await SaveAsync(Path.Combine(Path.GetDirectoryName(receipt!)!,"runtime-facts.json"),inputs.Facts()).ConfigureAwait(false);
            var api=new FixtureAdapter(inputs);
            Check.Require(api.Names().Order(StringComparer.Ordinal).SequenceEqual(CaseGroups.All.Values.SelectMany(x=>x).Order(StringComparer.Ordinal)),"exact-companion-case-catalog");
            if(mode=="facts"){await SaveAsync(receipt!,inputs.Facts()).ConfigureAwait(false);Console.WriteLine("FACTS_DONE");return 0;}
            if(mode=="native")
            {
                Check.Require(args.Length==6,"native-xml-path-required");
                int exit=await NativeRunner.RunAsync(group,args[5],inputs).ConfigureAwait(false);
                await SaveAsync(receipt!,new {Mode=mode,Selection=group,Passed=exit==0,ExpectedCases=NativeRunner.Selections[group],NativeAssertionsUnchanged=true}).ConfigureAwait(false);
                Console.WriteLine("NATIVE_DONE");return exit;
            }
            object result;
            if(mode is "canary-positive-control" or "canary-current" or "canary-suppressed")
                result=await FreshFlowCanary.RunAsync(mode,inputs,api).ConfigureAwait(false);
            else if(mode=="excluded")result=await ExcludedDriver.RunAsync(inputs,api).ConfigureAwait(false);
            else {Check.Require(mode is "disabled" or "active" or "pre-capped" or "failing-sink","closed-recording-mode");result=await FocusedDriver.RunAsync(mode,group,inputs,api).ConfigureAwait(false);}
            inputs.VerifyLoaded();inputs.VerifyFiles();await SaveAsync(receipt!,result).ConfigureAwait(false);
            bool counterFinish=inputs.Variant=="private" && inputs.Role!="excluded" && mode!="canary-positive-control";
            if(counterFinish)await new CounterControl(inputs).FinishAsync(mode=="canary-current" || mode=="canary-suppressed"?"active":mode).ConfigureAwait(false);
            else {Console.WriteLine("CASES_DONE");await Protocol.ExpectAsync("EXIT").ConfigureAwait(false);}
            return 0;
        }
        catch(Exception error)
        {
            if(receipt is not null && Path.IsPathFullyQualified(receipt) && !File.Exists(receipt))
                await SaveAsync(receipt,new {Passed=false,Failure=SafeFailure.From(error),RawExceptionPersisted=false}).ConfigureAwait(false);
            Console.WriteLine("CLOSED_FAILURE");return 1;
        }
    }
    private static async Task SaveAsync(string path,object value)
    {
        Check.Require(Path.IsPathFullyQualified(path) && !File.Exists(path),"fresh-result-file");
        await using FileStream stream=new(path,FileMode.CreateNew,FileAccess.Write,FileShare.Read);
        await JsonSerializer.SerializeAsync(stream,value,new JsonSerializerOptions{WriteIndented=true}).ConfigureAwait(false);
    }
}
