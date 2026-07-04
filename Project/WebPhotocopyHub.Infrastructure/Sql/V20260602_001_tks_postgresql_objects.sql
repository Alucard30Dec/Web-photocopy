-- WebPhotocopyHub PostgreSQL reporting and operations objects.
-- Version: 20260602_001 refreshed after the 20260704 canonical database scripts.
-- Runtime currently uses the EF/PostgreSQL app/system/audit schemas; do not treat the canonical SQL as an EF migration.
-- If you need the optional canonical objects, apply database/patches/V20260704_001_tks_canonical_webphotocopyhub.sql first.

CREATE SCHEMA IF NOT EXISTS reporting;
CREATE SCHEMA IF NOT EXISTS operations;
CREATE SCHEMA IF NOT EXISTS vi;

CREATE OR REPLACE VIEW reporting.application_users AS
SELECT
    u."Id" AS user_id,
    u."FullName" AS full_name,
    u."Email" AS email,
    u."PhoneNumber" AS phone_number,
    u."Address" AS address,
    COALESCE(SUM(w.balance), 0)::numeric(18,2) AS current_balance,
    u."IsActive" AS is_active,
    u."CreatedAt" AS created_at
FROM public."AspNetUsers" AS u
LEFT JOIN app.branch_wallets AS w
    ON w.user_id = u."Id"
GROUP BY
    u."Id",
    u."FullName",
    u."Email",
    u."PhoneNumber",
    u."Address",
    u."IsActive",
    u."CreatedAt";

CREATE OR REPLACE VIEW reporting.wallet_transactions AS
SELECT
    wt.id,
    wt.branch_id,
    wt.branch_wallet_id,
    wt.user_id,
    wt.transaction_type,
    wt.amount,
    wt.balance_before,
    wt.balance_after,
    wt.reference_type,
    wt.reference_id,
    wt.note,
    wt.idempotency_key,
    wt.performed_by_admin_id,
    wt.created_at,
    wt.updated_at
FROM app.wallet_transactions AS wt;

CREATE OR REPLACE VIEW reporting.top_up_requests AS
SELECT
    t.id,
    t.branch_id,
    t.user_id,
    t.amount,
    t.transfer_content,
    t.transaction_reference_code,
    t.create_idempotency_key,
    t.last_review_idempotency_key,
    t.channel,
    t.proof_file_id,
    t.status,
    t.requires_admin_approval,
    t.reviewed_by_admin_id,
    t.reviewed_at,
    t.review_note,
    t.second_reviewed_by_admin_id,
    t.second_reviewed_at,
    t.second_review_note,
    t.approved_wallet_transaction_id,
    t.created_at,
    t.updated_at
FROM app.top_up_requests AS t;

CREATE OR REPLACE VIEW reporting.print_job_details AS
SELECT
    p.id,
    p.branch_id,
    b.name AS branch_name,
    p.user_id,
    p.uploaded_file_id,
    f.original_file_name,
    p.paper_size,
    p.print_side,
    p.color_mode,
    p.is_photo,
    p.copies,
    p.total_pages,
    p.notes,
    p.delivery_method,
    p.delivery_address,
    p.unit_price,
    p.sub_total,
    p.shipping_fee,
    p.total_amount,
    p.status,
    p.confirmed_by_operator_id,
    p.confirmed_at,
    p.assigned_operator_id,
    p.last_status_note,
    p.paid_at,
    p.paid_wallet_transaction_id,
    p.submit_idempotency_key,
    p.processed_by_admin_id,
    p.refunded_by_user_id,
    p.refunded_at,
    p.refund_reason,
    p.created_at,
    p.updated_at
FROM app.print_jobs AS p
JOIN app.shop_branches AS b
    ON b.id = p.branch_id
JOIN app.uploaded_files AS f
    ON f.id = p.uploaded_file_id;

