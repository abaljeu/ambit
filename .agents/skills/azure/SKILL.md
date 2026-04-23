---
name: azure
description: Best practices and workflows for Azure CLI/SDK 
---

## When to use

Apply this skill whenever you need to work with Azure 

## Best Practices
0. **Explain to the user** Before proposing a command, state its action.
1. **Use Resource Groups:** Organize resources by lifecycle and permissions.
2. **Script with Azure CLI:** Prefer `az` commands for repeatable automation. Use `--output json` for parsing results.
3. **Parameterize Scripts:** Use variables for resource names, locations, and credentials.
4. **Check for Existing Resources:** Use `az resource show` or `az group exists` before creating.
5. **Error Handling:** Check exit codes and handle failures gracefully.
6. **Secure Credentials:** Never hardcode secrets; use environment variables or Azure Key Vault.
7. **Tag Resources:** Apply tags for cost tracking and management.
8. **Use Service Principals for Automation:** Avoid personal accounts in CI/CD.
9. **Cleanup:** Remove unused resources to avoid unnecessary costs.
10. **Document CLI Versions:** Scripts may depend on specific Azure CLI versions.

## Example: Create a PostgreSQL Database
```bash
az postgres flexible-server db create \
  --resource-group <group> \
  --server-name <server> \
  --name <db>
```
Note --database-name is deprecated, and --name is the replacement.

## Firewall
az postgres flexible-server firewall-rule create \
  --resource-group <group> \
  --server-name <server> \
  --name <firewall-rule-name> \
  --start-ip-address <ip> \
  --end-ip-address <ip>

## Example: Deploy Web App
```bash
az webapp up --name <app-name> --resource-group <group> --runtime "PYTHON|3.11"
```

## References
- [Azure CLI Documentation](https://docs.microsoft.com/cli/azure/)
- [Azure SDK for Python](https://docs.microsoft.com/python/api/overview/azure/)
- [Azure Resource Manager Templates](https://docs.microsoft.com/azure/azure-resource-manager/templates/)
