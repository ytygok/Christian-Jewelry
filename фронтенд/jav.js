// ============================================================
//  CHRISTIAN JEWELRY — Main JS  v4
//  USE_SERVER = false  →  localStorage (без сервера)
//  USE_SERVER = true   →  REST API на localhost:3000
// ============================================================

const USE_SERVER = true;          // ← змін на true коли запустиш server/server.js
const API_URL    = 'http://localhost:5000/api';

// ════════════════════════════════════════════════════════════
//  DB LAYER — автоматично перемикається між localStorage і API
// ════════════════════════════════════════════════════════════
const DB = USE_SERVER ? {

    // ── SERVER MODE ─────────────────────────────────────────
    _userId: () => {
        const u = JSON.parse(sessionStorage.getItem('cj_current') || 'null');
        return u ? u.id : null;
    },
    _headers: () => {
        const h = { 'Content-Type': 'application/json' };
        const id = DB._userId();
        if (id) h['x-user-id'] = id;
        return h;
    },
    async getProducts(category) {
        const url = category ? `${API_URL}/products?category=${category}` : `${API_URL}/products`;
        const r = await fetch(url);
        return r.json();
    },
    async addProduct(data) {
        const r = await fetch(`${API_URL}/products`, { method:'POST', headers:DB._headers(), body:JSON.stringify(data) });
        return r.json();
    },
    async updateProduct(id, data) {
        const r = await fetch(`${API_URL}/products/${id}`, { method:'PUT', headers:DB._headers(), body:JSON.stringify(data) });
        return r.json();
    },
    async deleteProduct(id) {
        const r = await fetch(`${API_URL}/products/${id}`, { method:'DELETE', headers:DB._headers() });
        return r.json();
    },
    async login(email, password) {
        const r = await fetch(`${API_URL}/auth/login`, { method:'POST', headers:{'Content-Type':'application/json'}, body:JSON.stringify({email,password}) });
        return r.json();
    },
    async register(data) {
        const r = await fetch(`${API_URL}/auth/register`, { method:'POST', headers:{'Content-Type':'application/json'}, body:JSON.stringify(data) });
        return r.json();
    },
    async getUsers() {
        const r = await fetch(`${API_URL}/users`, { headers:DB._headers() });
        return r.json();
    },
    async getOrders() {
        const r = await fetch(`${API_URL}/orders`, { headers:DB._headers() });
        return r.json();
    },
    async addOrder(data) {
        const r = await fetch(`${API_URL}/orders`, { method:'POST', headers:DB._headers(), body:JSON.stringify(data) });
        return r.json();
    },

} : {

    // ── LOCALSTORAGE MODE ────────────────────────────────────
    _get: k  => JSON.parse(localStorage.getItem(k) || '[]'),
    _set: (k,v) => localStorage.setItem(k, JSON.stringify(v)),

    async getProducts(category) {
        const all = DB._get('cj_products');
        return category ? all.filter(p => p.category === category) : all;
    },
    async addProduct(data) {
        const products = DB._get('cj_products');
        const p = { id: uid(), ...data, createdAt: now() };
        products.push(p);
        DB._set('cj_products', products);
        return { success: true, product: p };
    },
    async updateProduct(id, data) {
        const products = DB._get('cj_products');
        const idx = products.findIndex(p => p.id === id);
        if (idx === -1) return { error: 'Не знайдено' };
        products[idx] = { ...products[idx], ...data, updatedAt: now() };
        DB._set('cj_products', products);
        return { success: true, product: products[idx] };
    },
    async deleteProduct(id) {
        DB._set('cj_products', DB._get('cj_products').filter(p => p.id !== id));
        return { success: true };
    },
    async login(email, password) {
        const user = DB._get('cj_users').find(u => u.email === email && u.password === password);
        if (!user) return { error: 'Невірний email або пароль' };
        const { password: _, ...safe } = user;
        return { success: true, user: safe };
    },
    async register(data) {
        const users = DB._get('cj_users');
        if (users.find(u => u.email === data.email)) return { error: 'Цей email вже зареєстрований' };
        const newUser = { id: uid(), ...data, role: 'user', createdAt: now() };
        users.push(newUser);
        DB._set('cj_users', users);
        const { password: _, ...safe } = newUser;
        return { success: true, user: safe };
    },
    async getUsers() {
        return DB._get('cj_users').filter(u => u.role !== 'admin').map(({ password: _, ...u }) => u);
    },
    async getOrders() { return DB._get('cj_orders'); },
    async addOrder(data) {
        const orders = DB._get('cj_orders');
        const o = { id: uid(), ...data, status: 'new', createdAt: now() };
        orders.push(o);
        DB._set('cj_orders', orders);
        return { success: true };
    },
    exportJSON() {
        const data = {
            exportedAt: now(),
            users:    DB._get('cj_users'),
            products: DB._get('cj_products'),
            orders:   DB._get('cj_orders'),
        };
        const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
        const a = document.createElement('a');
        a.href = URL.createObjectURL(blob);
        a.download = 'cj_backup_' + new Date().toISOString().slice(0,10) + '.json';
        a.click();
    }
};

