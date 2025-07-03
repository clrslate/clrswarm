# Contributing to CLRSwarm

Thank you for your interest in contributing to CLRSwarm! We welcome contributions from the community and are grateful for your support.

## Code of Conduct

This project adheres to a [Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code.

## How to Contribute

### 1. Fork and Clone

1. Fork the repository on GitHub
2. Clone your fork locally:
   ```bash
   git clone https://github.com/YOUR_USERNAME/clrswarm.git
   cd clrswarm
   ```

### 2. Set Up Development Environment

**Prerequisites:**
- .NET 8.0 SDK or later
- Visual Studio 2022 or VS Code
- Git

**Setup:**
```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test
```

### 3. Making Changes

1. **Create a feature branch:**
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Follow coding standards:**
   - All source files must include the Apache 2.0 license header
   - Follow .NET coding conventions
   - Add unit tests for new functionality
   - Update documentation as needed

3. **License Header Requirement:**
   Every `.cs` file must start with the Apache 2.0 license header. Use the template in `.header-template.txt`.

4. **Commit Guidelines:**
   - Use clear, descriptive commit messages
   - Sign your commits with `git commit -s` (DCO compliance)
   - Keep commits focused on a single change

### 4. Submitting Changes

1. **Push your changes:**
   ```bash
   git push origin feature/your-feature-name
   ```

2. **Create a Pull Request:**
   - Use a clear title and description
   - Reference any related issues
   - Ensure all CI checks pass
   - Sign the Contributor License Agreement (CLA) if prompted

### 5. Pull Request Process

1. **Automated Checks:**
   - License header verification
   - Code formatting
   - Unit tests
   - Dependency license scan

2. **Review Process:**
   - At least one maintainer review required
   - Address any feedback promptly
   - Keep the PR updated with the main branch

3. **Merge:**
   - PRs are merged using squash commits
   - The PR title becomes the commit message

## Development Guidelines

### Code Style

- Follow standard .NET coding conventions
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Keep methods focused and reasonably sized

### Testing

- Write unit tests for new functionality
- Ensure existing tests continue to pass
- Aim for good test coverage
- Use descriptive test method names

### Documentation

- Update README.md if adding new features
- Add XML documentation for public APIs
- Update architectural documentation in `docs/` if needed

## Legal Requirements

### Developer Certificate of Origin (DCO)

All commits must be signed off using `git commit -s`. This certifies that you wrote the code or have the right to submit it under the project's license.

### License Compliance

- All contributions must be compatible with Apache 2.0
- Include proper license headers in all source files
- Declare any third-party dependencies and their licenses

## Getting Help

- **Questions:** Open a discussion in the repository
- **Bugs:** Create an issue with detailed reproduction steps
- **Feature Requests:** Open an issue describing the desired functionality

## Maintainers

Current maintainers:
- ClrSlate Tech labs Private Limited team

For direct contact regarding contributions, please reach out through GitHub issues or discussions.

## Recognition

Contributors will be recognized in:
- NOTICE file (for significant contributions)
- Release notes
- Project documentation

Thank you for contributing to CLRSwarm! 🐝
