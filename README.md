# Power BI Metadata Extractor GitHub Action

This composite GitHub Action extracts metadata from a Power BI workspace using the XMLA endpoint and uploads it as a JSON artifact.

## 🔧 Inputs

| Name            | Description                      | Required |
|-----------------|----------------------------------|----------|
| `workspace_name`| Name of the Power BI workspace   | ✅       |
| `tenant_id`     | Azure AD Tenant ID               | ✅       |
| `client_id`     | Azure AD App Client ID           | ✅       |
| `client_secret` | Azure AD App Client Secret       | ✅       |

## 📦 Outputs

| Name           | Description                     |
|----------------|----------------------------------|
| `artifact-name`| Name of the uploaded metadata artifact |

## 🚀 Example Usage

```yaml
jobs:
  extract-metadata:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: reecetech/pbi-metadata-action@v1
        with:
          workspace_name: 'FinanceWorkspace'
          tenant_id: ${{ secrets.TENANT_ID }}
          client_id: ${{ secrets.CLIENT_ID }}
          client_secret: ${{ secrets.CLIENT_SECRET }}
