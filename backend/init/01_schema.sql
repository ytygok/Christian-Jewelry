-- Розширення
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm"; -- для пошуку

-- Категорії виробів
CREATE TABLE categories (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name        VARCHAR(100) NOT NULL,          -- "Каблучки / Перстні"
    slug        VARCHAR(100) UNIQUE NOT NULL,   -- "rings"
    icon_url    TEXT,
    sort_order  SMALLINT DEFAULT 0,
    is_active   BOOLEAN DEFAULT TRUE,
    created_at  TIMESTAMPTZ DEFAULT NOW()
);

-- Матеріали
CREATE TABLE materials (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name        VARCHAR(100) NOT NULL,   -- "Срібло 925"
    code        VARCHAR(20) UNIQUE,     -- "AG925"
    description TEXT,
    created_at  TIMESTAMPTZ DEFAULT NOW()
);

-- Символи / теми (хрест, ікона, голуб...)
CREATE TABLE symbols (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name        VARCHAR(100) NOT NULL,   -- "Хрест", "Ікона Богородиці"
    slug        VARCHAR(100) UNIQUE NOT NULL,
    description TEXT,
    created_at  TIMESTAMPTZ DEFAULT NOW()
);

-- КОРИСТУВАЧІ

CREATE TABLE users (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    email           VARCHAR(255) UNIQUE NOT NULL,
    phone           VARCHAR(20),
    full_name       VARCHAR(200),
    password_hash   TEXT NOT NULL,
    role            VARCHAR(20) DEFAULT 'customer'  -- 'customer', 'admin'
                    CHECK (role IN ('customer', 'admin')),
    is_verified     BOOLEAN DEFAULT FALSE,
    is_active       BOOLEAN DEFAULT TRUE,
    created_at      TIMESTAMPTZ DEFAULT NOW(),
    updated_at      TIMESTAMPTZ DEFAULT NOW()
);

-- Адреси доставки
CREATE TABLE user_addresses (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id         UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    label           VARCHAR(50),         -- "Дім", "Робота"
    city            VARCHAR(100) NOT NULL,
    street          VARCHAR(200) NOT NULL,
    zip_code        VARCHAR(10),
    nova_poshta_ref VARCHAR(50),         -- номер відділення НП
    is_default      BOOLEAN DEFAULT FALSE,
    created_at      TIMESTAMPTZ DEFAULT NOW()
);

-- ПРОДУКТИ

CREATE TABLE products (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    category_id     UUID NOT NULL REFERENCES categories(id),
    name            VARCHAR(200) NOT NULL,       -- "Підвіска Хрест"
    slug            VARCHAR(200) UNIQUE NOT NULL,
    description     TEXT,
    price           NUMERIC(10,2) NOT NULL CHECK (price >= 0),
    price_old       NUMERIC(10,2),               -- для показу знижки
    stock_qty       INTEGER DEFAULT 0 CHECK (stock_qty >= 0),
    sku             VARCHAR(50) UNIQUE,          -- артикул
    is_active       BOOLEAN DEFAULT TRUE,
    is_custom       BOOLEAN DEFAULT FALSE,       -- "Індивідуальне"
    is_featured     BOOLEAN DEFAULT FALSE,       -- на головну
    weight_grams    NUMERIC(8,2),
    created_at      TIMESTAMPTZ DEFAULT NOW(),
    updated_at      TIMESTAMPTZ DEFAULT NOW()
);

-- Зображення продукту (галерея, як 3/4, 2/3 на скріншоті)
CREATE TABLE product_images (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    product_id  UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    url         TEXT NOT NULL,
    alt_text    VARCHAR(200),
    sort_order  SMALLINT DEFAULT 0,       -- порядок у слайдері
    is_cover    BOOLEAN DEFAULT FALSE,    -- головне фото
    created_at  TIMESTAMPTZ DEFAULT NOW()
);

-- Зв'язок продукт ↔ матеріал
CREATE TABLE product_materials (
    product_id  UUID REFERENCES products(id) ON DELETE CASCADE,
    material_id UUID REFERENCES materials(id) ON DELETE CASCADE,
    PRIMARY KEY (product_id, material_id)
);

-- Зв'язок продукт ↔ символ
CREATE TABLE product_symbols (
    product_id  UUID REFERENCES products(id) ON DELETE CASCADE,
    symbol_id   UUID REFERENCES symbols(id) ON DELETE CASCADE,
    PRIMARY KEY (product_id, symbol_id)
);

-- Варіанти продукту (розмір кільця, довжина ланцюжка...)
CREATE TABLE product_variants (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    product_id      UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    name            VARCHAR(100) NOT NULL,   -- "Розмір 17"
    price_modifier  NUMERIC(8,2) DEFAULT 0, -- +/- до базової ціни
    stock_qty       INTEGER DEFAULT 0,
    created_at      TIMESTAMPTZ DEFAULT NOW()
);
-- ЗАМОВЛЕННЯ