// ════════════════════════════════════════════════════════════
//  HELPERS
// ════════════════════════════════════════════════════════════
function uid()  { return Date.now().toString(36) + Math.random().toString(36).slice(2); }
function now()  { return new Date().toISOString(); }
function esc(s) {
    if (!s) return '';
    return String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
}

// ════════════════════════════════════════════════════════════
//  ADMIN SEED (localStorage only)
// ════════════════════════════════════════════════════════════
if (!USE_SERVER) {
    const users = JSON.parse(localStorage.getItem('cj_users') || '[]');
    if (!users.find(u => u.email === 'admin@christianjewelry.ua')) {
        users.push({ id:'admin', name:'Адмін', surname:'', phone:'', email:'admin@christianjewelry.ua', password:'admin123', role:'admin', createdAt:now() });
        localStorage.setItem('cj_users', JSON.stringify(users));
    }
}

// ════════════════════════════════════════════════════════════
//  SESSION
// ════════════════════════════════════════════════════════════
let currentUser = JSON.parse(sessionStorage.getItem('cj_current') || 'null');

// ════════════════════════════════════════════════════════════
//  CATEGORY CONFIG
// ════════════════════════════════════════════════════════════
const CATEGORIES = {
    rings:     'Каблучки / Перстні',
    earrings:  'Сережки',
    pendants:  'Підвіски',
    bracelets: 'Браслети',
    necklaces: "Коль'є",
    brooches:  'Брошки',
    custom:    'Індивідуальне',
};

let currentCategory = null;

// ════════════════════════════════════════════════════════════
//  PAGES + BROWSER HISTORY
// ════════════════════════════════════════════════════════════
function showPage(name, pushState = true) {
    document.querySelectorAll('.page').forEach(p => p.classList.remove('active'));
    const page = document.getElementById('page-' + name);
    if (page) page.classList.add('active');
    window.scrollTo(0, 0);
    if (name === 'home') renderHomeCatalog();
    if (pushState) {
        history.pushState({ page: name, category: null }, '', '#' + name);
    }
}

function showCategory(catKey, pushState = true) {
    currentCategory = catKey;
    document.getElementById('categoryTitle').textContent = CATEGORIES[catKey] || catKey;
    renderCategoryProducts(catKey);
    document.querySelectorAll('.page').forEach(p => p.classList.remove('active'));
    document.getElementById('page-category').classList.add('active');
    window.scrollTo(0, 0);
    if (pushState) {
        history.pushState({ page: 'category', category: catKey }, '', '#category-' + catKey);
    }
}

// Браузерна кнопка «Назад» / «Вперед»
window.addEventListener('popstate', e => {
    const state = e.state;
    if (!state) { showPage('home', false); return; }
    if (state.page === 'category' && state.category) {
        showCategory(state.category, false);
    } else {
        showPage(state.page || 'home', false);
    }
});

