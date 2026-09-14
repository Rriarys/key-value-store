# Copilot Instructions

## Architecture
- Follow SOLID, DRY, KISS, and YAGNI principles.
- Prefer composition over inheritance.
- Prefer explicit code over clever code.
- Keep methods focused on a single responsibility.
- Avoid unnecessary layers, wrappers, and abstractions.
- Prefer existing project conventions over generic best practices.
- Match the existing coding style, architecture, naming, and file organization.

## Development Preferences
- Target .NET 10.
- Use the latest stable ASP.NET Core APIs.
- Assume C# 14 language features are available.
- Prefer modern C# language features when they improve readability or express intent more clearly.
- New syntax is encouraged when it remains understandable. If a language feature may be unfamiliar, add a short code comment explaining what it does or what classic syntax it replaces.
- Prefer async/await over synchronous APIs when available.
- Prefer dependency injection over manual service instantiation.
- Prefer early returns over deep nesting.
- Respect nullable reference types.
- Avoid using the null-forgiving operator (!) unless it is actually required.
- If nullable operators (!, ?, ??, ?.) are introduced where they were not used before, explain the reason in the chat response instead of adding comments to the code.
- Use descriptive and meaningful names.
- Variable, parameter, field, property, and method names should be descriptive and normally at least 7 characters long unless a shorter name is conventional (e.g. id, db, ct, ex).
- Prefer LINQ when it improves readability.
- Handle errors explicitly. Do not swallow exceptions.
- Never hardcode secrets, passwords, tokens, or connection strings.
- Prefer built-in ASP.NET Core and .NET features over third-party libraries unless explicitly requested.

## Code Generation Rules
- Use existing APIs only. Do not invent methods, libraries, configuration values, file names, or project structure.
- If required information is missing, ask instead of guessing.
- Keep solutions simple and avoid unnecessary abstractions.
- Avoid unrelated refactoring.
- Modify only what is necessary to complete the requested task.
- Keep changes as small as possible.
- Do not rewrite working code unless explicitly requested.
- Preserve existing user comments.
- Write new code comments in English.
- Explain intent rather than obvious implementation details.
- Comments should briefly describe the algorithm, transformation, expectation, or purpose instead of restating what the code already says.
- Short contextual comments are encouraged where they improve readability (e.g. `// Expected: null`, `// 404`, `// Normalize input`).
- Do not end comment lines with periods.
- When modifying code, show only changed blocks unless full file output is requested.

## Testing
- When adding or changing functionality, consider whether tests should also be added or updated.

## Communication
- Respond in Russian unless explicitly requested otherwise.
- When introducing non-obvious language features or nullable operators, explain the reasoning in the chat response instead of the source code comments.

## Commit Messages
- Follow the Conventional Commits specification.
- Output exactly one line.
- Output only the commit message text.
- Do not include markdown, code blocks, quotes, or explanations.
- Use English only.
- Use imperative mood.
- Use lowercase for type and scope.
- Scope should represent the affected module, component, or folder.
- Do not end the description with a period.

### Format
- type(scope): description

### Allowed Types
- feat
- fix
- docs
- style
- refactor
- test
- chore

### Examples
- feat(auth): add login validation
- fix(database): resolve migration timeout
- refactor(tasks): simplify command parser
- docs(readme): update installation guide