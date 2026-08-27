import { resolve } from 'node:path';

/** Server-session storage state used by live Chromium; it contains only HttpOnly cookies. */
export const browserSessionStoragePath = resolve('.auth', 'projects-ui-browser-session.json');
