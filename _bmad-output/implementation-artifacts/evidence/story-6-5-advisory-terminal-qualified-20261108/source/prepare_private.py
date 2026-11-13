from pathlib import Path
import difflib,hashlib,json
P=Path(__file__).resolve().parent
raw=(P/'ProjectAdvisoryQueryExecutor.original.cs').read_bytes();s=raw.decode();edits=[]
def change(old,new):
 global s
 assert s.count(old)>=1,(old,s.count(old));s=s.replace(old,new,1);edits.append({'old':old,'new':new})
def insert_before(line,added):change(line,added+line)
insert_before('        // One linked dependency budget begins with scope authorization, before validating caller input.\r\n',
 '        using var terminal = conversation ? null : Hexalith.PrivateCaptureCounter.AdvisoryTerminalCapture.Begin();\r\n')
insert_before('        try\r\n        {\r\n            if (!await ScopeAsync(query, token).WaitAsync(token).ConfigureAwait(false)) return Denied();',
 '        try\r\n        {\r\n')
# Distinct marker before the first original try, no original await/filter/token expression changes.
insert_before('            if (!await ScopeAsync(query, token).WaitAsync(token).ConfigureAwait(false)) return Denied();',
 '            terminal?.Enter(Hexalith.PrivateCaptureCounter.AdvisoryBoundary.InitialScope);\r\n')
# Preserve the exact if/await expression; only give its original return branch a fixed marker.
change('            if (!await ScopeAsync(query, token).WaitAsync(token).ConfigureAwait(false)) return Denied();',
 '            if (!await ScopeAsync(query, token).WaitAsync(token).ConfigureAwait(false))\r\n            {\r\n                terminal?.Termination(Hexalith.PrivateCaptureCounter.AdvisoryTermination.DenialReturnEntered);\r\n                return Denied();\r\n            }')
change('        catch (OperationCanceledException) when (callerToken.IsCancellationRequested) { throw; }\r\n        catch (Exception exception)\r\n        {\r\n            LogFailure(query, exception);',
 '        catch (OperationCanceledException) when (callerToken.IsCancellationRequested)\r\n        {\r\n            terminal?.Failure(Hexalith.PrivateCaptureCounter.AdvisoryExceptionKind.Cancellation);\r\n            terminal?.Termination(Hexalith.PrivateCaptureCounter.AdvisoryTermination.CallerCancellationRethrown);\r\n            throw;\r\n        }\r\n        catch (Exception exception)\r\n        {\r\n            terminal?.Failure(exception is OperationCanceledException ? Hexalith.PrivateCaptureCounter.AdvisoryExceptionKind.Cancellation : Hexalith.PrivateCaptureCounter.AdvisoryExceptionKind.Other);\r\n            terminal?.Termination(Hexalith.PrivateCaptureCounter.AdvisoryTermination.InitialScopeFallbackEntered);\r\n            LogFailure(query, exception);')
insert_before('        if (query.AggregateId != "projects" || query.Paging is not null || query.IsGlobalAdmin',
 '        terminal?.Enter(Hexalith.PrivateCaptureCounter.AdvisoryBoundary.EnvelopeValidation);\r\n')
# The three original Invalid returns stay under their exact original condition.
for old in [
 '            || query.EntityId is not null && query.EntityId != query.AggregateId) return Invalid();',
 '                || input is null || !Identity(query.TenantId, input.ConversationId)) return Invalid();',
 '                    || !Identity(query.TenantId, item.ReferenceId))) return Invalid();']:
 change(old,old.replace('return Invalid();','{ terminal?.Termination(Hexalith.PrivateCaptureCounter.AdvisoryTermination.InvalidReturnEntered); return Invalid(); }'))
insert_before('            ProjectResolution resolved = new ProjectResolutionEngine().Resolve(',
 '            terminal?.Enter(Hexalith.PrivateCaptureCounter.AdvisoryBoundary.Response);\r\n')
# Only response construction is held in a local, with its original expression evaluated once.
change('            return QueryResult.FromPayload(JsonSerializer.SerializeToElement(new ProjectAdvisoryResolutionResponse(',
 '            QueryResult response = QueryResult.FromPayload(JsonSerializer.SerializeToElement(new ProjectAdvisoryResolutionResponse(')
insert_before('        }\r\n\r\n        try\r\n        {\r\n            ConversationAdvisoryMetadataResult? observed = null;',
 '            terminal?.ResponseReturned(incomplete != 0);\r\n            return response;\r\n')
