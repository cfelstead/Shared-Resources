# Multi-Tenant Azure Bicep

A sample multi-tenant SaaS deployment on Azure, built to demonstrate Infrastructure as Code with Bicep and a GitHub Actions pipeline that deploys per-customer environments from a single template.

It shows how to:
- define infrastructure in Bicep, parameterized by both customer and environment,
- deploy from GitHub Actions using OIDC (no client secret),
- create one isolated resource group per customer from a single deployment,
- gate infrastructure changes behind a `what-if` plan on pull request before they ever reach `main`.

The deployment is driven by:
- `Infra/main.bicep` (subscription-scope orchestrator)
- `Infra/customer-resources.bicep` (per-customer resources)
- `.github/workflows/deploy.yml` / `deploy-test.yml` (production and test pipelines)
- `.github/workflows/validate.yml` (pull request `what-if` gate)

---

## What this sample deploys

For each customer ID in `Infra/main.bicep` (`customerIds` array), the deployment creates a dedicated resource group containing:
1. An App Service Plan
2. An Azure SQL Server and database
3. An Azure Function App (products API)
4. A Web App (Razor Pages front end)

Resources are named `<type>-<environmentName>-<customerId>`, e.g. `func-prod-custalpha` or `app-test-custbeta`, so production and test deployments never collide and every resource name makes its environment and tenant obvious at a glance.

---

## Environments

A single pair of templates (`main.bicep` / `customer-resources.bicep`) serves both environments via an `environmentName` parameter (`prod` or `test`), rather than maintaining parallel copies of each file. `deploy.yml` (triggered on push to `main`) passes `environmentName=prod`; `deploy-test.yml` (triggered on push to `iac-change*` branches) passes `environmentName=test`.

---

## Prerequisites

- Azure subscription
- GitHub repository with this code
- Permission to create app registrations and role assignments in Azure

---

## Azure setup

### 1) Create an App Registration (Microsoft Entra ID)

In Azure Portal:
1. Go to `Microsoft Entra ID` > `App registrations` > `New registration`
2. Name it something like `github-actions-saas-deployer`
3. Register the app

### 2) Add Federated Credential (OIDC)

In the App Registration:
1. Go to `Certificates & secrets` > `Federated credentials` > `Add credential`
2. Scenario: `GitHub Actions deploying Azure resources`
3. Configure:
   - GitHub Organization/User
   - Repository name
   - Entity type: typically `Branch`
   - Branch: `main` (add a second credential for `iac-change*` branches if you want the test pipeline to authenticate too)

### 3) Assign Azure RBAC permissions

Grant the App Registration access at the target scope (subscription recommended for this sample):
1. Go to `Subscriptions` > your subscription > `Access control (IAM)` > `Add role assignment`
2. Role: `Contributor`
3. Assign access to: your App Registration (`github-actions-saas-deployer`)

---

## GitHub setup

In your GitHub repo:
1. Go to `Settings` > `Secrets and variables` > `Actions`
2. Add these repository secrets:
   - `AZURE_CLIENT_ID` = Application (client) ID from App Registration
   - `AZURE_TENANT_ID` = Directory (tenant) ID from Entra tenant
   - `AZURE_SUBSCRIPTION_ID` = Azure subscription ID
   - `AZURE_SQL_ENTRA_ADMIN_LOGIN` = Entra login/UPN used as SQL logical server admin
   - `AZURE_SQL_ENTRA_ADMIN_OBJECT_ID` = Entra object ID of that SQL admin principal

---

## How deployment works

- `validate.yml` runs on every pull request that touches `Infra/**`, targeting `main`. It performs an `az deployment sub what-if` for both the `prod` and `test` parameterizations, so infrastructure drift is visible in the PR before anything merges.
- `deploy.yml` logs in with `azure/login@v2` using OIDC, deploys at subscription scope with `azure/arm-deploy@v2`, then publishes and deploys the Function and Web apps. Triggered on push to `main`.
- `deploy-test.yml` does the same against the `test` environment. Triggered on push to `iac-change*` branches.

---

## Customization

- Add/remove customers in `Infra/main.bicep`:
  - `param customerIds array = [ ... ]`
- Change deployment region in a workflow:
  - `DEPLOYMENT_LOCATION` environment variable
- Change resource group naming prefix:
  - `resourceGroupPrefix` parameter in `Infra/main.bicep`

## Grant database access to Function managed identity (one-time)

After first deployment, grant each Function App identity access to its database.

For each customer database, run as SQL Entra admin:

`CREATE USER [<function-app-name>] FROM EXTERNAL PROVIDER;`

`ALTER ROLE db_datareader ADD MEMBER [<function-app-name>];`

`ALTER ROLE db_datawriter ADD MEMBER [<function-app-name>];`

Example function app name pattern:

`func-<environmentName>-<customerId>`

(This step runs automatically as part of both deploy workflows; it's listed here for reference or manual runs.)

---

## Run locally (optional)

You can also deploy from the CLI:

`az deployment sub create --location <region> --template-file Infra/main.bicep --parameters environmentName=test`

Example:

`az deployment sub create --location eastus --template-file Infra/main.bicep --parameters environmentName=test`

---

## Notes on this sample's tradeoffs

A few choices here are deliberately simplified for a sample and would need revisiting for production use:

- SQL Server has `publicNetworkAccess: 'Enabled'` with a firewall rule allowing Azure services; a production setup would likely use private endpoints instead.
- The Function App's storage connection string is built from an account key retrieved via `listKeys()`. A production setup would prefer a managed-identity-based connection or a Key Vault reference rather than embedding the key in app settings.
