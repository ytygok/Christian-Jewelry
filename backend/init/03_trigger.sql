-- ТРИГЕРИ ДЛЯ АВТОМАТИЧНОГО ОНОВЛЕННЯ updated_at
-- Функція-тригер (одна для всіх таблиць)
CREATE OR REPLACE FUNCTION trigger_set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Підключення тригера до кожної таблиці з updated_at

-- users
CREATE TRIGGER set_updated_at_users
    BEFORE UPDATE ON users
    FOR EACH ROW
    EXECUTE FUNCTION trigger_set_updated_at();

-- products
CREATE TRIGGER set_updated_at_products
    BEFORE UPDATE ON products
    FOR EACH ROW
    EXECUTE FUNCTION trigger_set_updated_at();

-- orders
CREATE TRIGGER set_updated_at_orders
    BEFORE UPDATE ON orders
    FOR EACH ROW
    EXECUTE FUNCTION trigger_set_updated_at();

-- custom_requests
CREATE TRIGGER set_updated_at_custom_requests
    BEFORE UPDATE ON custom_requests
    FOR EACH ROW
    EXECUTE FUNCTION trigger_set_updated_at();


-- ТРИГЕР: автоматичний розрахунок subtotal в order_items
CREATE OR REPLACE FUNCTION trigger_calc_order_item_subtotal()
RETURNS TRIGGER AS $$
BEGIN
    NEW.subtotal = NEW.unit_price * NEW.quantity;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER calc_order_item_subtotal
    BEFORE INSERT OR UPDATE OF unit_price, quantity ON order_items
    FOR EACH ROW
    EXECUTE FUNCTION trigger_calc_order_item_subtotal();

-- ТРИГЕР: перерахунок total_amount в orders після зміни items
CREATE OR REPLACE FUNCTION trigger_recalc_order_total()
RETURNS TRIGGER AS $$
DECLARE
    v_subtotal NUMERIC(10,2);
BEGIN
    SELECT COALESCE(SUM(subtotal), 0)
    INTO v_subtotal
    FROM order_items
    WHERE order_id = COALESCE(NEW.order_id, OLD.order_id);

    UPDATE orders
    SET
        subtotal     = v_subtotal,
        total_amount = v_subtotal - COALESCE(discount_amount, 0) + COALESCE(shipping_cost, 0),
        updated_at   = NOW()
    WHERE id = COALESCE(NEW.order_id, OLD.order_id);

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER recalc_order_total_on_insert
    AFTER INSERT OR UPDATE OR DELETE ON order_items
    FOR EACH ROW
    EXECUTE FUNCTION trigger_recalc_order_total();

-- ТРИГЕР: один default address на користувача
-- Якщо встановлюємо нову адресу як default — знімаємо з інших
CREATE OR REPLACE FUNCTION trigger_single_default_address()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.is_default = TRUE THEN
        UPDATE user_addresses
        SET is_default = FALSE
        WHERE user_id = NEW.user_id
          AND id <> NEW.id;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER single_default_address
    BEFORE INSERT OR UPDATE OF is_default ON user_addresses
    FOR EACH ROW
    WHEN (NEW.is_default = TRUE)
    EXECUTE FUNCTION trigger_single_default_address();

-- ТРИГЕР: лічильник використання промокоду
CREATE OR REPLACE FUNCTION trigger_increment_promo_usage()
RETURNS TRIGGER AS $$
BEGIN
    -- При встановленні promo_code_id — збільшити лічильник
    IF NEW.promo_code_id IS NOT NULL AND
       (OLD.promo_code_id IS NULL OR OLD.promo_code_id <> NEW.promo_code_id) THEN
        UPDATE promo_codes
        SET used_count = used_count + 1
        WHERE id = NEW.promo_code_id;
    END IF;

    -- При знятті promo_code_id — зменшити лічильник
    IF OLD.promo_code_id IS NOT NULL AND NEW.promo_code_id IS NULL THEN
        UPDATE promo_codes
        SET used_count = GREATEST(used_count - 1, 0)
        WHERE id = OLD.promo_code_id;
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER increment_promo_usage
    AFTER UPDATE OF promo_code_id ON orders
    FOR EACH ROW
    EXECUTE FUNCTION trigger_increment_promo_usage();

-- ТРИГЕР: зменшення stock_qty при підтвердженні замовлення
CREATE OR REPLACE FUNCTION trigger_reserve_stock()
RETURNS TRIGGER AS $$
BEGIN
    -- Коли статус змінюється на 'confirmed' — резервуємо товар
    IF NEW.status = 'confirmed' AND OLD.status = 'new' THEN
        UPDATE products p
        SET stock_qty = p.stock_qty - oi.quantity
        FROM order_items oi
        WHERE oi.order_id = NEW.id
          AND oi.product_id = p.id;

        -- Якщо є варіант — зменшуємо і там
        UPDATE product_variants pv
        SET stock_qty = pv.stock_qty - oi.quantity
        FROM order_items oi
        WHERE oi.order_id = NEW.id
          AND oi.variant_id = pv.id;
    END IF;

    -- Коли замовлення скасовується — повертаємо товар
    IF NEW.status = 'cancelled' AND OLD.status IN ('confirmed', 'in_production') THEN
        UPDATE products p
        SET stock_qty = p.stock_qty + oi.quantity
        FROM order_items oi
        WHERE oi.order_id = NEW.id
          AND oi.product_id = p.id;

        UPDATE product_variants pv
        SET stock_qty = pv.stock_qty + oi.quantity
        FROM order_items oi
        WHERE oi.order_id = NEW.id
          AND oi.variant_id = pv.id;
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER reserve_stock_on_confirm
    AFTER UPDATE OF status ON orders
    FOR EACH ROW
    EXECUTE FUNCTION trigger_reserve_stock();


-- перевірка: промокод не прострочений і не вичерпаний
CREATE OR REPLACE FUNCTION trigger_validate_promo_code()
RETURNS TRIGGER AS $$
DECLARE
    v_promo promo_codes%ROWTYPE;
BEGIN
    IF NEW.promo_code_id IS NOT NULL THEN
        SELECT * INTO v_promo FROM promo_codes WHERE id = NEW.promo_code_id;

        IF NOT FOUND THEN
            RAISE EXCEPTION 'Промокод не знайдено';
        END IF;

        IF NOT v_promo.is_active THEN
            RAISE EXCEPTION 'Промокод неактивний';
        END IF;

        IF v_promo.expires_at IS NOT NULL AND v_promo.expires_at < NOW() THEN
            RAISE EXCEPTION 'Термін дії промокоду вичерпано';
        END IF;

        IF v_promo.max_uses IS NOT NULL AND v_promo.used_count >= v_promo.max_uses THEN
            RAISE EXCEPTION 'Промокод вже використано максимальну кількість разів';
        END IF;
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER validate_promo_code
    BEFORE INSERT OR UPDATE OF promo_code_id ON orders
    FOR EACH ROW
    EXECUTE FUNCTION trigger_validate_promo_code();
