---
name: azure
description: Azure CLI and SDK hygiene. Use when running az commands, provisioning Azure resources, scripting Azure automation, or handling Azure credentials and service principals.
---

# Azure

## Process

### 1. State the action

Before proposing a command, tell the user what it will do.
Done: the user has heard the action in plain language.

### 2. Prefer repeatable `az`

Script with Azure CLI; use `--output json` when parsing. Parameterize resource names, locations, and credentials. Check for existing resources (`az resource show`, `az group exists`) before create. Organize by resource group; tag for cost tracking; remove unused resources when the task includes cleanup.
Done: the proposed commands are parameterized, existence-checked where create is involved, and scoped to a resource group.

### 3. Keep credentials out of source

Use environment variables or Azure Key Vault. Prefer service principals for CI/CD over personal accounts. Check exit codes and handle failures in scripts.
Done: no secrets appear in commands or files to commit; automation identity is a service principal when CI is in play.

### 4. Note CLI assumptions

When a script depends on a specific Azure CLI version or a renamed flag, say so beside the command.
Done: version or flag caveats the user needs are stated once next to the command.

## Examples

PostgreSQL database (use `--name`, not deprecated `--database-name`):

```bash
az postgres flexible-server db create \
  --resource-group <group> \
  --server-name <server> \
  --name <db>
```

Firewall rule:

```bash
az postgres flexible-server firewall-rule create \
  --resource-group <group> \
  --server-name <server> \
  --name <firewall-rule-name> \
  --start-ip-address <ip> \
  --end-ip-address <ip>
```

Web app:

```bash
az webapp up --name <app-name> --resource-group <group> --runtime "PYTHON|3.11"
```

## References

- [Azure CLI Documentation](https://docs.microsoft.com/cli/azure/)
- [Azure SDK for Python](https://docs.microsoft.com/python/api/overview/azure/)
- [Azure Resource Manager Templates](https://docs.microsoft.com/azure/azure-resource-manager/templates/)
