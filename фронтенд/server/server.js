// ============================================================
//  server.js — Express сервер для Christian Jewelry
//  Запуск: node server.js  або  npm start
//  Сайт:   http://localhost:3000
// ============================================================

const express = require('express');
const cors    = require('cors');
const path    = require('path');
const { randomUUID } = require('crypto');
const db = require('./database');

const app  = express();
const PORT = process.env.PORT || 3000;

// ── Middleware ───────────────────────────────────────────────
app.use(cors());
app.use(express.json({ limit: '10mb' }));

// Роздаємо статичні файли сайту з батьківської папки
app.use(express.static(path.join(__dirname, '..')));

// ── Helpers ──────────────────────────────────────────────────
function now() { return new Date().toISOString(); }

function requireAdmin(req, res, next) {
  const userId = req.headers['x-user-id'];
  if (!userId) return res.status(401).json({ error: 'Не авторизовано' });
  const user = db.prepare('SELECT * FROM users WHERE id = ?').get(userId);
  if (!user || user.role !== 'admin') return res.status(403).json({ error: 'Доступ заборонено' });
  req.user = user;
  next();
}

function requireAuth(req, res, next) {
  const userId = req.headers['x-user-id'];
  if (!userId) return res.status(401).json({ error: 'Не авторизовано' });
  const user = db.prepare('SELECT * FROM users WHERE id = ?').get(userId);
  if (!user) return res.status(401).json({ error: 'Не авторизовано' });
  req.user = user;
  next();
}

// ════════════════════════════════════════════════════════════
//  AUTH
// ════════════════════════════════════════════════════════════

// POST /api/auth/register
app.post('/api/auth/register', (req, res) => {
  const { name, surname, phone, email, password } = req.body;

  if (!name || !surname || !email || !password)
    return res.status(400).json({ error: 'Заповніть всі обов\'язкові поля' });

  if (password.length < 6)
    return res.status(400).json({ error: 'Пароль має містити мінімум 6 символів' });

  const exists = db.prepare('SELECT id FROM users WHERE email = ?').get(email);
  if (exists) return res.status(409).json({ error: 'Цей email вже зареєстрований' });

  const id = randomUUID();
  db.prepare(`INSERT INTO users (id, name, surname, phone, email, password, role, createdAt)
              VALUES (?, ?, ?, ?, ?, ?, 'user', ?)`)
    .run(id, name.trim(), surname.trim(), phone || '', email.trim().toLowerCase(), password, now());

  const user = db.prepare('SELECT id, name, surname, phone, email, role, createdAt FROM users WHERE id = ?').get(id);
  res.json({ success: true, user });
});

// POST /api/auth/login
app.post('/api/auth/login', (req, res) => {
  const { email, password } = req.body;
  const user = db.prepare('SELECT * FROM users WHERE email = ? AND password = ?')
                 .get(email?.trim().toLowerCase(), password);

  if (!user) return res.status(401).json({ error: 'Невірний email або пароль' });

  const { password: _pw, ...safeUser } = user;
  res.json({ success: true, user: safeUser });
});

// ════════════════════════════════════════════════════════════
//  PRODUCTS
// ════════════════════════════════════════════════════════════

// GET /api/products  — всі або за категорією (?category=rings)
app.get('/api/products', (req, res) => {
  const { category } = req.query;
  const products = category
    ? db.prepare('SELECT * FROM products WHERE category = ? ORDER BY createdAt DESC').all(category)
    : db.prepare('SELECT * FROM products ORDER BY createdAt DESC').all();
  res.json(products);
});

// POST /api/products  — додати (admin)
app.post('/api/products', requireAdmin, (req, res) => {
  const { category, name, desc, material, price, img } = req.body;
  if (!name) return res.status(400).json({ error: 'Назва обов\'язкова' });

  const id = randomUUID();
  db.prepare(`INSERT INTO products (id, category, name, desc, material, price, img, createdAt)
              VALUES (?, ?, ?, ?, ?, ?, ?, ?)`)
    .run(id, category || 'custom', name.trim(), desc || '', material || '', price || null, img || '', now());

  const product = db.prepare('SELECT * FROM products WHERE id = ?').get(id);
  res.json({ success: true, product });
});

