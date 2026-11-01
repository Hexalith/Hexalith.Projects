"""Apply only approved owner patches and preserve original source/evidence identities."""
from record import ROOT, EVIDENCE, WORK, run, write, digest, state, sources, git
import gzip, json, shutil

write('git-before.json', state())
with gzip.open(EVIDENCE/'sources-before.json.gz','wt') as output: json.dump(sources(),output,sort_keys=True)
previous={str(p.relative_to(EVIDENCE.parent)):digest(p) for p in EVIDENCE.parent.rglob('*') if p.is_file() and EVIDENCE not in p.parents}
write('historical-evidence-before.json',previous)
parties=ROOT/'references/Hexalith.Parties'
write('shared-parties-before.json',{str(p.relative_to(parties)):digest(p) for p in parties.rglob('*') if p.is_file() and not any(x in ('.git','node_modules') for x in p.relative_to(parties).parts)})
proposal=json.loads((EVIDENCE.parent/'proposed-repair.json').read_text())
for owner,entry in proposal.items():
    repo=ROOT if owner=='projects' else ROOT/'references'/('Hexalith.'+owner.title())
    assert git(repo,'rev-parse','HEAD').decode().strip()==entry['head']
    for item in entry['files']: assert digest(repo/item['path'])==item['current_sha256'],item
    patch=EVIDENCE.parent/entry['patch']; assert digest(patch)==entry['patch_sha256']
    assert run(owner+'-patch-check',['git','apply','--check',str(patch)],cwd=repo)==0
    assert run(owner+'-patch-apply',['git','apply',str(patch)],cwd=repo)==0
    for item in entry['files']:assert digest(repo/item['path'])==item['candidate_sha256'],item

path=ROOT/'tests/Hexalith.Projects.Server.Tests/Queries/SupportedProjectReadTests.cs'
raw=path.read_bytes();s=raw.decode();newline='\r\n' if b'\r\n' in raw else '\n';s=s.replace('\r\n','\n')
old='        ProjectListPage page = SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(fixture.Query(new ListProjectsQuery()), TestContext.Current.CancellationToken));'
new='''        QueryResult result = await fixture.Executor.ListAsync(fixture.Query(new ListProjectsQuery()), TestContext.Current.CancellationToken);
        result.Success.ShouldBeTrue(); result.ProjectionType.ShouldBeNull(); result.Metadata.ShouldBeNull();
        ProjectListPage page = SupportedProjectReadFixture.Payload<ProjectListPage>(result);'''
assert old in s;s=s.replace(old,new,1)
s=s.replace('        safe.Success.ShouldBeTrue();','        safe.Success.ShouldBeTrue(); safe.ProjectionType.ShouldBeNull(); safe.Metadata.ShouldBeNull();',1)
old='        GetProjectResponse detail = SupportedProjectReadFixture.Payload<GetProjectResponse>(await fixture.Executor.GetAsync(inspected, TestContext.Current.CancellationToken));'
new='''        QueryResult inspectedResult = await fixture.Executor.GetAsync(inspected, TestContext.Current.CancellationToken);
        inspectedResult.Success.ShouldBeTrue(); inspectedResult.ProjectionType.ShouldBeNull(); inspectedResult.Metadata.ShouldBeNull();
        GetProjectResponse detail = SupportedProjectReadFixture.Payload<GetProjectResponse>(inspectedResult);'''
assert old in s;s=s.replace(old,new,1)
# Keep the original catalog-enforced fixtures byte-for-byte. Exercise rejection using their public seam.
marker='    [Fact]\n    public async Task ListDefaultsToFiftyAndPreservesConversationPolicy()'
addition='''#if HEXALITH_SOURCE_FIXTURE
    [Theory]
    [InlineData("projection")]
    [InlineData("actor")]
    [InlineData("uncommitted-query")]
    public async Task GatewayCatalogRefusesTargetsOutsideCommittedAuthorityWithoutWrites(string mismatch)
    {
        await using ProjectReadAuthorityFixture fixture = await ProjectReadAuthorityFixture.CreateAsync();
        ProjectAuthoritySnapshot[] before = await fixture.SnapshotAsync();
        SubmitQueryRequest request = mismatch switch
        {
            "projection" => fixture.Open() with { ProjectionType = GetProjectQuery.QueryType },
            "actor" => fixture.Open() with { ProjectionActorType = "uncommitted-actor" },
            _ => fixture.List() with { QueryType = ResolveProjectFromAttachmentsQuery.QueryType },
        };
        int acquisitions = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
            .GetRequiredService<GatewayFixtureRouteCatalog>(fixture.Gateway.Services).AcquisitionCount;
        using System.Net.Http.HttpResponseMessage response = await System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(
            fixture.Client, "/api/v1/queries", request, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.InternalServerError);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        foreach (string sensitive in new[] { fixture.Tenant, "project-a", "actor-1", "uncommitted-actor", request.QueryType })
            body.ShouldNotContain(sensitive);
        Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
            .GetRequiredService<GatewayFixtureRouteCatalog>(fixture.Gateway.Services).AcquisitionCount.ShouldBeGreaterThan(acquisitions);
        await fixture.AssertUnchangedAsync(before);
    }
#endif

'''
assert marker in s;s=s.replace(marker,addition+marker,1)
path.write_bytes(s.replace('\n',newline).encode())
write('applied-repair.json',{'approved_patches':proposal,'additional_assertions':{'path':str(path.relative_to(ROOT)),'sha256':digest(path)},'preserved_fixture_sha256':{p:digest(ROOT/p) for p in ['tests/Hexalith.Projects.Server.Tests/Queries/ProjectReadAuthorityFixture.cs','tests/Hexalith.Projects.Server.Tests/Queries/SupportedProjectGatewayHttpTests.cs','tests/Hexalith.Projects.Server.Tests/Queries/GatewayFixtureRouteCatalog.cs']}})
print('Approved source repair applied; original fixtures preserved.',flush=True)
