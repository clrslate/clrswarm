# Development Environment Sample

This sample is optimized for local development and testing with:

- Single replica for simplicity
- NodePort service for easy local access
- Debug logging enabled
- Minimal resource requirements
- Faster health check intervals
- Development-friendly configurations

## Quick Start

```bash
# Install for development
helm install clrslate-swarm-dev ../../helm -f values.yaml

# Access via NodePort (if using minikube)
minikube service clrslate-swarm-dev

# Or port forward
kubectl port-forward service/clrslate-swarm-dev 8080:80

# View debug logs
kubectl logs -f deployment/clrslate-swarm-dev
```

## Development Features

- **Debug Logging**: All MCP servers run with `DEBUG=*`
- **Fast Iteration**: `pullPolicy: Always` ensures latest image
- **Local Access**: NodePort service on port 30080
- **Minimal Resources**: Low CPU/memory for development machines
- **Quick Health Checks**: Faster probe intervals for rapid feedback

## Testing MCP Configuration

```bash
# Check the configuration
kubectl get configmap clrslate-swarm-dev-config -o jsonpath='{.data.appsettings\.json}' | jq

# Update configuration and restart
helm upgrade clrslate-swarm-dev ../../helm -f values.yaml
```
