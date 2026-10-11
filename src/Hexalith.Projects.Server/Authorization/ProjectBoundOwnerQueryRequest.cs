using Hexalith.EventStore.Contracts.Queries;

namespace Hexalith.Projects.Server.Authorization;

internal sealed record ProjectBoundOwnerQueryRequest(SubmitQueryRequest Query, string PrincipalId);