// ════════════════════════════════════════════════════════════
//  CARD BUILDER
// ════════════════════════════════════════════════════════════
const PLACEHOLDER = `data:image/svg+xml,<svg xmlns='http://www.w3.org/2000/svg' width='400' height='400'><rect fill='%23f0f0f0' width='400' height='400'/><text x='50%25' y='50%25' dominant-baseline='middle' text-anchor='middle' fill='%23bbb' font-size='60'>✦</text></svg>`;

function getMedia(p) {
    // media — масив об'єктів { type:'image'|'video', src:string }
    if (p.media && p.media.length) return p.media;
    if (p.img) return [{ type: 'image', src: p.img }];
    return [];
}

function buildCard(p, showBadge) {
    const role     = currentUser ? currentUser.role : 'guest';
    const badge    = showBadge ? `<div class="product-card__cat-badge">${esc(CATEGORIES[p.category] || p.category)}</div>` : '';
    const orderBtn = role !== 'guest' ? `<button class="btn-sm" onclick="openOrderModal('${esc(p.name)}')">Замовити</button>` : '';
    const editBtn  = role === 'admin'  ? `<button class="btn-sm warning" onclick="openEditModal('${esc(p.id)}')">Редагувати</button>` : '';
    const delBtn   = role === 'admin'  ? `<button class="btn-sm danger"  onclick="deleteProduct('${esc(p.id)}')">Видалити</button>` : '';
    const actions  = (orderBtn||editBtn||delBtn) ? `<div class="product-card__actions">${orderBtn}${editBtn}${delBtn}</div>` : '';
    const price    = p.price ? `<div class="product-card__price">${Number(p.price).toLocaleString('uk-UA')} грн</div>` : '';

    const media    = getMedia(p);
    const firstSrc = media.length ? media[0].src : PLACEHOLDER;
    const isVideo  = media.length && media[0].type === 'video';
    const countBadge = media.length > 1 ? `<div class="product-card__media-count">▶ ${media.length} фото/відео</div>` : '';
    const mediaEl  = isVideo
        ? `<video src="${esc(firstSrc)}" muted playsinline preload="metadata" style="width:100%;height:100%;object-fit:cover;"></video>`
        : `<img src="${esc(firstSrc)}" alt="${esc(p.name)}" onerror="this.src='${PLACEHOLDER}'">`;

    return `<div class="product-card" data-id="${esc(p.id)}">
        ${badge}
        <div class="product-card__img" onclick="openLightbox('${esc(p.id)}', 0)">
            ${mediaEl}
            ${countBadge}
        </div>
        <div class="product-card__info">
            <div class="product-card__name">${esc(p.name)}</div>
            ${p.material ? `<div class="product-card__material">${esc(p.material)}</div>` : ''}
            ${p.desc     ? `<div class="product-card__desc">${esc(p.desc)}</div>` : ''}
            ${price}
            ${actions}
        </div>
    </div>`;
}

// ════════════════════════════════════════════════════════════
//  LIGHTBOX
// ════════════════════════════════════════════════════════════
let _lbProducts = [];   // всі завантажені продукти для пошуку
let _lbMedia    = [];   // масив { type, src } поточного виробу
let _lbIndex    = 0;

async function openLightbox(productId, startIndex) {
    // підвантажуємо всі продукти якщо ще не маємо
    if (!_lbProducts.length) _lbProducts = await DB.getProducts();
    const p = _lbProducts.find(x => x.id === productId);
    if (!p) return;

    _lbMedia = getMedia(p);
    if (!_lbMedia.length) return;

    _lbIndex = startIndex || 0;
    renderLightbox();
    openModal('lightboxModal');

    // клавіатура
    document.addEventListener('keydown', _lbKeyHandler);
}

function closeLightbox() {
    closeModal('lightboxModal');
    document.removeEventListener('keydown', _lbKeyHandler);
    // зупиняємо відео якщо грає
    const vid = document.querySelector('#lightboxMedia video');
    if (vid) vid.pause();
}

