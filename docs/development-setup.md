# Development Setup

## One-time Setup (Required for all developers)

### 1. Install pre-commit
```bash
# Using pip
pip install pre-commit

# Or using conda
conda install -c conda-forge pre-commit
```

### 2. Install the git hooks
```bash
# Run this in the project root
pre-commit install
```

That's it! 🎉

## How It Works

- When you run `git commit`, the license headers are automatically checked and fixed
- If headers are added/fixed, the commit will be blocked with a helpful message
- Just run `git add .` and `git commit` again after the auto-fix

## Example Workflow

```bash
git add .
git commit -m "Add new feature"
# License Eye.................................................Failed
# - files were modified by this hook

git add .  # Add the fixed files
git commit -m "Add new feature"  
# License Eye.................................................Passed
# ✅ Commit successful!
```

## Troubleshooting

If you get Docker errors, make sure Docker is running on your machine.
