-- Inventory module. Product ids are not foreign keys: modules don't share tables or constraints.
create schema inventory;

create table inventory.stock (
    product_id int primary key,
    quantity   int         not null check (quantity >= 0),
    updated_at timestamptz not null default now()
);

-- Change capture for cache invalidation: every insert or update notifies listeners with the product id,
-- whoever made the change (the API, another system, or manual SQL).
create function inventory.notify_stock_changed() returns trigger
language plpgsql as $$
begin
    perform pg_notify('inventory_stock_changed', new.product_id::text);
    return new;
end;
$$;

create trigger stock_changed
after insert or update on inventory.stock
for each row execute function inventory.notify_stock_changed();

insert into inventory.stock (product_id, quantity)
select n, (n * 7) % 120 from generate_series(1, 200) as n;
