const packageProjects = [
  'src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj',
  'src/Hexalith.Projects/Hexalith.Projects.csproj',
  'src/Hexalith.Projects.Client/Hexalith.Projects.Client.csproj',
  'src/Hexalith.Projects.Testing/Hexalith.Projects.Testing.csproj',
  'src/Hexalith.Projects.ServiceDefaults/Hexalith.Projects.ServiceDefaults.csproj',
];

const packCommands = packageProjects.map((project) =>
  `dotnet pack ${project} --no-build --configuration Release --output ./nupkgs -p:HexalithProjectsPackageVersion=\${nextRelease.version} -p:HexalithCommonsRoot="$PWD/references/Hexalith.Commons" /m:1 /nr:false`,
);

module.exports = {
  branches: ['main'],
  plugins: [
    '@semantic-release/commit-analyzer',
    '@semantic-release/release-notes-generator',
    [
      '@semantic-release/exec',
      {
        prepareCmd: [
          'rm -rf ./nupkgs',
          'mkdir -p ./nupkgs',
          ...packCommands,
          'pwsh ./tests/tools/run-package-dependency-gate.ps1 -Version \${nextRelease.version} -PackageDirectory ./nupkgs -SkipPack',
        ].join(' && '),
        publishCmd: [
          'test "$(find ./nupkgs -maxdepth 1 -name \'*.nupkg\' ! -name \'*.symbols.*\' | wc -l)" -eq 5',
          'dotnet nuget push ./nupkgs/*.nupkg --api-key "$NUGET_API_KEY" --source https://api.nuget.org/v3/index.json --skip-duplicate',
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
