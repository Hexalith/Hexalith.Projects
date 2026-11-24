"""Projects requires the Conversations AppHost Toolkit dependency in G-6."""

CONVERSATIONS_APPHOST = (
    "references/Hexalith.Conversations/src/Hexalith.Conversations.AppHost/"
    "Hexalith.Conversations.AppHost.csproj"
)
TOOLKIT_PACKAGE = "CommunityToolkit.Aspire.Hosting.Dapr"


def toolkit_issue(audit: dict) -> str | None:
    """Check that the selected AppHost actually restored the approved Toolkit."""
    version = audit.get("effectiveTuple", {}).get("communityToolkitAspireDapr")
    if not version:
        return "Current G-6 audit did not determine the effective Toolkit version"
    restored = [
        package["version"]
        for project in audit.get("resolvedPackages", [])
        if project["path"] == CONVERSATIONS_APPHOST
        for package in project["packages"]
        if package["id"].casefold() == TOOLKIT_PACKAGE.casefold()
    ]
    if restored != [version]:
        return f"{CONVERSATIONS_APPHOST} must restore {TOOLKIT_PACKAGE} {version}; found {restored}"
    return None