CREATE OR REPLACE VIEW reporting.product_order_details AS
SELECT
    o.id AS order_id,
    o.branch_id,
    b.name AS branch_name,
    o.user_id,
    o.total_amount,
    o.delivery_method,
    o.delivery_address,
    o.notes,
    o.order_idempotency_key,
    o.status,
    o.processed_by_operator_id,
    o.processed_at,
    o.process_note,
    i.id AS item_id,
    i.product_id,
    p.name AS product_name,
    i.quantity,
    i.unit_price,
    i.line_total,
    o.created_at,
    o.updated_at
FROM app.product_orders AS o
JOIN app.shop_branches AS b
    ON b.id = o.branch_id
JOIN app.product_order_items AS i
    ON i.product_order_id = o.id
JOIN app.products AS p
    ON p.id = i.product_id;

CREATE OR REPLACE VIEW reporting.current_inventory AS
SELECT
    p.id AS product_id,
    p.branch_id,
    b.name AS branch_name,
    p.name,
    p.description,
    p.price,
    p.stock_quantity,
    p.image_url,
    p.is_active,
    p.created_at,
    p.updated_at
FROM app.products AS p
JOIN app.shop_branches AS b
    ON b.id = p.branch_id;

CREATE OR REPLACE VIEW reporting.support_service_order_details AS
SELECT
    o.id AS order_id,
    o.branch_id,
    b.name AS branch_name,
    o.user_id,
    o.support_service_id,
    s.name AS support_service_name,
    o.quantity,
    o.unit_price,
    o.total_amount,
    o.notes,
    o.order_idempotency_key,
    o.status,
    o.processed_by_operator_id,
    o.processed_at,
    o.process_note,
    o.created_at,
    o.updated_at
FROM app.support_service_orders AS o
JOIN app.shop_branches AS b
    ON b.id = o.branch_id
JOIN app.support_services AS s
    ON s.id = o.support_service_id;

CREATE OR REPLACE VIEW reporting.branch_wallet_reconciliation AS
SELECT
    w.id AS wallet_id,
    w.user_id,
    u."Email" AS email,
    w.branch_id,
    b.name AS branch_name,
    w.balance AS current_balance,
    COALESCE(SUM(t.amount), 0)::numeric(18,2) AS ledger_balance,
    (w.balance - COALESCE(SUM(t.amount), 0))::numeric(18,2) AS difference
FROM app.branch_wallets AS w
JOIN public."AspNetUsers" AS u
    ON u."Id" = w.user_id
JOIN app.shop_branches AS b
    ON b.id = w.branch_id
LEFT JOIN app.wallet_transactions AS t
    ON t.branch_wallet_id = w.id
GROUP BY
    w.id,
    w.user_id,
    u."Email",
    w.branch_id,
    b.name,
    w.balance;

CREATE OR REPLACE VIEW vi.doi_soat_vi_chi_nhanh AS
SELECT
    wallet_id AS ma_vi,
    user_id AS ma_nguoi_dung,
    email AS email,
    branch_id AS ma_chi_nhanh,
    branch_name AS ten_chi_nhanh,
    current_balance AS so_du_hien_tai,
    ledger_balance AS so_du_theo_giao_dich,
    difference AS chenh_lech
FROM reporting.branch_wallet_reconciliation;

CREATE OR REPLACE VIEW vi.chi_tiet_don_in AS
SELECT
    id AS ma_don_in,
    branch_id AS ma_chi_nhanh,
    branch_name AS ten_chi_nhanh,
    user_id AS ma_nguoi_dung,
    original_file_name AS ten_file_goc,
    total_amount AS tong_tien,
    status AS trang_thai,
    created_at AS ngay_tao,
    updated_at AS ngay_cap_nhat
FROM reporting.print_job_details;

CREATE OR REPLACE VIEW vi.chi_tiet_don_hang AS
SELECT
    order_id AS ma_don_hang,
    branch_id AS ma_chi_nhanh,
    branch_name AS ten_chi_nhanh,
    user_id AS ma_nguoi_dung,
    product_id AS ma_san_pham,
    product_name AS ten_san_pham,
    quantity AS so_luong,
    line_total AS thanh_tien,
    status AS trang_thai,
    created_at AS ngay_tao
