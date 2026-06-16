# Christian Jewelry — Database & API Spec

PostgreSQL база даних + Swagger UI для інтернет-магазину християнських прикрас.

## Швидкий старт

```bash
git clone https://github.com/YOUR_USERNAME/christian-jewelry-db.git
cd christian-jewelry-db
cp .env.example .env          # заповніть пароль
docker-compose up -d
```

| Сервіс     | URL                        |
|------------|----------------------------|
| PostgreSQL | `localhost:5432`           |
| Swagger UI | http://localhost:8080      |

## Структура

```
├── docker-compose.yml
├── Dockerfile
├── .env.example
├── init/
│   ├── 01_schema.sql    # таблиці
│   ├── 02_seed.sql      # початкові дані
│   └── 03_triggers.sql  # тригери
└── docs/
    └── openapi.yaml     # Swagger специфікація
```

## Корисні команди

```bash
# Перезапустити з чистою БД
docker-compose down -v && docker-compose up -d

# Підключитись до БД
psql -h localhost -U jewelry_admin -d christian_jewelry

# Логи
docker-compose logs -f postgres
```
