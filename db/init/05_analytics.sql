-- Analytics module.
create schema analytics;

create table analytics.product_views (
    product_id int primary key,
    views      bigint      not null default 0,
    updated_at timestamptz not null default now()
);

insert into analytics.product_views (product_id, views)
select n, (n * 97) % 1000 from generate_series(1, 200) as n;