function _lbKeyHandler(e) {
    if (e.key === 'ArrowLeft')  lightboxNav(-1);
    if (e.key === 'ArrowRight') lightboxNav(1);
    if (e.key === 'Escape')     closeLightbox();
}

function lightboxNav(dir) {
    _lbIndex = (_lbIndex + dir + _lbMedia.length) % _lbMedia.length;
    renderLightbox();
}

function lightboxGoTo(idx) {
    _lbIndex = idx;
    renderLightbox();
}

function renderLightbox() {
    const item    = _lbMedia[_lbIndex];
    const mediaEl = document.getElementById('lightboxMedia');
    const counter = document.getElementById('lightboxCounter');
    const thumbs  = document.getElementById('lightboxThumbs');

    // зупиняємо попереднє відео
    const oldVid = mediaEl.querySelector('video');
    if (oldVid) oldVid.pause();

    // головний медіа-елемент
    if (item.type === 'video') {
        mediaEl.innerHTML = `<video src="${esc(item.src)}" controls autoplay playsinline style="max-width:100%;max-height:72vh;outline:none;"></video>`;
    } else {
        mediaEl.innerHTML = `<img src="${esc(item.src)}" alt="Фото виробу" style="max-width:100%;max-height:72vh;object-fit:contain;">`;
    }

    // лічильник
    counter.textContent = `${_lbIndex + 1} / ${_lbMedia.length}`;

    // мініатюри
    thumbs.innerHTML = _lbMedia.map((m, i) => {
        const active = i === _lbIndex ? 'active' : '';
        if (m.type === 'video') {
            return `<div class="lightbox__thumb-video ${active}" onclick="lightboxGoTo(${i})">▶</div>`;
        }
        return `<img class="lightbox__thumb ${active}" src="${esc(m.src)}" onclick="lightboxGoTo(${i})" alt="мініатюра ${i+1}">`;
    }).join('');

    // приховуємо навігацію якщо один елемент
    document.querySelector('.lightbox__nav--prev').style.display = _lbMedia.length > 1 ? '' : 'none';
    document.querySelector('.lightbox__nav--next').style.display = _lbMedia.length > 1 ? '' : 'none';
}

// ════════════════════════════════════════════════════════════
//  PRODUCT CRUD (admin)
// ════════════════════════════════════════════════════════════

// Поточний список медіа у формі адміна
let _adminMedia = [];   // [{ type:'image'|'video', src:string }]

function openAddProductModal() {
    document.getElementById('addProductTitle').textContent = 'Додати виріб';
    document.getElementById('saveProductBtn').textContent  = '+ Зберегти виріб';
    document.getElementById('editProductId').value = '';
    ['addName','addDesc','addMaterial','addPrice','addMediaUrl'].forEach(id => document.getElementById(id).value = '');
    document.getElementById('addCat').value = 'rings';
    _adminMedia = [];
    renderMediaPreview();
    openModal('addProductModal');
}

let _cachedProducts = [];

async function openEditModal(id) {
    _cachedProducts = await DB.getProducts();
    const p = _cachedProducts.find(x => x.id === id);
    if (!p) { showNotif('Виріб не знайдено', 'error'); return; }

    document.getElementById('addProductTitle').textContent = 'Редагувати виріб';
    document.getElementById('saveProductBtn').textContent  = '✓ Зберегти зміни';
    document.getElementById('editProductId').value = id;
    document.getElementById('addCat').value      = p.category || 'rings';
    document.getElementById('addName').value     = p.name     || '';
    document.getElementById('addDesc').value     = p.desc     || '';
    document.getElementById('addMaterial').value = p.material || '';
    document.getElementById('addPrice').value    = p.price    || '';
    document.getElementById('addMediaUrl').value = '';

    // Завантажуємо наявні медіа
    _adminMedia = getMedia(p).map(m => ({ ...m }));
    renderMediaPreview();
    openModal('addProductModal');
}

