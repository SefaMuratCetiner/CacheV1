-- ReferenceData module.
create schema reference_data;

create table reference_data.countries (
    code     text primary key check (code ~ '^[A-Z]{2}$'),
    name     text not null,
    currency text not null check (currency ~ '^[A-Z]{3}$')
);

insert into reference_data.countries (code, name, currency) values
    ('DE', 'Germany', 'EUR'),
    ('ES', 'Spain', 'EUR'),
    ('FR', 'France', 'EUR'),
    ('GB', 'United Kingdom', 'GBP'),
    ('IT', 'Italy', 'EUR'),
    ('NL', 'Netherlands', 'EUR'),
    ('TR', 'Türkiye', 'TRY'),
    ('US', 'United States', 'USD');
