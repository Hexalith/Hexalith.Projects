# Blind Hunter review triage

The configured reviewer computed N = min(floor(sqrt(13.197 kB) + 1), 10) = 4 and returned four findings. Every finding was checked against owning source and patched; no review finding is deferred.

| Finding | Verdict / disposition | Evidence and correction |
| --- | --- | --- |
| Missing Parties runtime dependency | medium / patch | ProjectsDomainProcessor grant/revoke -> ProjectOwnerBootstrapVerifier.IsCurrentOwnerAsync invokes Parties authority through EventStore. Added existing `parties` dependency and provenance. No new owner or fake dependency. |
| Missing bound-stream signing secret | medium / patch | ProjectChatBotAuthorityGateway.SendAsync requires Authentication:BoundStreamRead:PrivateKey before signing proof. Added logical secret only; README requires matching EventStore public trust. No key value or grant fabricated. |
| Fixed projection stores not explained | medium / patch | ProjectReadModelKeys.StoreName is statestore; ConversationStartSetupProjectionHandler.StoreName is projects-conversation-start-setup. Documented both fixed component prerequisites and that Projects:StateStore does not redirect them. No configuration key or supported binding invented. |
| Empty identity egress inventory | medium / patch | Projects bearer configuration and EventStore JwtWorkloadAssertionIssuer discover metadata/JWKS or call explicit/discovered token endpoint. Added five symbolic schema-supported egress destinations and workload-authority fallback documentation. No deployment URL selected. |

The source/runtime/release boundaries remain explicit. Public final validation/preparation and the two focused Debug builds/test lanes are repeated using the reviewed producer environment. Historical counts are not added to repeated counts. No commit or original Git mutation is authorized or performed by this session.
