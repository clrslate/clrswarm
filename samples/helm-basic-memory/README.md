# Sample deployment commands for basic memory server

# Install the chart with basic memory configuration
helm install clrslate-swarm ../../helm -f values.yaml

# Upgrade the deployment
helm upgrade clrslate-swarm ../../helm -f values.yaml

# Port forward to access locally
kubectl port-forward service/clrslate-swarm 8080:80

# View logs
kubectl logs -f deployment/clrslate-swarm

# Check configuration
kubectl get configmap clrslate-swarm-config -o yaml