CREATE TABLE orders (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id             UUID REFERENCES users(id) ON DELETE SET NULL,
    status              VARCHAR(30) DEFAULT 'new'
                        CHECK (status IN (
                            'new',          -- нове
                            'confirmed',    -- підтверджено
                            'in_production',-- у виготовленні
                            'shipped',      -- відправлено
                            'delivered',    -- доставлено
                            'cancelled'     -- скасовано
                        )),
    -- Контакти (якщо гість)
    customer_name       VARCHAR(200) NOT NULL,
    customer_phone      VARCHAR(20) NOT NULL,
    customer_email      VARCHAR(255),

    -- Доставка
    delivery_method     VARCHAR(50) DEFAULT 'nova_poshta'
                        CHECK (delivery_method IN ('nova_poshta', 'ukrposhta', 'pickup')),
    delivery_city       VARCHAR(100),
    delivery_address    TEXT,
    nova_poshta_ref     VARCHAR(50),         -- ТТН

    -- Фінанси
    subtotal            NUMERIC(10,2) NOT NULL,
    discount_amount     NUMERIC(10,2) DEFAULT 0,
    shipping_cost       NUMERIC(10,2) DEFAULT 0,
    total_amount        NUMERIC(10,2) NOT NULL,

    -- Оплата
    payment_method      VARCHAR(30) DEFAULT 'card'
                        CHECK (payment_method IN ('card', 'cod', 'bank_transfer')),
    payment_status      VARCHAR(20) DEFAULT 'pending'
                        CHECK (payment_status IN ('pending', 'paid', 'refunded')),

    -- Додатково
    notes               TEXT,               -- коментар покупця
    admin_notes         TEXT,               -- нотатки адміністратора
    promo_code_id       UUID,
    created_at          TIMESTAMPTZ DEFAULT NOW(),
    updated_at          TIMESTAMPTZ DEFAULT NOW()
);

-- Позиції замовлення
CREATE TABLE order_items (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    order_id        UUID NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    product_id      UUID REFERENCES products(id) ON DELETE SET NULL,
    variant_id      UUID REFERENCES product_variants(id) ON DELETE SET NULL,

    -- Знімок на момент замовлення
    product_name    VARCHAR(200) NOT NULL,
    sku             VARCHAR(50),
    unit_price      NUMERIC(10,2) NOT NULL,
    quantity        INTEGER NOT NULL DEFAULT 1 CHECK (quantity > 0),
    subtotal        NUMERIC(10,2) NOT NULL,

    -- Для індивідуального виробу
    custom_note     TEXT,
    created_at      TIMESTAMPTZ DEFAULT NOW()
);

-- ПРОМОКОДИ

CREATE TABLE promo_codes (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    code            VARCHAR(50) UNIQUE NOT NULL,
    discount_type   VARCHAR(10) CHECK (discount_type IN ('percent', 'fixed')),
    discount_value  NUMERIC(8,2) NOT NULL,
    min_order_amount NUMERIC(10,2) DEFAULT 0,
    max_uses        INTEGER,
    used_count      INTEGER DEFAULT 0,
    expires_at      TIMESTAMPTZ,
    is_active       BOOLEAN DEFAULT TRUE,
    created_at      TIMESTAMPTZ DEFAULT NOW()
);

-- Зовнішній ключ для promo_code_id в orders
ALTER TABLE orders
    ADD CONSTRAINT fk_orders_promo_code
    FOREIGN KEY (promo_code_id) REFERENCES promo_codes(id) ON DELETE SET NULL;

-- ІНДИВІДУАЛЬНІ ЗАПИТИ (секція "Ваша ідея")

CREATE TABLE custom_requests (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id         UUID REFERENCES users(id) ON DELETE SET NULL,
    customer_name   VARCHAR(200) NOT NULL,
    customer_phone  VARCHAR(20) NOT NULL,
    customer_email  VARCHAR(255),
    description     TEXT NOT NULL,          -- опис виробу
    budget          NUMERIC(10,2),
    reference_urls  TEXT[],                 -- посилання на приклади
    status          VARCHAR(20) DEFAULT 'new'
                    CHECK (status IN ('new', 'in_review', 'quoted', 'approved', 'rejected')),
    admin_notes     TEXT,
    created_at      TIMESTAMPTZ DEFAULT NOW(),
    updated_at      TIMESTAMPTZ DEFAULT NOW()
);

-- Файли до індивідуального запиту
CREATE TABLE custom_request_files (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    request_id      UUID NOT NULL REFERENCES custom_requests(id) ON DELETE CASCADE,
    url             TEXT NOT NULL,
    file_type       VARCHAR(50),    -- 'image', 'pdf', 'sketch'
    created_at      TIMESTAMPTZ DEFAULT NOW()
);

-- КОШИК

CREATE TABLE cart_items (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id     UUID REFERENCES users(id) ON DELETE CASCADE,
    session_id  VARCHAR(100),           -- для гостей
    product_id  UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    variant_id  UUID REFERENCES product_variants(id) ON DELETE SET NULL,
    quantity    INTEGER DEFAULT 1 CHECK (quantity > 0),
    added_at    TIMESTAMPTZ DEFAULT NOW(),
    CONSTRAINT cart_user_or_session CHECK (
        user_id IS NOT NULL OR session_id IS NOT NULL
    )
);

-- ВІДГУКИ

CREATE TABLE reviews (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    product_id  UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    user_id     UUID REFERENCES users(id) ON DELETE SET NULL,
    author_name VARCHAR(100),
    rating      SMALLINT NOT NULL CHECK (rating BETWEEN 1 AND 5),
    comment     TEXT,
    is_approved BOOLEAN DEFAULT FALSE,
    created_at  TIMESTAMPTZ DEFAULT NOW()
);

-- ІНДЕКСИ

CREATE INDEX idx_products_category ON products(category_id);
CREATE INDEX idx_products_slug ON products(slug);
CREATE INDEX idx_products_active ON products(is_active) WHERE is_active = TRUE;
CREATE INDEX idx_orders_user ON orders(user_id);
CREATE INDEX idx_orders_status ON orders(status);
CREATE INDEX idx_orders_created ON orders(created_at DESC);
CREATE INDEX idx_order_items_order ON order_items(order_id);
CREATE INDEX idx_reviews_product ON reviews(product_id);
CREATE INDEX idx_cart_user ON cart_items(user_id);
CREATE INDEX idx_cart_session ON cart_items(session_id);

-- Пошук по назві продукту
CREATE INDEX idx_products_name_trgm ON products USING GIN (name gin_trgm_ops);