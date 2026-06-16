-- ПОЧАТКОВІ ДАНІ
-- Категорії (відповідно до скріншоту)
INSERT INTO categories (name, slug, sort_order) VALUES
    ('Каблучки / Перстні', 'rings',     1),
    ('Сережки',            'earrings',  2),
    ('Підвіски',           'pendants',  3),
    ('Браслети',           'bracelets', 4),
    ('Кольє',              'necklaces', 5),
    ('Брошки',             'brooches',  6),
    ('Індивідуальне',      'custom',    7);

-- Матеріали
INSERT INTO materials (name, code) VALUES
    ('Срібло 925',         'AG925'),
    ('Срібло 999',         'AG999'),
    ('Золото 585',         'AU585'),
    ('Шкіряний шнур',     'LEATHER'),
    ('Натуральні камені', 'STONES');

-- Символи
INSERT INTO symbols (name, slug) VALUES
    ('Хрест',               'cross'),
    ('Ісус Христос',        'jesus-christ'),
    ('Богородиця',          'virgin-mary'),
    ('Голуб',               'dove'),
    ('Риба (Іхтіс)',        'ichthys'),
    ('Ієрусалимський хрест','jerusalem-cross');

-- Адмін
INSERT INTO users (email, full_name, password_hash, role, is_verified) VALUES
    ('admin@christian-jewelry.ua', 'Admin', '$2b$12$placeholder', 'admin', TRUE);