import type { PlaywrightTestOptions, PlaywrightWorkerOptions, Project } from '@playwright/test';

type BrowserProject = Project<PlaywrightTestOptions, PlaywrightWorkerOptions>;

/** Pure browser selection shared by Playwright configuration and its product scenarios. */
export function createBrowserProjects(options: {
  env: Record<string, string | undefined>;
  args: string[];
  exists: (path: string) => boolean;
  managedExecutablePath: () => string;
  devices: Record<string, BrowserProject['use']>;
}): BrowserProject[] {
  const { env, args, exists, devices } = options;
  const ci = !!env.CI;
  const executable = (paths: Array<string | undefined>): string | undefined =>
    paths.find(path => !!path && exists(path));
  const fallback = executable([
    env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH,
    !ci ? '/usr/bin/google-chrome' : undefined,
    !ci ? '/usr/bin/google-chrome-stable' : undefined,
    !ci ? '/usr/bin/chromium' : undefined,
    !ci ? '/usr/bin/chromium-browser' : undefined,
  ]);
  const chromium = executable([env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH,
    !ci ? options.managedExecutablePath() : undefined, fallback]);
  const requested = args.some((argument, index) => argument === '--project'
    ? ['firefox', 'webkit'].includes(args[index + 1])
    : ['--project=firefox', '--project=webkit'].includes(argument));
  const managed = ci || requested || env.PLAYWRIGHT_INCLUDE_MANAGED_BROWSERS === '1' || !fallback;
  return [
    { name: 'chromium', use: chromium
      ? { ...devices['Desktop Chrome'], launchOptions: { executablePath: chromium } }
      : { ...devices['Desktop Chrome'] } },
    ...(managed ? [
      { name: 'firefox', use: { ...devices['Desktop Firefox'] } },
      { name: 'webkit', use: { ...devices['Desktop Safari'] } },
    ] : []),
  ];
}
