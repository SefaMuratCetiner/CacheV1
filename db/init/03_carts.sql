-- Carts module.
create schema carts;

create table carts.cart_items (
    cart_id    uuid        not null,
    product_id int         not null,
    quantity   int         not null check (quantity > 0),
    updated_at timestamptz not null default now(),
    primary key (cart_id, product_id)
);

-- One sample cart.
insert into carts.cart_items (cart_id, product_id, quantity) values
    ('11111111-1111-1111-1111-111111111111', 1, 2),
    ('11111111-1111-1111-1111-111111111111', 7, 1);
