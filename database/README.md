# Sales Forecast System database

## Database strategy

The project uses Database First with versioned SQL migrations.

This approach is intentional because the database owns critical consistency rules that are not represented by the current EF Core model alone:

- Inventory posting stored procedures.
- Append-only inventory ledger triggers.
- Document immutability triggers.
- Forecast horizon constraints.
- Reporting views.
- Transaction-level warehouse locks.
- Restricted permissions for the application database role.

Application entities, properties and database identifiers use English names. Existing Vietnamese tables were migrated in place without deleting their data.

## Deployment

Run:

```powershell
.\database\Deploy.ps1 -Verify
```

The deployment is idempotent. It creates a new database when needed, upgrades an existing Vietnamese schema, rebuilds database operations and optionally runs rollback-based verification.

The current schema version is 6.

## Tables

| Area | Table | Purpose |
|---|---|---|
| Authorization | `Roles` | Application roles |
| Authorization | `Users` | User accounts and BCrypt password hashes |
| Authentication | `LoginSessions` | JWT sessions and revocation state |
| Catalog | `Categories` | Product categories |
| Catalog | `Products` | Product master data, image URL, minimum stock threshold and current sale price |
| Inventory | `Warehouses` | Warehouses |
| Partners | `Customers` | Customers |
| Partners | `Suppliers` | Suppliers |
| Purchasing | `PurchaseOrders` | Purchase order headers |
| Purchasing | `PurchaseOrderItems` | Purchase order lines |
| Sales | `SalesOrders` | Sales order headers |
| Sales | `SalesOrderItems` | Sales order lines |
| Inventory | `InventoryTransactions` | Append-only inventory ledger |
| Forecasting | `ForecastModels` | Forecast model definitions and parameters |
| Forecasting | `ForecastRuns` | Training and forecast horizons, status and metrics |
| Forecasting | `ForecastResults` | Forecast values by product and date |
| Technical | `SchemaVersions` | Applied database versions |

## Database operations

### Views

- `vw_InventoryBalances`: current quantity by warehouse and product.
- `vw_DailySales`: completed sales grouped by date, warehouse and product.
- `vw_SalesOrderTotals`: order totals calculated from line items.

### Stored procedures

- `usp_PostPurchaseOrder`: atomically posts a purchase order and increases inventory.
- `usp_CompleteSalesOrder`: atomically completes a sales order after verifying inventory.

Application code must use these procedures for posting. It must not update document status or insert inventory transactions directly.

## ACID guarantees

### Atomicity

- Posting operations use explicit transactions with `XACT_ABORT ON`.
- `TRY/CATCH` rolls back the entire transaction when any validation or write fails.
- Document status and inventory transactions are committed together.

### Consistency

- Foreign keys protect references.
- Check constraints protect statuses, quantities, prices, forecast ranges and dates.
- Unique indexes make posting idempotent and prevent duplicate ledger sources.
- Posted documents and their items are immutable.
- Inventory transactions are append-only.
- A database trigger rejects any write that would make inventory negative.
- Product minimum stock cannot be negative, and row versions protect concurrent API updates.

### Isolation

- Posting locks the document row with `UPDLOCK` and `HOLDLOCK`.
- `sp_getapplock` serializes inventory posting by warehouse.
- Concurrent completion requests cannot consume the same stock twice.

### Durability

- SQL Server commits document state and ledger rows in the same durable transaction.
- Inventory is derived from the ledger instead of a separately mutable balance column.

## Status values

| Entity | Values |
|---|---|
| `Users` | `Active`, `Locked` |
| `PurchaseOrders` | `Draft`, `Posted`, `Cancelled` |
| `SalesOrders` | `Draft`, `Completed`, `Cancelled` |
| `ForecastRuns` | `Pending`, `Running`, `Completed`, `Failed` |

## Application database role

`SalesForecastApp` can read the schema and edit allowed master or draft data. It cannot write directly to `InventoryTransactions` and can post inventory only through the two stored procedures.

Do not grant `db_owner` or broad `db_datawriter` permissions to the production application user.

## Verification

`003_verify.sql` runs rollback-based database checks for:

- Purchase and sales posting in one atomic flow.
- Idempotent procedure retries.
- Correct inventory and revenue totals.
- Overselling rejection and rollback.
- Completed document immutability.
- Positive quantity constraints.
- Non-negative product minimum stock constraints.
- Append-only inventory history.
- Forecast result horizon validation.

The verification creates no lasting business records, although SQL Server identity values may contain gaps after rolled-back tests.
