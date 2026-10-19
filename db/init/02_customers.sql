-- Customers module.
create schema customers;

create table customers.customers (
    id         int generated always as identity primary key,
    name       text        not null,
    email      text        not null unique,
    version    bigint      not null default 1,
    updated_at timestamptz not null default now()
);

insert into customers.customers (name, email)
select 'Customer ' || n, 'customer' || n || '@example.com'
from generate_series(1, 50) as n;
