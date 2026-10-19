# Pattern map

Where each caching pattern lives, which module uses it, and how to see it working.

The mechanism is the reusable code in `src/BuildingBlocks/CacheShowcase.BuildingBlocks.Caching/<folder>`. The module
column names the class that uses it. TTLs are short on purpose, so each pattern can be watched within a few minutes.

To follow along, set `K=cacheshowcase:development` and `V="docker-compose exec -T valkey valkey-cli"`. Watch the
`X-Cache` response header, or use `curl -i`.

## Read patterns

| Pattern | Mechanism | Used in | Try it |
|---|---|---|---|
| Cache-aside | `Read/CacheAside` | Catalog `CachedProductReader` (GetProduct, ListProducts, GetProductImage) | `GET /api/catalog/products/5` twice: `MISS`, then `HIT` |
| Read-through | `Read/ReadThrough` | Customers `CustomerLoader` + `CachedCustomerStore` | `GET /api/customers/3` twice: `MISS`, then `HIT`. The call site passes no factory |
| Refresh-ahead | `Read/RefreshAhead` | Pricing `ExchangeRateProvider` | `GET /api/pricing/exchange-rates/usd`, wait 60s, GET again: still `HIT`, the log shows `Background refresh scheduled … (refresh-ahead)`, and `storedAt` in `$V get $K:pricing.exchange-rates:v1:USD` moves forward |
| Cache warming | `Read/Warming` | ReferenceData `CountriesWarmer` | Restart the API: the log shows `Cache warmer reference-data.countries finished`, and the first `GET /api/reference-data/countries` is a `HIT` |
| Memoization | `Read/Memoization` | Pricing `CalculateTaxEndpoint` (`TaxRules.ResolveRate`) | `GET /api/pricing/tax?country=DE&category=books&amount=100` twice. In the metrics, `cache.lookups{cache.pattern=memoization}` turns from miss to hit |
| Id-list caching + batching | `Read/CacheAside` (`GetOrCreateManyAsync`) | Catalog `CachedProductReader.FindIdsAsync` / `GetManyAsync` | `GET /api/catalog/products?category=books&pageSize=3` after reading product 5: `PARTIAL` (page missed, some products hit) |

## Write patterns

| Pattern | Mechanism | Used in | Try it |
|---|---|---|---|
| Write-around + invalidate-on-write | `Write/WriteAround` | Catalog `CachedProductWriter.CreateAsync`, Carts `CachedCartStore.AddItemAsync` | `POST /api/carts/{id}/items`, then GET the cart: `MISS` with the new item |
| Write-through | `Write/WriteThrough` | Customers `CachedCustomerStore.UpdateAsync` | `PUT /api/customers/3`, then GET it: `HIT` with the new values (read-your-writes) |
| Update-on-write (compare-and-set) | `Write/UpdateOnWrite` | Used by write-through in Customers | `$V set $K:customers.customer:v1:3 '{"version":99}' px 60000`, then PUT: the log shows `Versioned write … skipped: the cache already holds version 99` |
| Write-behind | `Write/WriteBehind` | Analytics `ProductViewSink` + `CachedProductViewStore` | `POST /api/analytics/products/3/views` ×5 → `$V hgetall $K:write-behind:analytics.product-views:pending` shows `3 → 5`; within 10s the log shows `flushed`, and `analytics.product_views` has grown by 5 |

## Invalidation patterns

