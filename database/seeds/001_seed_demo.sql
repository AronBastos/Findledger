-- Script de seed para dados de demonstração
-- Execute após rodar as migrations: dotnet ef database update

INSERT INTO accounts (id, owner_name, name, type, balance, created_at)
VALUES
  ('a1000000-0000-0000-0000-000000000001', 'demo-user', 'Conta Corrente',  'Checking', 4700.00, now() - interval '30 days'),
  ('a1000000-0000-0000-0000-000000000002', 'demo-user', 'Reserva de Emergência', 'Savings', 12000.00, now() - interval '60 days'),
  ('a1000000-0000-0000-0000-000000000003', 'demo-user', 'Investimentos',    'Savings', 8500.00, now() - interval '90 days')
ON CONFLICT (id) DO NOTHING;

INSERT INTO transactions (id, account_id, type, amount, description, created_at)
VALUES
  (gen_random_uuid(), 'a1000000-0000-0000-0000-000000000001', 'Deposit',    5000.00, 'Salário',         now() - interval '25 days'),
  (gen_random_uuid(), 'a1000000-0000-0000-0000-000000000001', 'Withdrawal',  300.00, 'Aluguel',         now() - interval '20 days'),
  (gen_random_uuid(), 'a1000000-0000-0000-0000-000000000001', 'TransferDebit', 1000.00, 'Reserva mensal', now() - interval '15 days'),
  (gen_random_uuid(), 'a1000000-0000-0000-0000-000000000002', 'TransferCredit', 1000.00, 'Reserva mensal', now() - interval '15 days'),
  (gen_random_uuid(), 'a1000000-0000-0000-0000-000000000001', 'Deposit',     200.00, 'Freelance',       now() - interval '5 days'),
  (gen_random_uuid(), 'a1000000-0000-0000-0000-000000000001', 'Withdrawal',  200.00, 'Supermercado',    now() - interval '3 days')
ON CONFLICT DO NOTHING;
