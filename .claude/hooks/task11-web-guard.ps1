# PreToolUse hook: deny EVERY WebSearch and WebFetch call. Task 11 (SDK migrations).
#
# TASK-11 VARIANT (2026-11-20). Unlike the task 1-10 guards, which deny only lookups that touch
# the provider, this guard denies all web lookups, for every arm. Two reasons:
#
#  1. The prompts say so. The none arm's prompt says "No documentation, plugin or spec is
#     provided. You have the code, the build, and the package that the build restores." A guard
#     that let it read the provider's docs would make that sentence false and turn the none arm
#     into a docs arm, the leak knowledge/opencode-web-guard-detect-only.md describes for task 1.
#  2. It keeps the design dep-agent's pilot used: dep-agent denied web fetches to every arm.
#
# The arm is the only variable, so the policy is identical for every arm of every step. It is a
# separate file selected per arm (webGuardFile), so the guards earlier tasks recorded stay
# byte-identical.
#
# NOT blocked, on purpose: commands run through Bash or PowerShell. `dotnet restore` has to reach
# nuget.org on every arm, and the build needs it. A `curl` to a docs site is therefore possible;
# the transcript audit is the backstop for that, as it is on every other task.
#
# Claude Code only: it is a PreToolUse hook. On opencode it is not enforced
# (knowledge/opencode-web-guard-detect-only.md).

$raw = [Console]::In.ReadToEnd()
try { $evt = $raw | ConvertFrom-Json } catch { exit 0 }

if ($evt.tool_name -ne 'WebSearch' -and $evt.tool_name -ne 'WebFetch') { exit 0 }

$decision = @{
    hookSpecificOutput = @{
        hookEventName            = 'PreToolUse'
        permissionDecision       = 'deny'
        permissionDecisionReason = 'Web lookups are not available in this workspace. Use only the reference provided inside the workspace.'
    }
}
Write-Output ($decision | ConvertTo-Json -Depth 5 -Compress)
exit 0
