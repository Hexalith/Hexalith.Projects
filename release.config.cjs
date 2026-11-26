const packageIds = [
  'Hexalith.Projects.EventStoreExtensions',
  'Hexalith.Projects.Contracts',
  'Hexalith.Projects',
  'Hexalith.Projects.Client',
  'Hexalith.Projects.Testing',
  'Hexalith.Projects.ServiceDefaults',
];

const packCommands = packageIds.map((id) =>
  `dotnet pack src/${id}/${id}.csproj --no-build --configuration Release --output ./nupkgs -p:HexalithProjectsPackageVersion=\${nextRelease.version} -p:HexalithCommonsRoot="$PWD/references/Hexalith.Commons" /m:1 /nr:false`,
);

// Publish the new package first so a refused creation leaves the existing IDs untouched.
const publishCommands = packageIds.map((id) =>
  `dotnet nuget push "./nupkgs/${id}.\${nextRelease.version}.nupkg" --api-key "$NUGET_API_KEY" --source https://api.nuget.org/v3/index.json`,
);

const checkReleaseAvailability = 'python3 scripts/check-release-version-available.py ${nextRelease.version}';

module.exports = {
  branches: ['main'],
  plugins: [
    '@semantic-release/commit-analyzer',
    '@semantic-release/release-notes-generator',
    [
      '@semantic-release/exec',
      {
        verifyReleaseCmd: checkReleaseAvailability,
        prepareCmd: [
          'dotnet restore Hexalith.Projects.CI.slnx',
          'rm -rf ./nupkgs',
          'mkdir -p ./nupkgs',
          ...packCommands,
          'pwsh ./tests/tools/run-package-dependency-gate.ps1 -Version \${nextRelease.version} -PackageDirectory ./nupkgs -SkipPack',
        ].join(' && '),
        publishCmd: [
          'test "$(find ./nupkgs -maxdepth 1 -name \'*.nupkg\' ! -name \'*.symbols.*\' | wc -l)" -eq 6',
          checkReleaseAvailability,
          ...publishCommands,
        ].join(' && '),
      },
    ],
    [
      '@semantic-release/github',
      {
        assets: ['nupkgs/*.nupkg'],
      },
    ],
  ],
};
