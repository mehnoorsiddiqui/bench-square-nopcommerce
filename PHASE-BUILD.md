# Task — Upgrade ApimaticBench.Square from 1.2.0 to 3.0.0

You are upgrading the **ApimaticBench.Square** dependency of this repository from version **1.2.0** to
**3.0.0**.

Change the package reference in `src/Plugins/Nop.Plugin.Payments.Square/Nop.Plugin.Payments.Square.csproj` to exactly **3.0.0**. Do **not** change any other
dependency version. Your job is to make the code work correctly against ApimaticBench.Square 3.0.0: it
must build, its tests must pass, and every use of ApimaticBench.Square must match the 3.0.0 API.

---

## Your reference for ApimaticBench.Square 3.0.0

- **No documentation, plugin or spec is provided.** You have the code, the build, and the
  ApimaticBench.Square 3.0.0 package that the build restores.
- Work out how ApimaticBench.Square 3.0.0 is meant to be used from what is available to you **before**
  editing code.
- Check each use of ApimaticBench.Square against what you can establish about ApimaticBench.Square 3.0.0.

---

## What else you have to work with

- **The existing code.** It was written against 1.2.0, so it shows how the old API was used.
- **The build and tests** (`dotnet build src/Plugins/Nop.Plugin.Payments.Square/Nop.Plugin.Payments.Square.csproj`). They catch types and methods that were removed or
  renamed. They do not catch changes that still compile, such as a changed default, a new or
  removed enum value, a renamed wire field, or a parameter that became required but has a default.

There are no release notes for this upgrade. That does not mean nothing changed.

## How to work

1. Use the reference described above **before** editing code. Do not rely on memory of
   ApimaticBench.Square: what you remember may be from another version.
2. Find every place the code uses ApimaticBench.Square: client setup, configuration and authentication, each
   call, the request and response models it builds or reads, enums, and error handling.
3. Check each of those uses against the reference above: names, parameters and which are required,
   model fields, enum values, return types, defaults, and the errors it can throw. Do this for
   every use, including the ones that already compile.
4. Change what no longer matches. Keep changes as small as possible and keep behaviour the same.
   If 3.0.0 changes behaviour in a way the code cannot keep, say so in your summary rather than
   guessing.
5. Run `dotnet build src/Plugins/Nop.Plugin.Payments.Square/Nop.Plugin.Payments.Square.csproj` and fix what fails.
6. Change tests only where they call the part of the ApimaticBench.Square API that changed. Never delete or
   skip a test to make it pass.

A clean build does not mean you are done: finish step 3 for every use first. Finding that nothing
needed to change is a valid result, as long as you checked.

Do not change files outside this repository.

## When you are done

End with a short summary (at most 10 bullet points) of what you changed and why, which ApimaticBench.Square
uses you checked and left unchanged, and anything you could not fix or are unsure about.

---

## Constraints

- **Report a gap only when it is genuinely a gap.** Stop and report when the reference you were
  given does not cover something this upgrade requires. A design decision being hard,
  open-ended, or left to your judgment is **not** a gap — decide it and proceed.
- **You are running headless — there is no one to answer you.** Work until the upgrade is
  complete. Never hand back, never end with a question, and never defer remaining work to the
  user: decide and proceed.