FROM reporting.product_order_details;

CREATE OR REPLACE VIEW vi.ton_kho_hien_tai AS
SELECT
    product_id AS ma_san_pham,
    branch_id AS ma_chi_nhanh,
    branch_name AS ten_chi_nhanh,
    name AS ten_san_pham,
    stock_quantity AS ton_kho,
    is_active AS dang_hoat_dong,
    updated_at AS ngay_cap_nhat
FROM reporting.current_inventory;

CREATE OR REPLACE FUNCTION operations.get_branch_wallet_balance(
    p_user_id varchar,
    p_branch_id uuid
)
RETURNS numeric(18,2)
LANGUAGE sql
STABLE
SECURITY INVOKER
AS $$
    SELECT COALESCE((
        SELECT w.balance
        FROM app.branch_wallets AS w
        WHERE w.user_id = p_user_id
            AND w.branch_id = p_branch_id
            AND w.is_deleted = 0
    ), 0)::numeric(18,2);
$$;

CREATE OR REPLACE FUNCTION operations.reconcile_branch_wallet(
    p_branch_id uuid
)
RETURNS TABLE (
    wallet_id uuid,
    user_id varchar,
    branch_id uuid,
    current_balance numeric(18,2),
    ledger_balance numeric(18,2),
    difference numeric(18,2)
)
LANGUAGE sql
STABLE
SECURITY INVOKER
AS $$
    SELECT
        r.wallet_id,
        r.user_id,
        r.branch_id,
        r.current_balance,
        r.ledger_balance,
        r.difference
    FROM reporting.branch_wallet_reconciliation AS r
    WHERE r.branch_id = p_branch_id
    ORDER BY abs(r.difference) DESC, r.user_id;
$$;

CREATE OR REPLACE FUNCTION vi.lay_so_du_vi_chi_nhanh(
    p_nguoi_dung_id varchar,
    p_chi_nhanh_id uuid
)
RETURNS numeric(18,2)
LANGUAGE sql
STABLE
SECURITY INVOKER
AS $$
    SELECT operations.get_branch_wallet_balance(
        p_nguoi_dung_id,
        p_chi_nhanh_id
    );
$$;

CREATE OR REPLACE FUNCTION vi.doi_soat_vi_chi_nhanh(
    p_chi_nhanh_id uuid
)
RETURNS TABLE (
    ma_vi uuid,
    ma_nguoi_dung varchar,
    ma_chi_nhanh uuid,
    so_du_hien_tai numeric(18,2),
    so_du_theo_giao_dich numeric(18,2),
    chenh_lech numeric(18,2)
)
LANGUAGE sql
STABLE
SECURITY INVOKER
AS $$
    SELECT
        wallet_id AS ma_vi,
        user_id AS ma_nguoi_dung,
        branch_id AS ma_chi_nhanh,
        current_balance AS so_du_hien_tai,
        ledger_balance AS so_du_theo_giao_dich,
        difference AS chenh_lech
    FROM operations.reconcile_branch_wallet(p_chi_nhanh_id);
$$;

COMMENT ON VIEW reporting.branch_wallet_reconciliation IS
    'Branch wallet reconciliation - Đối soát ví theo chi nhánh';
COMMENT ON VIEW reporting.print_job_details IS
    'Print job details - Chi tiết đơn in';
COMMENT ON VIEW reporting.product_order_details IS
    'Product order details - Chi tiết đơn hàng sản phẩm';
COMMENT ON VIEW reporting.current_inventory IS
    'Current inventory - Tồn kho hiện tại';
COMMENT ON FUNCTION operations.get_branch_wallet_balance(varchar, uuid) IS
    'Get branch wallet balance - Lấy số dư ví theo người dùng và chi nhánh';
COMMENT ON FUNCTION operations.reconcile_branch_wallet(uuid) IS
    'Reconcile branch wallet - Đối soát ví theo chi nhánh';