markers=[
 ('                var read = await ConversationAsync(query, conversationId, token).WaitAsync(token).ConfigureAwait(false);','InitialConversation'),
 ('            string[] acceptedInputs = await FilterInputsAsync(inputs.Where(item => item.ReferenceKind == "folder")','InitialInputs'),
 ('                var index = await store.GetAsync<ProjectReadIndex>(ProjectReadModelKeys.StoreName,','Index'),
 ('                await Parallel.ForEachAsync(selected, new ParallelOptions','Discovery'),
 ('                var final = await ConversationAsync(query, conversationId, token).WaitAsync(token).ConfigureAwait(false);','FinalConversation'),
 ('            acceptedInputs = await FilterInputsAsync(acceptedInputs, query, Omitted, token).WaitAsync(token).ConfigureAwait(false);','FinalInputs'),
 ('            await Parallel.ForEachAsync(candidates.ToArray(), new ParallelOptions','FinalCandidates'),
 ('            if (!await ScopeAsync(query, token).WaitAsync(token).ConfigureAwait(false)) return Denied();','FinalScope')]
for line,stage in markers:
 indent=line[:len(line)-len(line.lstrip())];insert_before(line,indent+'terminal?.Enter(Hexalith.PrivateCaptureCounter.AdvisoryBoundary.'+stage+');\r\n')
# Original return branch outcomes, with no DTO passed to the recorder.
for old in ['                if (read.Denied) return Denied();','                if (final.Denied) return Denied();','            if (!await ScopeAsync(query, token).WaitAsync(token).ConfigureAwait(false)) return Denied();']:
 change(old,old.replace('return Denied();','{ terminal?.Termination(Hexalith.PrivateCaptureCounter.AdvisoryTermination.DenialReturnEntered); return Denied(); }'))
change('        catch (OperationCanceledException) when (callerToken.IsCancellationRequested) { throw; }\r\n        catch (Exception exception) { LogFailure(query, exception); Omitted(); return Response([]); }\r\n    }',
 '        catch (OperationCanceledException) when (callerToken.IsCancellationRequested)\r\n        {\r\n            terminal?.Failure(Hexalith.PrivateCaptureCounter.AdvisoryExceptionKind.Cancellation);\r\n            terminal?.Termination(Hexalith.PrivateCaptureCounter.AdvisoryTermination.CallerCancellationRethrown);\r\n            throw;\r\n        }\r\n        catch (Exception exception)\r\n        {\r\n            terminal?.Failure(exception is OperationCanceledException ? Hexalith.PrivateCaptureCounter.AdvisoryExceptionKind.Cancellation : Hexalith.PrivateCaptureCounter.AdvisoryExceptionKind.Other);\r\n            terminal?.Termination(Hexalith.PrivateCaptureCounter.AdvisoryTermination.OperationFallbackEntered);\r\n            LogFailure(query, exception); Omitted(); return Response([]);\r\n        }\r\n        }\r\n        finally\r\n        {\r\n            terminal?.Close(callerToken.IsCancellationRequested, token.IsCancellationRequested);\r\n        }\r\n    }')
private=s.encode();(P/'ProjectAdvisoryQueryExecutor.private.cs').write_bytes(private)
rename='public sealed partial class ProjectAdvisoryQueryExecutor('
assert s.count(rename)==1
instrumented=s.replace(rename,'public sealed partial class InstrumentedProjectAdvisoryQueryExecutor(',1)
instrumented=instrumented.replace('    /// <summary>The maximum distinct Project details examined per query.</summary>', '    internal Hexalith.PrivateCaptureCounter.AdvisoryTerminalCapture? TerminalForProof { get; private set; }\r\n\r\n    /// <summary>The maximum distinct Project details examined per query.</summary>',1)
instrumented=instrumented.replace('        using var terminal = conversation ? null : Hexalith.PrivateCaptureCounter.AdvisoryTerminalCapture.Begin();', '        using var terminal = conversation ? null : Hexalith.PrivateCaptureCounter.AdvisoryTerminalCapture.Begin();\r\n        TerminalForProof = terminal;',1)
(P/'InstrumentedProjectAdvisoryQueryExecutor.cs').write_bytes(instrumented.encode())
r=s
for e in reversed(edits):assert r.count(e['new'])==1;r=r.replace(e['new'],e['old'],1)
assert r.encode()==raw
(P/'normalization.json').write_text(json.dumps({'edits':edits,'reverseRestoresOriginalBytes':True},indent=2)+'\n')
(P/'source.patch').write_text(''.join(difflib.unified_diff(raw.decode().splitlines(keepends=True),s.splitlines(keepends=True),fromfile='original/ProjectAdvisoryQueryExecutor.cs',tofile='private/ProjectAdvisoryQueryExecutor.cs')))
print('private',hashlib.sha256(private).hexdigest(),'edits',len(edits))
