# Public files and local data

Public documentation uses generic examples rather than the original user's hardware
and graphics preferences. New installations do not assume a personal legacy import
folder. Existing explicitly configured paths remain local and are preserved.

Profiles, captured game definitions, benchmarks, transaction backups, local settings
and diagnostic logs belong in the local library, not the repository or release ZIP.
Before sharing a screenshot, inspect visible names, paths, timestamps and overlays.
The README screenshot was explicitly supplied for publication. Repository ownership,
project author attribution and source references are intentional public information.

`tools/Check-PublicFiles.ps1` checks public text for common personal-path patterns and
rejects data-like repository/package entries. Packaging runs this check automatically.
It is a targeted check, not a comprehensive credential scanner, image OCR, or an
inspection of all strings inside a compressed executable.

Cleaning the current branch does not erase older Git commits, published release ZIPs,
forks or previously downloaded copies. Existing releases remain unchanged. If a secret
is discovered, revoke it first and plan targeted historical remediation. Removing
ordinary attribution or historical hardware observations does not justify silently
rewriting shared history or replacing a published artifact.

Local benchmark attachments can include information about other applications; review
them before sharing. A redacted support-export feature remains future work.