// Обробка файлів через input
function handleMediaFiles(input) {
    const files = Array.from(input.files);
    files.forEach(file => {
        const type = file.type.startsWith('video') ? 'video' : 'image';
        const reader = new FileReader();
        reader.onload = e => {
            _adminMedia.push({ type, src: e.target.result });
            renderMediaPreview();
        };
        reader.readAsDataURL(file);
    });
    input.value = ''; // скидаємо щоб можна було додати той самий файл ще раз
}

// Додавання через URL
function addMediaUrl() {
    const input = document.getElementById('addMediaUrl');
    const url   = input.value.trim();
    if (!url) return;
    const isVideo = /\.(mp4|mov|webm|ogg)(\?|$)/i.test(url);
    _adminMedia.push({ type: isVideo ? 'video' : 'image', src: url });
    input.value = '';
    renderMediaPreview();
}

// Видалення медіа зі списку
function removeMedia(idx) {
    _adminMedia.splice(idx, 1);
    renderMediaPreview();
}

// Відображення мініатюр у формі адміна
function renderMediaPreview() {
    const list = document.getElementById('mediaPreviewList');
    if (!_adminMedia.length) {
        list.innerHTML = '';
        return;
    }
    list.innerHTML = _adminMedia.map((m, i) => {
        const preview = m.type === 'video'
            ? `<video src="${esc(m.src)}" muted playsinline preload="metadata"></video>`
            : `<img src="${esc(m.src)}" alt="медіа ${i+1}" onerror="this.style.background='#eee'">`;
        const typeBadge = m.type === 'video' ? `<span class="media-thumb__type">відео</span>` : '';
        return `<div class="media-thumb">
            ${preview}
            ${typeBadge}
            <button class="media-thumb__remove" onclick="removeMedia(${i})" title="Видалити">✕</button>
        </div>`;
    }).join('');
}

async function adminSaveProduct() {
    const id       = document.getElementById('editProductId').value;
    const category = document.getElementById('addCat').value;
    const name     = document.getElementById('addName').value.trim();
    const desc     = document.getElementById('addDesc').value.trim();
    const material = document.getElementById('addMaterial').value.trim();
    const price    = document.getElementById('addPrice').value;

    if (!name) { showNotif('Введіть назву виробу', 'error'); return; }

    // img — перше зображення для сумісності зі старим кодом
    const firstImg = _adminMedia.find(m => m.type === 'image');
    const data = {
        category, name, desc, material,
        price: price || null,
        img:   firstImg ? firstImg.src : '',
        media: _adminMedia,
    };

    let result;
    if (id) {
        result = await DB.updateProduct(id, data);
        if (result.error) { showNotif(result.error, 'error'); return; }
        showNotif('Зміни збережено!', 'success');
    } else {
        result = await DB.addProduct(data);
        if (result.error) { showNotif(result.error, 'error'); return; }
        showNotif('Виріб додано!', 'success');
    }

    _lbProducts = []; // скидаємо кеш lightbox
    closeModal('addProductModal');
    refreshAll();
}

async function deleteProduct(id) {
    if (!confirm('Видалити цей виріб? Це незворотньо.')) return;
    const result = await DB.deleteProduct(id);
    if (result.error) { showNotif(result.error, 'error'); return; }
    showNotif('Виріб видалено', 'success');
    _lbProducts = [];
    refreshAll();
}

function refreshAll() {
    renderHomeCatalog();
    if (currentCategory) renderCategoryProducts(currentCategory);
}

function exportDB() {
    if (USE_SERVER) { showNotif('Для сервер-режиму — завантаж через панель адміна', 'error'); return; }
    DB.exportJSON();
    showNotif('Базу даних експортовано!', 'success');
}

