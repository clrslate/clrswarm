# GitHub Actions Setup for Azure Container Registry

This repository includes GitHub Actions workflows to automatically build and publish Docker images to Azure Container Registry (ACR).

## Workflows

### 1. `docker-publish.yml` - Username/Password Authentication
Uses ACR username and password for authentication.

### 2. `docker-publish-azure-cli.yml` - Azure CLI with OIDC Authentication (Recommended)
Uses Azure CLI with OpenID Connect (OIDC) authentication for more secure, passwordless authentication.

## Setup Instructions

### Option 1: Username/Password Authentication

1. **Create an Azure Container Registry** if you don't have one:
   ```bash
   az acr create --name your-acr-name --resource-group your-resource-group --sku Basic --admin-enabled true
   ```

2. **Get ACR credentials**:
   ```bash
   az acr credential show --name your-acr-name
   ```

3. **Configure GitHub Secrets**:
   - Go to your GitHub repository → Settings → Secrets and variables → Actions
   - Add the following secrets:
     - `ACR_USERNAME`: The username from step 2
     - `ACR_PASSWORD`: The password from step 2

4. **Update the workflow file**:
   - Edit `.github/workflows/docker-publish.yml`
   - Replace `your-acr-name.azurecr.io` with your actual ACR name

### Option 2: Azure CLI with OIDC Authentication (Recommended)

This method is more secure as it doesn't require storing passwords in GitHub secrets.

1. **Create an Azure Container Registry** if you don't have one:
   ```bash
   az acr create --name your-acr-name --resource-group your-resource-group --sku Basic
   ```

2. **Create a Service Principal and configure OIDC**:
   ```bash
   # Create a service principal
   az ad sp create-for-rbac --name "github-actions-acr" --role acrpush --scopes /subscriptions/{subscription-id}/resourceGroups/{resource-group}/providers/Microsoft.ContainerRegistry/registries/{acr-name} --json-auth
   
   # Get the application ID
   az ad sp list --display-name "github-actions-acr" --query "[].{appId:appId}" --output table
   ```

3. **Configure GitHub OIDC**:
   ```bash
   # Add federated credentials for your GitHub repository
   az ad app federated-credential create --id {app-id} --parameters '{
     "name": "github-actions",
     "issuer": "https://token.actions.githubusercontent.com",
     "subject": "repo:{your-github-username}/{your-repo-name}:ref:refs/heads/main",
     "audiences": ["api://AzureADTokenExchange"]
   }'
   ```

4. **Configure GitHub Secrets**:
   - Go to your GitHub repository → Settings → Secrets and variables → Actions
   - Add the following secrets:
     - `AZURE_CLIENT_ID`: The application ID from step 2
     - `AZURE_TENANT_ID`: Your Azure tenant ID
     - `AZURE_SUBSCRIPTION_ID`: Your Azure subscription ID

5. **Update the workflow file**:
   - Edit `.github/workflows/docker-publish-azure-cli.yml`
   - Replace the following placeholders:
     - `your-acr-name.azurecr.io` with your actual ACR name
     - `your-resource-group` with your resource group name
     - `your-acr-name` with your ACR name

## Workflow Features

Both workflows include:

- **Multi-platform builds**: Builds for both `linux/amd64` and `linux/arm64`
- **Smart tagging**: 
  - Branch names for pushes to branches
  - Semantic versioning for tags (v1.0.0, v1.0, v1)
  - SHA-based tags for unique identification
- **Caching**: Uses GitHub Actions cache to speed up builds
- **Pull Request safety**: Only builds images for PRs, doesn't push to registry
- **Security**: Only pushes images when not in a pull request

## Triggering the Workflow

The workflows will run automatically when:
- Code is pushed to the `main` branch
- A tag starting with `v` is pushed (e.g., `v1.0.0`)
- A pull request is opened against `main` (build only, no push)

## Manual Triggering

You can also trigger the workflow manually from the GitHub Actions tab in your repository.

## Viewing Results

After a successful run, you can:
1. Check the Actions tab for build logs
2. View your images in the Azure Container Registry
3. Pull and run your image:
   ```bash
   docker pull your-acr-name.azurecr.io/clrslate-swarm:main
   docker run -p 8080:8080 your-acr-name.azurecr.io/clrslate-swarm:main
   ```

## Troubleshooting

- **Authentication errors**: Double-check your secrets and ACR configuration
- **Build errors**: Check the Dockerfile path and build context
- **Permission errors**: Ensure your service principal has the correct permissions (AcrPush role)
