using Hexalith.EventStore.Testing.CatalogInvocation;
using Hexalith.EventStore.Testing.OwnerSourceRead;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.ServiceDefaults.Routing;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hexalith.Projects.PackageFixtures;

/// <summary>Finite private source proof worker hosted only inside the credential-owning process.</summary>
internal sealed class PackageOwnerWorker(OwnerSourceReadOptions selection,
    Func<StreamReadRequest, CancellationToken, Task<StreamReadPage>> read,
    ILogger<PackageOwnerWorker> logger) : BackgroundService
{
    private readonly PackageIpcFiles _files = new(selection.Directory);

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (string path in _files.Pending())
            {
                string id = Path.GetFileNameWithoutExtension(path);
                _files.Claim(id);
                using var operation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                operation.CancelAfter(TimeSpan.FromSeconds(45));
                OwnerSourceReadResult result;
                try
                {
                    var request = await _files.ReadAsync<OwnerSourceReadRequest>(id, ".working", operation.Token).ConfigureAwait(false);
                    if (request.Id != id) throw new InvalidOperationException("Mismatched private owner source identity.");
                    result = new(id, await InvokeAsync(request, operation.Token).ConfigureAwait(false), null);
                }
                catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
                {
                    RouteCatalogRefusalDiagnostics.Report(logger, "package-owner-worker", exception);
                    result = new(id, null, exception.GetType().Name);
                }
                try { await _files.WriteAsync(id, ".response", result, operation.Token).ConfigureAwait(false); }
                catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested) { }
            }
            await Task.Delay(25, stoppingToken).ConfigureAwait(false);
        }
    }

    /// <summary>Admits only the exact bootstrap stream and propagates actual SDK failures and deadlines.</summary>
    internal async Task<StreamReadPage> InvokeAsync(OwnerSourceReadRequest request, CancellationToken hostToken)
    {
        PackageOwnerSelection.Validate(request);
        _ = _files.PathFor(request.Id, ".working");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(hostToken);
        deadline.CancelAfter(request.ExpiresAt - DateTimeOffset.UtcNow);
        deadline.Token.ThrowIfCancellationRequested();
        StreamReadPage page = await read(new(request.Scope, request.Domain, request.AggregateId,
            PageSize: PackageOwnerSelection.MaximumEvents), deadline.Token).ConfigureAwait(false);
        deadline.Token.ThrowIfCancellationRequested();
        if (DateTimeOffset.UtcNow >= request.ExpiresAt) throw new OperationCanceledException("Private owner source deadline elapsed.");
        PackageOwnerSelection.ValidatePage(page);
        return page;
    }
}