// ════════════════════════════════════════════════════════════
//  HOME CATALOG
// ════════════════════════════════════════════════════════════
async function renderHomeCatalog() {
    const grid = document.getElementById('homeCatalogGrid');
    grid.innerHTML = '<div class="home-catalog__empty"><p>Завантаження...</p></div>';
    try {
        const products = await DB.getProducts();
        _lbProducts = products; // кешуємо для lightbox
        if (!products.length) {
            grid.innerHTML = `<div class="home-catalog__empty">
                <h3>Каталог порожній</h3>
                <p>Адміністратор ще не додав жодного виробу.</p>
            </div>`;
            return;
        }
        grid.innerHTML = products.map(p => buildCard(p, true)).join('');
    } catch(e) {
        grid.innerHTML = '<div class="home-catalog__empty"><p>Помилка завантаження каталогу.</p></div>';
    }
}

// ════════════════════════════════════════════════════════════
//  CATEGORY PAGE
// ════════════════════════════════════════════════════════════
async function renderCategoryProducts(catKey) {
    const grid = document.getElementById('productsGrid');
    grid.innerHTML = '<div class="empty-state"><p>Завантаження...</p></div>';
    try {
        const products = await DB.getProducts(catKey);
        // додаємо до кешу lightbox (не замінюємо, а merge)
        products.forEach(p => { if (!_lbProducts.find(x => x.id === p.id)) _lbProducts.push(p); });
        if (!products.length) {
            grid.innerHTML = `<div class="empty-state">
                <h3>Поки що тут порожньо</h3>
                <p>Незабаром тут з'являться вироби з цієї категорії.</p>
            </div>`;
            return;
        }
        grid.innerHTML = products.map(p => buildCard(p, false)).join('');
    } catch(e) {
        grid.innerHTML = '<div class="empty-state"><p>Помилка завантаження.</p></div>';
    }
}

// ════════════════════════════════════════════════════════════
//  PRODUCT CRUD
// ════════════════════════════════════════════════════════════
// ════════════════════════════════════════════════════════════
//  AUTH UI
// ════════════════════════════════════════════════════════════
function renderAuthArea() {
    const area     = document.getElementById('authArea');
    const adminBar = document.getElementById('adminBar');

    if (!currentUser) {
        area.innerHTML = `<button class="nav__btn" onclick="openAuthModal()">Увійти</button>`;
        adminBar.style.display = 'none';
    } else {
        const label = currentUser.role === 'admin' ? 'Адмін' : 'Користувач';
        area.innerHTML = `<div class="user-badge" onclick="doLogout()" title="Вийти">
            <span>${esc(currentUser.name)}</span>
            <span class="role-tag ${currentUser.role}">${label}</span>
            <span style="font-size:10px;opacity:.5;">Вийти</span>
        </div>`;
        adminBar.style.display = currentUser.role === 'admin' ? 'flex' : 'none';
    }
}

// ════════════════════════════════════════════════════════════
//  MODALS
// ════════════════════════════════════════════════════════════
function openModal(id)  { document.getElementById(id).classList.add('open'); }
function closeModal(id) { document.getElementById(id).classList.remove('open'); }
document.addEventListener('click', e => {
    if (e.target.classList.contains('modal-overlay')) e.target.classList.remove('open');
});
function openAuthModal() { openModal('authModal'); switchTab('login'); }

// ════════════════════════════════════════════════════════════
//  AUTH LOGIC
// ════════════════════════════════════════════════════════════
function switchTab(tab) {
    document.getElementById('loginForm').style.display    = tab==='login'    ? 'block':'none';
    document.getElementById('registerForm').style.display = tab==='register' ? 'block':'none';
    document.getElementById('tabLogin').classList.toggle('active',    tab==='login');
    document.getElementById('tabRegister').classList.toggle('active', tab==='register');
    document.getElementById('authTitle').textContent    = tab==='login' ? 'Увійти' : 'Реєстрація';
    document.getElementById('authSubtitle').textContent = tab==='login' ? 'Введіть свої дані для входу' : 'Створіть обліковий запис';
    document.getElementById('loginError').style.display     = 'none';
    document.getElementById('registerError').style.display  = 'none';
    document.getElementById('registerSuccess').style.display = 'none';
}