| Pattern | Mechanism | Used in | Try it |
|---|---|---|---|
| TTL-only | `Expiration/Policies` | Analytics trending, ReferenceData L1 copy | Nothing invalidates `analytics.trending`; it is simply never more than 20s old |
| Explicit key delete | `Invalidation/KeyDelete` | Catalog create (deletes the cached "not found" for the new id) | `GET /api/catalog/products/{next id}` → 404 cached; `POST /api/catalog/products`; GET → 200 |
| Generation (namespace) versioning | `Invalidation/Generations` | Catalog list pages | `$V get $K:catalog.product-ids:generation` before and after a POST/PUT; old `…:g0:…` page keys are never read again |
| Tag-based (output cache) | `BuildingBlocks.Http/OutputCaching` | Reports `CatalogOverviewEndpoint` | GET `/api/reports/catalog-overview` twice: the second has an `Age` header. After any product or stock change, the next GET is regenerated |
| Schema versioning | `Keys/CacheKey` | Every `*CacheKeys` class (`…:v1:…`) | Bump a schema version to `2`: old entries are ignored and expire |
| Immutable (content-addressed) keys | `Keys/CacheKey` | Catalog `GetProductImageEndpoint` | Update a product's name: its `imageUrl` changes, and the old image stays valid. `Cache-Control: … immutable` |
| Event-driven | FastEndpoints events + `*.Contracts` | Catalog/Inventory publish → Reports `EvictCatalogOverviewOn*` | As for tag-based: Catalog and Inventory never reference Reports |
| Change capture (CDC) | `BuildingBlocks.Persistence/ChangeCapture` | Inventory `StockChangeListener` | `docker-compose exec postgres psql -U cacheshowcase -c "update inventory.stock set quantity = 777 where product_id = 12"`, then `GET /api/inventory/12`: `MISS` with `777`, and the report is regenerated |
| Pub/sub broadcast | `Invalidation/Broadcast` | ReferenceData `CachedCountryCatalog.UpdateAsync` | Run a second instance on another port (see the README), read countries on both (`HIT`), then PUT a country on one: the other's next read is a `MISS` with the new data |
| Leases | `Invalidation/Leases` | Inventory `CachedStockStore` | `$V set $K:inventory.stock:v1:11:lease other px 3000`, `$V del $K:inventory.stock:v1:11`, then GET: it waits about 1s for the "other" holder, then returns `BYPASS`. A write during a load logs `Lease … was revoked` |
| Delayed double delete | `Invalidation/DoubleDelete` | Catalog `CachedProductWriter.UpdateAsync` | `PUT /api/catalog/products/5`: the log shows `double-delete invalidated …:5` twice, 500 ms apart |

## Expiration patterns

| Pattern | Mechanism | Used in | Try it |
|---|---|---|---|
| Absolute TTL per namespace | `Expiration/Policies` | All modules (`AddCacheNamespace`) | `$V pttl $K:catalog.product:v1:5` |
| TTL jitter | `Expiration/Jitter` | Every distributed write | PTTLs of entries written together differ by up to 10%, never above the configured TTL |
| Negative caching | `Expiration/Negative` | Catalog products, Customers, Pricing rates, Inventory | `GET /api/catalog/products/9999` twice: 404 `MISS`, then 404 `HIT`. `$V get $K:catalog.product:v1:9999` returns `null` |
| Sliding expiration | `Expiration/Sliding` | Carts `CachedCartStore` | GET a cart, then `$V pttl $K:carts.cart:v1:1111…`; wait, GET again: the PTTL is back to about 120000 |
| Soft / hard TTL (stale-while-revalidate, stale-if-error) | `Expiration/SoftHard` | Pricing `CurrencyCatalog` | `GET /api/pricing/currencies`, wait 30s, GET: `STALE`, then `HIT`. With the provider unreachable it stays `STALE` instead of failing |
| Probabilistic early expiration (XFetch) | `Expiration/ProbabilisticEarly` | Analytics `CachedProductViewStore.GetTrendingAsync` | Call `/api/analytics/trending` in a loop: the log shows `Probabilistic early recompute … 00:00:00.38 before expiry` |
| Upstream-derived TTL | `Expiration/Upstream` | Pricing `ExchangeRateProvider`, `CurrencyCatalog` | The entry's TTL never exceeds Frankfurter's `max-age` minus `Age` |

## Cross-cutting

| Concern | Where | Try it |
|---|---|---|
| Stampede protection (per process) | `Internal/SingleFlight` | 50 concurrent first requests for one product: Postgres logs 1 query |
| Stampede protection (cross-instance) | `Invalidation/Leases` | 50 concurrent first requests for one stock level: 1 query |
| Fail open | `Stores/Distributed/ValkeyStore` | `docker-compose stop valkey`: every endpoint answers in milliseconds with `BYPASS`, the L1 country list still `HIT`s, and write-behind writes straight to the database |
| Kill switch | `CachingOptions.Enabled` | Set it to `false` in `appsettings.json` while running: `BYPASS` everywhere, no restart |
| Two-level (near) cache | `Stores/TwoLevel` + `Stores/InMemory` | Countries are served from process memory, without touching Valkey |
| HTTP caching defaults | `BuildingBlocks.Http/Headers` | Writes, errors and unmarked responses carry `Cache-Control: no-store`; 5xx always does |
