# Final review corrections

Both owner-directory factories now resolve the delegated client through the active HttpContext RequestServices. SDK lifetimes and host registrations remain unchanged. Four controls cover both registration orders, with and without a host-provided transport, reusing root-captured directories across two validated request scopes. Folder resolution is asserted before the Conversation call so a swallowed Folder scope error cannot pass vacuously.

The Conversations Contracts README now distinguishes strict native-proof requirements from advisory ordinary owner Read authorization, lag and incomplete coverage, and independent context/confirmation authority.

Narrow Server and test-project builds passed with zero warnings/errors. Only the new directory-registration class and existing module-registration class ran: 20 tests passed, zero failures/skips. Exact commands, source hashes and passing XML are in this directory. Private runtime output remains at `/tmp/story64-final-directory-registration.private.log`.

These corrections postdate the parent review-4 source manifest and full-suite receipts. Parent retains the final full verification and refreshed aggregate evidence. No wider tests, release or Git mutation was performed in this correction pass.