async function doLogin() {
    const email = document.getElementById('loginEmail').value.trim();
    const pass  = document.getElementById('loginPassword').value;
    const result = await DB.login(email, pass);
    if (result.error) { document.getElementById('loginError').style.display='block'; return; }
    currentUser = result.user;
    sessionStorage.setItem('cj_current', JSON.stringify(currentUser));
    closeModal('authModal');
    renderAuthArea();
    renderHomeCatalog();
    showNotif(`Ласкаво просимо, ${currentUser.name}!`, 'success');
}

async function doRegister() {
    const name    = document.getElementById('regName').value.trim();
    const surname = document.getElementById('regSurname').value.trim();
    const phone   = document.getElementById('regPhone').value.trim();
    const email   = document.getElementById('regEmail').value.trim();
    const password = document.getElementById('regPassword').value;
    const errEl   = document.getElementById('registerError');
    errEl.style.display = 'none';

    if (!name||!surname||!phone||!email||!password) {
        errEl.textContent='Будь ласка, заповніть всі поля.'; errEl.style.display='block'; return;
    }
    if (password.length < 6) {
        errEl.textContent='Пароль має містити мінімум 6 символів.'; errEl.style.display='block'; return;
    }

    const result = await DB.register({ name, surname, phone, email: email.toLowerCase(), password });
    if (result.error) { errEl.textContent=result.error; errEl.style.display='block'; return; }

    document.getElementById('registerSuccess').style.display='block';
    setTimeout(() => switchTab('login'), 1600);
}

function doLogout() {
    if (!confirm('Вийти з облікового запису?')) return;
    currentUser = null;
    sessionStorage.removeItem('cj_current');
    renderAuthArea();
    renderHomeCatalog();
    showNotif('Ви вийшли з облікового запису');
    showPage('home');
}

// ════════════════════════════════════════════════════════════
//  ORDER
// ════════════════════════════════════════════════════════════
function openOrderModal(productName) {
    if (!currentUser) { showNotif('Для замовлення потрібно увійти', 'error'); openAuthModal(); return; }
    const sel = document.getElementById('orderProduct');
    if (productName) {
        let found = false;
        for (let i=0; i<sel.options.length; i++) {
            if (sel.options[i].value===productName || sel.options[i].text===productName) {
                sel.selectedIndex=i; found=true; break;
            }
        }
        if (!found) { const opt=document.createElement('option'); opt.value=opt.text=productName; sel.appendChild(opt); sel.value=productName; }
    }
    document.getElementById('orderSuccess').style.display='none';
    document.getElementById('orderFilesPreview').innerHTML='';
    openModal('orderModal');
}

function previewFiles(input) {
    const preview = document.getElementById('orderFilesPreview');
    preview.innerHTML='';
    Array.from(input.files).forEach(file => {
        const r=new FileReader();
        r.onload=e=>{ const img=document.createElement('img'); img.src=e.target.result; preview.appendChild(img); };
        r.readAsDataURL(file);
    });
}

async function submitOrder() {
    const product  = document.getElementById('orderProduct').value;
    const material = document.getElementById('orderMaterial').value;
    const style    = document.getElementById('orderStyle').value;
    const extras   = document.getElementById('orderExtras').value;
    const notes    = document.getElementById('orderNotes').value;
    if (!product||!material||!style) { showNotif('Оберіть виріб, матеріал і стиль','error'); return; }

    const result = await DB.addOrder({
        userId: currentUser.id,
        userName: currentUser.name + ' ' + currentUser.surname,
        userEmail: currentUser.email,
        userPhone: currentUser.phone || '',
        product, material, style, extras, notes
    });

    if (result.error) { showNotif(result.error, 'error'); return; }
    document.getElementById('orderSuccess').style.display='block';
    setTimeout(()=>closeModal('orderModal'), 3000);
}

