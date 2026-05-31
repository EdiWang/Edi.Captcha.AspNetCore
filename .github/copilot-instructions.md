# Copilot Instructions for Edi.Captcha.AspNetCore

This repository contains `Edi.Captcha`, an ASP.NET Core captcha library, plus a sample MVC app and NUnit tests. Treat the package project as a public NuGet library: keep changes small, preserve public API compatibility unless the request explicitly calls for a breaking change, and update README/sample usage when behavior or setup changes.

## Project Layout

- `src/Edi.Captcha/` is the reusable library.
- `src/Edi.Captcha.SampleApp/` demonstrates session-based, stateless, and shared-key stateless captcha flows.
- `src/Edi.Captcha.Tests/` contains NUnit tests with Moq where dependencies need to be isolated.
- `src/Edi.Captcha.slnx` is the solution file.

## Target Frameworks and Commands

- The library targets `net8.0;net10.0`; do not use APIs unavailable to either target.
- The sample app and test project target `net10.0`.
- Prefer validating library changes with:
  - `dotnet build src/Edi.Captcha.slnx`
  - `dotnet test src/Edi.Captcha.Tests/Edi.Captcha.Tests.csproj`
- The VS Code build task builds `src/Edi.Captcha.SampleApp/Edi.Captcha.SampleApp.csproj`; use it when checking sample app compilation.

## C# Style

- Use file-scoped namespaces under `namespace Edi.Captcha;` or `namespace Edi.Captcha.Tests;`.
- Follow the existing concise C# style: expression-bodied members are fine for simple forwarding methods, and modern collection expressions like `[]` are already used.
- Keep public option classes simple mutable POCOs with sensible defaults.
- Keep comments sparse and useful. Existing comments explain non-obvious image, crypto, or validation behavior.
- Do not add copyright/license headers.

## Captcha and Security Rules

- Generate captcha codes through `SecureCaptchaGenerator.GenerateSecureCaptchaCode` or equivalent cryptographically secure logic. Do not replace this with `Random` for code generation.
- `Random.Shared` is currently used for visual noise and layout only; keep it out of security decisions.
- Preserve default captcha letters unless intentionally changing product behavior: `2346789ABCDGHKMNPRUVWXYZ`.
- Preserve the 1-32 code length validation behavior used by letter captcha implementations.
- For session-based captcha, keep session null checks explicit and continue removing the stored code by default after validation.
- For stateless captcha, keep token validation fail-closed: invalid, expired, malformed, or corrupted tokens should return `false`, not throw to callers.
- For Data Protection based stateless captcha, use the existing protector purpose string `Edi.Captcha.Stateless` unless a migration strategy is part of the change.
- For shared-key stateless captcha, require a Base64-encoded 256-bit key and do not log, expose, or hard-code real keys.
- Respect `BlockedCodes` and keep the retry guard to avoid infinite loops when configured letters cannot produce an allowed code.

## Image Generation Rules

- The library uses a custom in-repo image renderer and PNG encoder. Do not introduce `System.Drawing`, SkiaSharp, ImageSharp, or other image dependencies unless explicitly requested.
- Generated images should remain valid PNG byte arrays with correct IHDR dimensions.
- Keep image rendering cross-platform and allocation-conscious; dispose `CaptchaImage`, streams, crypto objects, and other disposable resources with `using`/`using var`.

## Dependency Injection and Public API

- Keep DI extension methods in `CaptchaServiceCollectionExtensions` discoverable and named around the supported flows: session-based, stateless, and shared-key stateless captcha.
- Preserve service lifetimes unless there is a clear reason to change them. Current patterns use transient captcha services and singleton option instances for stateless flows.
- If adding options, wire them through DI registration, implementation, README examples, and tests.
- If adding a public type or member, consider whether it needs XML comments, README coverage, and sample app usage.

## Tests

- Use NUnit attributes such as `[TestFixture]`, `[SetUp]`, and `[Test]`.
- Use `Assert.That(...)` style assertions.
- Name tests with the existing `MethodOrScenario_WithCondition_ExpectedResult` pattern.
- Prefer focused unit tests for validation, edge cases, security boundaries, token expiration/corruption, image PNG structure, and DI registration behavior.
- For randomness-sensitive code, assert invariants rather than exact outputs. Avoid flaky expectations based on a single random sample.
- Use Moq for framework dependencies such as `IDataProtectionProvider`, `IDataProtector`, or `ISession` when that keeps tests focused.

## Documentation and Samples

- README is the primary user-facing documentation for install and usage examples.
- Keep README snippets aligned with the actual public API and default option values.
- When changing controller/view integration behavior, update the sample app so users have a complete working example.
- Avoid broad unrelated refactors in the sample app; it exists to demonstrate the package.

## Packaging

- The library project generates a NuGet package on build and includes the root `README.md` and `img/edi-logo-blue.png` as package assets.
- Do not change package metadata, target frameworks, package icon, or version unless explicitly requested.