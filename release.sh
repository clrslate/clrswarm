#!/bin/bash

# Semantic Version Release Helper
# Usage: ./release.sh <version>
# Example: ./release.sh v1.2.3

set -e

if [ $# -eq 0 ]; then
    echo "Usage: $0 <version>"
    echo "Example: $0 v1.2.3"
    exit 1
fi

VERSION=$1

# Validate semantic version format
if [[ ! $VERSION =~ ^v[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    echo "Error: Version must be in format v<major>.<minor>.<patch> (e.g., v1.2.3)"
    exit 1
fi

echo "Creating release $VERSION..."

# Check if we're on main branch
CURRENT_BRANCH=$(git branch --show-current)
if [ "$CURRENT_BRANCH" != "main" ]; then
    echo "Warning: You're not on the main branch. Current branch: $CURRENT_BRANCH"
    read -p "Continue anyway? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

# Check for uncommitted changes
if [ -n "$(git status --porcelain)" ]; then
    echo "Error: You have uncommitted changes. Please commit them first."
    git status --short
    exit 1
fi

# Create and push the tag
echo "Creating tag $VERSION..."
git tag -a "$VERSION" -m "Release $VERSION"

echo "Pushing tag to origin..."
git push origin "$VERSION"

echo "✅ Release $VERSION created successfully!"
echo "🚀 GitHub Actions will now build and push the Docker image with tags:"
echo "   - $VERSION"
echo "   - $(echo $VERSION | cut -d. -f1-2)  # Major.Minor"
echo "   - $(echo $VERSION | cut -d. -f1)    # Major"
echo ""
echo "Check the GitHub Actions workflow: https://github.com/clrslate/clrswarm/actions"