// ════════════════════════════════════════════════════════════
//  ADMIN — USERS & ORDERS PANEL
// ════════════════════════════════════════════════════════════
async function openUsersModal() {
    const wrap = document.getElementById('usersTableWrap');
    wrap.innerHTML = '<p style="padding:16px;color:#999;font-size:13px;">Завантаження...</p>';
    openModal('usersModal');

    const [users, orders] = await Promise.all([DB.getUsers(), DB.getOrders()]);

    let html = '';

    // Users table
    if (!users.length) {
        html += '<p style="margin-top:14px;color:#999;font-size:13px;">Зареєстрованих користувачів ще немає.</p>';
    } else {
        const rows = users.map(u => {
            const cnt = orders.filter(o => o.userId === u.id).length;
            return `<tr>
                <td>${esc(u.name)} ${esc(u.surname)}</td>
                <td>${esc(u.email)}</td>
                <td>${esc(u.phone)||'—'}</td>
                <td><span class="role-badge user">Користувач</span></td>
                <td>${cnt}</td>
                <td>${new Date(u.createdAt).toLocaleDateString('uk-UA')}</td>
            </tr>`;
        }).join('');
        html += `<table class="users-table">
            <thead><tr><th>Ім'я</th><th>Email</th><th>Телефон</th><th>Роль</th><th>Замовл.</th><th>Реєстр.</th></tr></thead>
            <tbody>${rows}</tbody>
        </table>`;
    }

    // Orders table
    const statusLabel = { new:'🆕 Нове', in_progress:'🔧 В роботі', done:'✅ Виконано', cancelled:'❌ Скасовано' };
    if (orders.length) {
        const rows2 = [...orders].reverse().slice(0,20).map(o=>`<tr>
            <td>${esc(o.userName)}</td>
            <td>${esc(o.userEmail)}</td>
            <td>${esc(o.product)}</td>
            <td>${esc(o.material)}</td>
            <td>${esc(o.style)}</td>
            <td>${statusLabel[o.status]||o.status}</td>
            <td>${new Date(o.createdAt).toLocaleDateString('uk-UA')}</td>
        </tr>`).join('');
        html += `<p class="section-title">Останні замовлення (${orders.length})</p>
        <table class="users-table" style="margin-top:8px;">
            <thead><tr><th>Клієнт</th><th>Email</th><th>Виріб</th><th>Матеріал</th><th>Стиль</th><th>Статус</th><th>Дата</th></tr></thead>
            <tbody>${rows2}</tbody>
        </table>`;
    } else {
        html += '<p style="margin-top:18px;color:#999;font-size:13px;">Замовлень ще немає.</p>';
    }

    wrap.innerHTML = html;
}

// ════════════════════════════════════════════════════════════
//  NOTIFICATIONS
// ════════════════════════════════════════════════════════════
function showNotif(msg, type) {
    const n = document.getElementById('notif');
    n.textContent = msg;
    n.className = 'notif' + (type ? ' '+type : '');
    void n.offsetWidth;
    n.classList.add('show');
    setTimeout(() => n.classList.remove('show'), 3000);
}

// ════════════════════════════════════════════════════════════
//  THEME TOGGLE
// ════════════════════════════════════════════════════════════
function applyTheme(theme) {
    document.documentElement.setAttribute('data-theme', theme);
    const btn = document.getElementById('themeBtn');
    if (btn) btn.textContent = theme === 'dark' ? '🌙' : '☀️';
    localStorage.setItem('cj_theme', theme);
}

function toggleTheme() {
    const current = document.documentElement.getAttribute('data-theme') || 'dark';
    applyTheme(current === 'dark' ? 'light' : 'dark');
}

// ════════════════════════════════════════════════════════════
//  BOOT
// ════════════════════════════════════════════════════════════

// Відновлюємо збережену тему
(function() {
    const saved = localStorage.getItem('cj_theme');
    if (saved) {
        applyTheme(saved);
    } else {
        // Автоматично визначаємо тему пристрою
        const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
        applyTheme(prefersDark ? 'dark' : 'light');
    }
})();

// Слідкуємо за зміною системної теми
window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', e => {
    if (!localStorage.getItem('cj_theme')) {
        applyTheme(e.matches ? 'dark' : 'light');
    }
});

renderAuthArea();
renderHomeCatalog();
history.replaceState({ page: 'home', category: null }, '', '#home');
