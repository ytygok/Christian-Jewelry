# Christian Jewelry — Database & API Spec

PostgreSQL база даних + Swagger UI для інтернет-магазину християнських прикрас.

Інтернет-магазин християнських прикрас: фронтенд + .NET backend + PostgreSQL + Swagger.

## Стек

| Компонент | Технологія |
|---|---|
| Frontend | HTML / CSS / Vanilla JS |
| Backend | .NET 8, ASP.NET Core |
| База даних | PostgreSQL 16 |
| ORM | Entity Framework Core 8 |
| Авторизація | JWT Bearer |
| Документація | Swagger / OpenAPI |
| Контейнери | Docker, Docker Compose |

## Швидкий старт

### 1. Клонувати репозиторій
```bash
git clone https://github.com/YOUR_USERNAME/Christian-Jewelry.git
cd Christian-Jewelry
```

### 2. Запустити PostgreSQL через Docker
```bash
cp .env.example .env
docker-compose up -d
```

### 3. Завантажити схему БД
```powershell
Get-Content ".\backend\init\01_schema.sql" | docker exec -i christian_jewelry_db psql -U cj_user -d christian_jewelry
Get-Content ".\backend\init\02_seed.sql"   | docker exec -i christian_jewelry_db psql -U cj_user -d christian_jewelry
Get-Content ".\backend\init\03_trigger.sql"| docker exec -i christian_jewelry_db psql -U cj_user -d christian_jewelry
```

### 4. Налаштувати appsettings.json
```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=christian_jewelry;Username=cj_user;Password=cj_pass;"
  },
  "Jwt": {
    "Secret": "ВАШ_СЕКРЕТНИЙ_КЛЮЧ_МІН_32_СИМВОЛИ",
    "Issuer": "ChristianJewelry",
    "Audience": "ChristianJewelryClient",
    "ExpiryMinutes": "1440"
  }
}
```

### 5. Запустити backend
```powershell
dotnet run --project Cristianjewelry.csproj
```

### 6. Відкрити сайт
| Сервіс | URL |
|---|---|
| Сайт | http://localhost:5000/#home |
| Swagger API | http://localhost:5000/swagger/index.html#/ |
| PostgreSQL | localhost:5432 |

## Структура проєкту
Christian-Jewelry/
├── backend/
│   ├── init/
│   │   ├── 01_schema.sql       ← схема БД
│   │   ├── 02_seed.sql         ← початкові дані
│   │   └── 03_trigger.sql      ← тригери updated_at
│   └── src/
│       ├── Domain/             ← entities, enums, interfaces
│       ├── Application/        ← DTOs, моделі
│       ├── Infrastructure/     ← EF Core, репозиторії, JWT
│       └── Controllers/        ← API endpoints
├── фронтенд/
│   ├── jav.html                ← головна сторінка
│   ├── jav.css                 ← стилі (dark/light тема)
│   └── jav.js                  ← логіка, підключення до API
├── wwwroot/                    ← статика для .NET (копія фронтенду)
├── docker-compose.yml
├── Dockerfile
├── appsettings.json
└── Cristianjewelry.csproj

## API Endpoints

| Метод | URL | Опис |
|---|---|---|
| POST | `/api/Auth/register` | Реєстрація |
| POST | `/api/Auth/login` | Вхід |
| GET | `/api/Categories` | Всі категорії |
| GET | `/api/Products` | Каталог з фільтрами |
| GET | `/api/Products/{slug}` | Деталі товару |
| GET | `/api/Cart` | Кошик |
| POST | `/api/Orders` | Оформити замовлення |
| POST | `/api/CustomRequests` | Запит "Ваша ідея" |
| POST | `/api/PromoCodes/validate` | Перевірити промокод |

### Admin (потребує JWT токен адміна)
| Метод | URL | Опис |
|---|---|---|
| POST | `/api/Products` | Додати товар |
| DELETE | `/api/Products/{id}` | Видалити товар |
| PUT | `/api/Orders/{id}/status` | Змінити статус замовлення |
| PUT | `/api/Reviews/{id}/approve` | Схвалити відгук |

## Корисні команди

```powershell
# Перезапустити БД з нуля
docker-compose down -v
docker-compose up -d

# Підключити сайт (фронтенд) до бази данних
dotnet run --project Cristianjewelry.csproj

# Підключитись до БД
docker exec -it christian_jewelry_db psql -U cj_user -d christian_jewelry

# Перевірити таблиці
docker exec -it christian_jewelry_db psql -U cj_user -d christian_jewelry -c "\dt"

# Перевірити товари
docker exec -it christian_jewelry_db psql -U cj_user -d christian_jewelry -c "SELECT name, price FROM products;"
```

## Ліцензія

MIT
