-- Catalog module. Owns the catalog schema; no other module reads it directly.
create schema catalog;

-- Content-addressed: the primary key is the SHA-256 of the content, so a row never changes.
create table catalog.product_images (
    hash         text primary key check (hash ~ '^[0-9a-f]{64}$'),
    content_type text  not null,
    content      bytea not null
);

create table catalog.products (
    id         int generated always as identity primary key,
    name       text          not null,
    category   text          not null check (category in ('books', 'electronics', 'food', 'garden', 'toys')),
    price      numeric(10,2) not null check (price > 0),
    version    int           not null default 1,
    image_hash text          not null references catalog.product_images (hash),
    created_at timestamptz   not null default now(),
    updated_at timestamptz   not null default now()
);

create index products_category_id on catalog.products (category, id);

-- 200 seed products with placeholder SVG images.
create temporary table seed as
select n,
       (array['books', 'electronics', 'food', 'garden', 'toys'])[1 + n % 5] as category,
       (array['Classic', 'Compact', 'Deluxe', 'Eco', 'Handy', 'Mini', 'Pro', 'Smart', 'Ultra', 'Vintage'])[1 + n % 10]
           || ' '
           || (array['Lamp', 'Kettle', 'Novel', 'Planter', 'Speaker', 'Puzzle', 'Notebook', 'Blender', 'Hose', 'Robot'])[1 + (n / 10) % 10]
           || ' ' || n as name,
       round((4.99 + (n * 37) % 300 + (n % 100) / 100.0)::numeric, 2) as price
from generate_series(1, 200) as n;

alter table seed add column content bytea;
update seed set content = convert_to(format(
    '<svg xmlns="http://www.w3.org/2000/svg" width="240" height="160" viewBox="0 0 240 160"><rect width="240" height="160" fill="#64748b"/><text x="120" y="86" font-family="sans-serif" font-size="16" fill="#fff" text-anchor="middle">%s</text></svg>',
    name), 'UTF8');

insert into catalog.product_images (hash, content_type, content)
select encode(sha256(content), 'hex'), 'image/svg+xml', content from seed;

insert into catalog.products (name, category, price, image_hash)
select name, category, price, encode(sha256(content), 'hex') from seed order by n;

drop table seed;
