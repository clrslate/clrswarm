# ClrSlate Swarm Helm Chart

A minimal Helm chart for deploying ClrSlate Swarm - an MCP (Model Context Protocol) Gateway.

## Overview

This Helm chart provides a simple way to deploy ClrSlate Swarm to Kubernetes with configurable MCP servers. The MCP configuration is mounted as a ConfigMap, allowing you to easily configure different MCP servers without rebuilding the container image.

## Quick Start

```bash
# Install with default values
helm install clrslate-swarm ./helm

# Install with custom values
helm install clrslate-swarm ./helm -f custom-values.yaml

# Upgrade deployment
helm upgrade clrslate-swarm ./helm -f custom-values.yaml
```

## Configuration

### MCP Servers

The chart allows you to configure multiple MCP servers via the `mcpServers` section in values.yaml:

```yaml
mcpServers:
  memory:
    type: "stdio"
    command: "npx"
    args:
      - "-y"
      - "@modelcontextprotocol/server-memory"
    env:
      NODE_ENV: "production"
  
  filesystem:
    type: "stdio"
    command: "npx"
    args:
      - "-y"
      - "@modelcontextprotocol/server-filesystem"
      - "/allowed/path"
  
  external-api:
    type: "http"
    url: "https://api.example.com/mcp"
    headers:
      Authorization: "Bearer token"
    name: "External API"
```

### Key Configuration Options

| Parameter | Description | Default |
|-----------|-------------|---------|
| `image.repository` | Container image repository | `clrslatepublic.azurecr.io/containers/clrswarm` |
| `image.tag` | Container image tag | `latest` |
| `replicaCount` | Number of replicas | `1` |
| `service.type` | Kubernetes service type | `ClusterIP` |
| `service.port` | Service port | `80` |
| `ingress.enabled` | Enable ingress | `false` |
| `mcpServers` | MCP servers configuration | See values.yaml |
| `app.environment` | ASP.NET Core environment | `Production` |
| `app.logLevel` | Application log level | `Information` |

## Sample Configurations

The `samples/` directory contains example configurations:

- **`helm-basic-memory/`**: Basic setup with memory server (similar to docker-compose sample)
- **`helm-development/`**: Development-optimized configuration with debug logging
- **`helm-production/`**: Production-ready setup with multiple servers, ingress, and autoscaling

## MCP Server Types

### STDIO Servers
For MCP servers that communicate via standard input/output:

```yaml
mcpServers:
  my-server:
    type: "stdio"
    command: "npx"
    args:
      - "-y"
      - "@modelcontextprotocol/server-package"
    env:
      NODE_ENV: "production"
```

### HTTP/SSE Servers
For MCP servers accessible via HTTP:

```yaml
mcpServers:
  my-http-server:
    type: "http"
    url: "https://api.example.com/mcp"
    headers:
      Authorization: "Bearer your-token"
      Content-Type: "application/json"
    name: "My HTTP Server"
```

## Requirements

- Kubernetes 1.16+
- Helm 3.0+

## Values

See [values.yaml](values.yaml) for the full list of configurable parameters.

## Installation Examples

### Basic Installation
```bash
helm install my-swarm ./helm
```

### Development Environment
```bash
helm install dev-swarm ./helm -f samples/helm-development/values.yaml
```

### Production with Custom Domain
```bash
helm install prod-swarm ./helm -f samples/helm-production/values.yaml \
  --set ingress.hosts[0].host=swarm.yourdomain.com \
  --set ingress.tls[0].hosts[0]=swarm.yourdomain.com
```

## Accessing the Application

After installation, follow the instructions in the NOTES to access your application:

```bash
# Get access instructions
helm status clrslate-swarm

# Port forward for local access
kubectl port-forward service/clrslate-swarm 8080:80
```

## Monitoring

View application logs:
```bash
kubectl logs -f deployment/clrslate-swarm
```

Check MCP configuration:
```bash
kubectl get configmap clrslate-swarm-config -o yaml
```

## Uninstallation

```bash
helm uninstall clrslate-swarm
```
