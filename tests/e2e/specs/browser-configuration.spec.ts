import { createBrowserProjects } from '../support/helpers/browser-configuration.js';

import { expect, test } from '../support/merged-fixtures.js';

type BrowserScenario = {
    name: string;
    files: string[];
    env?: Record<string, string>;
    args?: string[];
    executable?: string;
    projects: string[];
};

const allProjects = ['chromium', 'firefox', 'webkit'];
const scenarios: BrowserScenario[] = [
    {
        name: 'explicit override takes precedence over managed and system Chromium',
        files: ['/override/chromium', '/managed/chromium', '/usr/bin/google-chrome'],
        env: { PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH: '/override/chromium' },
        executable: '/override/chromium', projects: ['chromium'],
    },
    {
        name: 'installed managed Chromium takes precedence over system Chrome',
        files: ['/managed/chromium', '/usr/bin/google-chrome'],
        executable: '/managed/chromium', projects: ['chromium'],
    },
    {
        name: 'system Chrome remains the fallback when managed Chromium is absent',
        files: ['/usr/bin/google-chrome'],
        executable: '/usr/bin/google-chrome', projects: ['chromium'],
    },
    {
        name: 'managed Chromium keeps the full matrix when no system browser exists',
        files: ['/managed/chromium'], executable: '/managed/chromium', projects: allProjects,
    },
    {
        name: 'CI keeps managed defaults and the full matrix despite a system browser',
        files: ['/managed/chromium', '/usr/bin/google-chrome'],
        env: { CI: '1' }, projects: allProjects,
    },
    {
        name: 'CI still honors an explicit installed Chromium override',
        files: ['/override/chromium'],
        env: { CI: '1', PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH: '/override/chromium' },
        executable: '/override/chromium', projects: allProjects,
    },
    {
        name: 'the explicit matrix flag includes Firefox and WebKit on system-browser hosts',
        files: ['/managed/chromium', '/usr/bin/google-chrome'],
        env: { PLAYWRIGHT_INCLUDE_MANAGED_BROWSERS: '1' },
        executable: '/managed/chromium', projects: allProjects,
    },
    {
        name: 'an explicitly requested Firefox project enables the full matrix',
        files: ['/managed/chromium', '/usr/bin/google-chrome'], args: ['--project', 'firefox'],
        executable: '/managed/chromium', projects: allProjects,
    },
    {
        name: 'a missing override falls back to installed managed Chromium',
        files: ['/managed/chromium', '/usr/bin/google-chrome'],
        env: { PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH: '/missing/chromium' },
        executable: '/managed/chromium', projects: ['chromium'],
    },
];

for (const scenario of scenarios) {
    test(`browser configuration: ${scenario.name}`, () => {
        const projects = createBrowserProjects({
            env: scenario.env ?? {}, args: ['node', 'playwright', 'test', ...(scenario.args ?? [])],
            exists: path => scenario.files.includes(path), managedExecutablePath: () => '/managed/chromium',
            devices: { 'Desktop Chrome': {}, 'Desktop Firefox': {}, 'Desktop Safari': {} },
        });
        expect(projects?.map((project) => project.name)).toEqual(scenario.projects);
        expect(projects?.find((project) => project.name === 'chromium')?.use?.launchOptions?.executablePath)
            .toBe(scenario.executable);
    });
}
