// ============================================================
//  database.js — SQLite через бібліотеку sqlite3 (async)
// ============================================================
const sqlite3 = require('sqlite3').verbose();
const path    = require('path');

const db = new sqlite3.Database(path.join(__dirname, 'database.db'));

// Допоміжні функції для зручної роботи з async/await
function run(sql, params = []) {
    return new Promise((resolve, reject) => {
        db.run(sql, params, function(err) {
            if (err) reject(err);
            else resolve({ lastID: this.lastID, changes: this.changes });
        });
    });
}

function get(sql, params = []) {
    return new Promise((resolve, reject) => {
        db.get(sql, params, (err, row) => {
            if (err) reject(err);
            else resolve(row);
        });
    });
}

function all(sql, params = []) {
    return new Promise((resolve, reject) => {
        db.all(sql, params, (err, rows) => {
            if (err) reject(err);
            else resolve(rows);
        });
    });
}

// Ініціалізація таблиць
async function init() {
    await run(`CREATE TABLE IF NOT EXISTS users (
        id        TEXT PRIMARY KEY,
        name      TEXT NOT NULL,
        surname   TEXT NOT NULL,
        phone     TEXT,
        email     TEXT UNIQUE NOT NULL,
        password  TEXT NOT NULL,
        role      TEXT NOT NULL DEFAULT 'user',
        createdAt TEXT NOT NULL
    )`);

    await run(`CREATE TABLE IF NOT EXISTS products (
        id        TEXT PRIMARY KEY,
        category  TEXT NOT NULL,
        name      TEXT NOT NULL,
        desc      TEXT,
        material  TEXT,
        price     REAL,
        img       TEXT,
        createdAt TEXT NOT NULL,
        updatedAt TEXT
    )`);

    await run(`CREATE TABLE IF NOT EXISTS orders (
        id        TEXT PRIMARY KEY,
        userId    TEXT NOT NULL,
        userName  TEXT,
        userEmail TEXT,
        userPhone TEXT,
        product   TEXT NOT NULL,
        material  TEXT NOT NULL,
        style     TEXT NOT NULL,
        extras    TEXT,
        notes     TEXT,
        status    TEXT NOT NULL DEFAULT 'new',
        createdAt TEXT NOT NULL
    )`);

    // Seed admin
    const admin = await get('SELECT id FROM users WHERE email = ?', ['admin@christianjewelry.ua']);
    if (!admin) {
        await run(
            `INSERT INTO users (id, name, surname, phone, email, password, role, createdAt) VALUES (?, ?, ?, ?, ?, ?, ?, ?)`,
            ['admin', 'Адмін', '', '', 'admin@christianjewelry.ua', 'admin123', 'admin', new Date().toISOString()]
        );
        console.log('✅ Адмін створений: admin@christianjewelry.ua / admin123');
    }

    console.log('✅ База даних готова');
}

module.exports = { db, run, get, all, init };
