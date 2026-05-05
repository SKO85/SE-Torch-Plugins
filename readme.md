# SKO's Space Engineers Torch Plugins

Welcome to SKO's Space Engineers Torch Plugins repository! This collection of plugins is designed to enhance your Space Engineers experience using the Torch server management software.

## Setup referenced assemblies

Run `Setup.bat` to create a junction `_Torch` pointing at your Torch dedicated-server install. Pass the path as the first argument:

```cmd
Setup.bat "C:\path\to\Torch"
```

Or set the `TORCH_PATH` environment variable and run without arguments:

```cmd
set TORCH_PATH=C:\path\to\Torch
Setup.bat
```

This step is crucial for making the required referenced assemblies available in the C# projects used for the plugins.

## Contributing and Proposing Changes

If you'd like to propose fixes or changes to the code, please follow these steps:

1. Create a new branch named `fix/<fix-name>` or `feature/<feature-name>` from the `main` branch.
2. Implement and thoroughly test your changes locally.
3. Submit a Pull Request and notify SKO to review the modifications.
4. To ensure a smooth review process, please keep Pull Requests focused on specific fixes or features rather than introducing large changes. This helps maintain a clear and reviewable codebase.

## Review and Deployment

All Pull Requests will undergo a review by SKO. Once changes are merged into the main branch, a new version will be deployed.

Thank you for contributing! 🚀