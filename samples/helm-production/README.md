# Production Deployment Sample

This sample demonstrates a production-ready deployment with:

- Multiple replicas for high availability
- LoadBalancer service type
- Ingress with TLS
- Multiple MCP servers (memory, filesystem, and HTTP-based)
- Resource limits and requests
- Horizontal Pod Autoscaler
- Security contexts

## Prerequisites

- Kubernetes cluster with ingress controller (nginx)
- cert-manager for TLS certificates
- LoadBalancer support or external IP management

## Deployment

```bash
# Install with production configuration
helm install clrslate-swarm-prod ../../helm -f values.yaml -n production --create-namespace

# Monitor the deployment
kubectl get pods -n production -w

# Check ingress
kubectl get ingress -n production

# View auto-scaling status
kubectl get hpa -n production
```

## Configuration Notes

- Adjust the `ingress.hosts` and `tls` sections for your domain
- Update authorization tokens for external MCP servers
- Modify resource limits based on your cluster capacity
- Review security contexts for your environment requirements
