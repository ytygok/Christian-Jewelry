# 🗄️ Christian Jewelry — База даних: інструкція

## Що це і навіщо

Зараз сайт зберігає дані в `localStorage` браузера.
Це добре для тестування, але **має обмеження**:
- дані зберігаються тільки в одному браузері
- якщо очистити кеш — все зникне
- різні користувачі не бачать одні й ті самі дані

**Рішення** — підключити справжній сервер з базою даних.
Нижче — покроковий план з готовим кодом.

---

## Стек (що встановити)

| Компонент | Що це | Безкоштовно |
|-----------|-------|-------------|
| **Node.js** | Сервер (JS) | ✅ |
| **SQLite** | База даних (файл на диску) | ✅ |
| **Express** | Веб-фреймворк | ✅ |

SQLite обрано тому що це **один файл** (`database.db`) — не треба окремого сервера БД.

---

## Крок 1 — Встановити Node.js

1. Перейди на https://nodejs.org
2. Завантаж версію **LTS** (рекомендована)
3. Встанови, перевір у терміналі:
```
node --version   # має показати v20.x або вище
npm --version
```

---

## Крок 2 — Структура папок

```
christian-jewelry/
├── server/
│   ├── server.js        ← головний сервер
│   ├── database.js      ← підключення до БД
│   ├── package.json     ← залежності
│   └── database.db      ← файл БД (створюється автоматично)
├── index.html
├── jav.css
└── jav.js
```

---

## Крок 3 — Ініціалізація

Відкрий термінал у папці `server/` і виконай:
```bash
npm install
node server.js
```

Сервер запуститься на http://localhost:3000

---

## Крок 4 — Таблиці бази даних

База автоматично створює три таблиці при першому запуску:

### `users` — облікові записи
```sql
id TEXT PRIMARY KEY
name TEXT
surname TEXT
phone TEXT
email TEXT UNIQUE
password TEXT
role TEXT  -- 'user' або 'admin'
createdAt TEXT
```

### `products` — каталог виробів
```sql
id TEXT PRIMARY KEY
category TEXT   -- rings, earrings, pendants, bracelets, necklaces, brooches, custom
name TEXT
desc TEXT
material TEXT
price REAL
img TEXT
createdAt TEXT
updatedAt TEXT
```

### `orders` — замовлення
```sql
id TEXT PRIMARY KEY
userId TEXT
userName TEXT
userEmail TEXT
userPhone TEXT
product TEXT
material TEXT
style TEXT
extras TEXT
notes TEXT
createdAt TEXT
```

---

## API endpoints (що робить сервер)

```
POST /api/auth/register   — реєстрація
POST /api/auth/login      — вхід
GET  /api/products        — всі вироби
POST /api/products        — додати виріб (admin)
PUT  /api/products/:id    — редагувати виріб (admin)
DELETE /api/products/:id  — видалити виріб (admin)
GET  /api/orders          — всі замовлення (admin)
POST /api/orders          — нове замовлення (user)
GET  /api/users           — всі юзери (admin)
```

---

## Крок 5 — Підключити сайт до сервера

Після запуску `node server.js` відкрий сайт через:
```
http://localhost:3000
```
(не через файл, а через браузер з цією адресою)

Файл `jav.js` вже містить константу `API_URL` —
просто зміни `USE_SERVER = false` на `USE_SERVER = true`.