// PUT /api/products/:id  — редагувати (admin)
app.put('/api/products/:id', requireAdmin, (req, res) => {
  const { id } = req.params;
  const existing = db.prepare('SELECT * FROM products WHERE id = ?').get(id);
  if (!existing) return res.status(404).json({ error: 'Виріб не знайдено' });

  const { category, name, desc, material, price, img } = req.body;
  db.prepare(`UPDATE products SET category=?, name=?, desc=?, material=?, price=?, img=?, updatedAt=? WHERE id=?`)
    .run(
      category  ?? existing.category,
      name      ?? existing.name,
      desc      ?? existing.desc,
      material  ?? existing.material,
      price     ?? existing.price,
      img       ?? existing.img,
      now(), id
    );

  const product = db.prepare('SELECT * FROM products WHERE id = ?').get(id);
  res.json({ success: true, product });
});

// DELETE /api/products/:id  — видалити (admin)
app.delete('/api/products/:id', requireAdmin, (req, res) => {
  const { id } = req.params;
  const existing = db.prepare('SELECT id FROM products WHERE id = ?').get(id);
  if (!existing) return res.status(404).json({ error: 'Виріб не знайдено' });

  db.prepare('DELETE FROM products WHERE id = ?').run(id);
  res.json({ success: true });
});

// ════════════════════════════════════════════════════════════
//  ORDERS
// ════════════════════════════════════════════════════════════

// GET /api/orders  — всі замовлення (admin)
app.get('/api/orders', requireAdmin, (req, res) => {
  const orders = db.prepare('SELECT * FROM orders ORDER BY createdAt DESC').all();
  res.json(orders);
});

// POST /api/orders  — нове замовлення (user)
app.post('/api/orders', requireAuth, (req, res) => {
  const { product, material, style, extras, notes } = req.body;
  if (!product || !material || !style)
    return res.status(400).json({ error: 'Заповніть обов\'язкові поля' });

  const id = randomUUID();
  db.prepare(`INSERT INTO orders (id, userId, userName, userEmail, userPhone, product, material, style, extras, notes, status, createdAt)
              VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, 'new', ?)`)
    .run(
      id, req.user.id,
      `${req.user.name} ${req.user.surname}`,
      req.user.email, req.user.phone || '',
      product, material, style,
      extras || '', notes || '', now()
    );

  res.json({ success: true, orderId: id });
});

// PATCH /api/orders/:id/status  — змінити статус (admin)
app.patch('/api/orders/:id/status', requireAdmin, (req, res) => {
  const { status } = req.body; // new | in_progress | done | cancelled
  db.prepare('UPDATE orders SET status = ? WHERE id = ?').run(status, req.params.id);
  res.json({ success: true });
});

// ════════════════════════════════════════════════════════════
//  USERS (admin)
// ════════════════════════════════════════════════════════════

// GET /api/users  — список юзерів (admin)
app.get('/api/users', requireAdmin, (req, res) => {
  const users = db.prepare('SELECT id, name, surname, phone, email, role, createdAt FROM users WHERE role != \'admin\'').all();
  res.json(users);
});

// ── Fallback: повертає index.html для SPA ────────────────────
app.get('*', (req, res) => {
  res.sendFile(path.join(__dirname, '..', 'index.html'));
});

// ── Старт ────────────────────────────────────────────────────
app.listen(PORT, () => {
  console.log(`
╔══════════════════════════════════════════╗
║   Christian Jewelry — Сервер запущено   ║
║   http://localhost:${PORT}                  ║
╠══════════════════════════════════════════╣
║   Адмін: admin@christianjewelry.ua      ║
║   Пароль: admin123                      ║
╚══════════════════════════════════════════╝
  `);
});
