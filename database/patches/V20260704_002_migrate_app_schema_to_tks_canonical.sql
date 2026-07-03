-- V20260704_002_migrate_app_schema_to_tks_canonical.sql
-- Migrate the current WebPhotocopyHub app/system/audit schemas into the
-- TKS canonical public tables created by V20260704_001.

BEGIN;

SET LOCAL search_path = public;

DO $$
BEGIN
    IF to_regclass('app.shop_branches') IS NULL THEN
        RAISE EXCEPTION 'Missing source table app.shop_branches. Apply or restore the current WebPhotocopyHub schema before running this migration.';
    END IF;

    IF to_regclass('public."AspNetUsers"') IS NULL THEN
        RAISE EXCEPTION 'Missing source table public."AspNetUsers". Apply or restore Identity schema before running this migration.';
    END IF;

    IF to_regclass('app.branch_wallets') IS NULL THEN
        RAISE EXCEPTION 'Missing source table app.branch_wallets. Apply or restore the current WebPhotocopyHub schema before running this migration.';
    END IF;

    IF to_regclass('system.system_functions') IS NULL THEN
        RAISE EXCEPTION 'Missing source table system.system_functions. Apply or restore the current WebPhotocopyHub schema before running this migration.';
    END IF;

    IF to_regclass('audit.audit_logs') IS NULL THEN
        RAISE EXCEPTION 'Missing source table audit.audit_logs. Apply or restore the current WebPhotocopyHub schema before running this migration.';
    END IF;
END;
$$;

CREATE TEMP VIEW legacy_branches AS
SELECT
    id AS "Id",
    code AS "Code",
    slug AS "Slug",
    name AS "Name",
    address AS "Address",
    phone_number AS "PhoneNumber",
    email AS "Email",
    open_hours AS "OpenHours",
    short_description AS "ShortDescription",
    customer_note AS "CustomerNote",
    popular_services AS "PopularServices",
    quick_options AS "QuickOptions",
    is_active AS "IsActive",
    is_accepting_orders AS "IsAcceptingOrders",
    created_at AS "CreatedAt",
    updated_at AS "UpdatedAt"
FROM app.shop_branches
WHERE is_deleted = 0;

CREATE TEMP VIEW legacy_application_role_profiles AS
SELECT
    role_id AS "RoleId",
    display_name AS "DisplayName",
    description AS "Description",
    is_system_role AS "IsSystemRole",
    is_active AS "IsActive",
    created_at AS "CreatedAt",
    updated_at AS "UpdatedAt"
FROM system.application_role_profiles;

CREATE TEMP VIEW legacy_system_functions AS
SELECT
    id AS "Id",
    code AS "Code",
    name AS "Name",
    description AS "Description",
    parent_id AS "ParentId",
    area AS "Area",
    controller AS "Controller",
    action AS "Action",
    icon_key AS "IconKey",
    required_branch_feature_code AS "RequiredBranchFeatureCode",
    sort_order AS "SortOrder",
    requires_branch_selection AS "RequiresBranchSelection",
    is_menu_item AS "IsMenuItem",
    is_active AS "IsActive",
    is_system_function AS "IsSystemFunction",
    supports_view AS "SupportsView",
    supports_create AS "SupportsCreate",
    supports_edit AS "SupportsEdit",
    supports_delete AS "SupportsDelete",
    supports_export AS "SupportsExport",
    created_at AS "CreatedAt",
    updated_at AS "UpdatedAt"
FROM system.system_functions
WHERE is_deleted = 0;

CREATE TEMP VIEW legacy_role_function_permissions AS
SELECT
    role_id AS "RoleId",
    system_function_id AS "SystemFunctionId",
    can_view AS "CanView",
    can_create AS "CanCreate",
    can_edit AS "CanEdit",
    can_delete AS "CanDelete",
    can_export AS "CanExport"
FROM system.role_function_permissions;

CREATE TEMP VIEW legacy_branch_features AS
SELECT
    branch_id AS "BranchId",
    feature_code AS "FeatureCode",
    is_enabled AS "IsEnabled",
    updated_at AS "UpdatedAt",
    updated_by_user_id AS "UpdatedByUserId"
FROM app.branch_features;

CREATE TEMP VIEW legacy_branch_roles AS
SELECT
    id AS "Id",
    branch_id AS "BranchId",
    name AS "Name",
    description AS "Description",
    is_system_role AS "IsSystemRole",
    is_active AS "IsActive",
    created_at AS "CreatedAt",
    updated_at AS "UpdatedAt"
FROM app.branch_roles
WHERE is_deleted = 0;

CREATE TEMP VIEW legacy_branch_role_permissions AS
SELECT
    branch_role_id AS "BranchRoleId",
    permission_code AS "PermissionCode"
FROM app.branch_role_permissions;

CREATE TEMP VIEW legacy_user_branch_memberships AS
SELECT
    id AS "Id",
    user_id AS "UserId",
    branch_id AS "BranchId",
    branch_role_id AS "BranchRoleId",
    is_primary AS "IsPrimary",
    is_active AS "IsActive",
    assigned_by_user_id AS "AssignedByUserId",
    created_at AS "CreatedAt",
    updated_at AS "UpdatedAt"
FROM app.user_branch_memberships
WHERE is_deleted = 0;

CREATE TEMP VIEW legacy_pricing_rules AS
SELECT
    id AS "Id",
    branch_id AS "BranchId",
    paper_size AS "PaperSize",
    print_side AS "PrintSide",
    color_mode AS "ColorMode",
    is_photo AS "IsPhoto",
    unit_price AS "UnitPrice",
    is_active AS "IsActive",
    created_at AS "CreatedAt",
    updated_at AS "UpdatedAt"
FROM app.pricing_rules
WHERE is_deleted = 0;

CREATE TEMP VIEW legacy_products AS
SELECT
    id AS "Id",
    branch_id AS "BranchId",
    name AS "Name",
    description AS "Description",
    price AS "Price",
    stock_quantity AS "StockQuantity",
    image_url AS "ImageUrl",
    is_active AS "IsActive",
    created_at AS "CreatedAt",
    updated_at AS "UpdatedAt"
FROM app.products
WHERE is_deleted = 0;

CREATE TEMP VIEW legacy_support_services AS
SELECT
    id AS "Id",
    branch_id AS "BranchId",
    name AS "Name",
    description AS "Description",
    unit_price AS "UnitPrice",
    fee_type AS "FeeType",
    is_active AS "IsActive",
    created_at AS "CreatedAt",
    updated_at AS "UpdatedAt"
FROM app.support_services
WHERE is_deleted = 0;

CREATE TEMP VIEW legacy_uploaded_files AS
SELECT
    id AS "Id",
    branch_id AS "BranchId",
    owner_user_id AS "OwnerUserId",
    original_file_name AS "OriginalFileName",
    stored_file_name AS "StoredFileName",
    relative_path AS "RelativePath",
    size AS "Size",
    content_type AS "ContentType",
    is_for_print_job AS "IsForPrintJob",
    created_at AS "CreatedAt",
    updated_at AS "UpdatedAt"
FROM app.uploaded_files
WHERE is_deleted = 0;

CREATE TEMP VIEW legacy_branch_wallets AS
SELECT
    id AS "Id",
    branch_id AS "BranchId",
    user_id AS "UserId",
    balance AS "Balance",
    version AS "Version",
    created_at AS "CreatedAt",
    updated_at AS "UpdatedAt"
FROM app.branch_wallets
WHERE is_deleted = 0;

CREATE TEMP VIEW legacy_top_up_requests AS
SELECT
    id AS "Id",
    branch_id AS "BranchId",
    user_id AS "UserId",
    amount AS "Amount",
    transfer_content AS "TransferContent",
    transaction_reference_code AS "TransactionReferenceCode",
    create_idempotency_key AS "CreateIdempotencyKey",
    last_review_idempotency_key AS "LastReviewIdempotencyKey",
    channel AS "Channel",
    proof_file_id AS "ProofFileId",
    status AS "Status",
    requires_admin_approval AS "RequiresAdminApproval",
    reviewed_by_admin_id AS "ReviewedByAdminId",
    reviewed_at AS "ReviewedAt",
    review_note AS "ReviewNote",
    second_reviewed_by_admin_id AS "SecondReviewedByAdminId",
    second_reviewed_at AS "SecondReviewedAt",
    second_review_note AS "SecondReviewNote",
    approved_wallet_transaction_id AS "ApprovedWalletTransactionId",
    created_at AS "CreatedAt",
    updated_at AS "UpdatedAt"
FROM app.top_up_requests
WHERE is_deleted = 0;

CREATE TEMP VIEW legacy_print_jobs AS
SELECT
    id AS "Id",
    branch_id AS "BranchId",
    user_id AS "UserId",
    uploaded_file_id AS "UploadedFileId",
    paper_size AS "PaperSize",
    print_side AS "PrintSide",
    color_mode AS "ColorMode",
    is_photo AS "IsPhoto",
    copies AS "Copies",
    total_pages AS "TotalPages",
    notes AS "Notes",
    delivery_method AS "DeliveryMethod",
    delivery_address AS "DeliveryAddress",
    unit_price AS "UnitPrice",
    sub_total AS "SubTotal",
    shipping_fee AS "ShippingFee",
    total_amount AS "TotalAmount",
    status AS "Status",
    confirmed_by_operator_id AS "ConfirmedByOperatorId",
    confirmed_at AS "ConfirmedAt",
    assigned_operator_id AS "AssignedOperatorId",
    last_status_note AS "LastStatusNote",
    paid_at AS "PaidAt",
    paid_wallet_transaction_id AS "PaidWalletTransactionId",
    submit_idempotency_key AS "SubmitIdempotencyKey",
    processed_by_admin_id AS "ProcessedByAdminId",
    refunded_by_user_id AS "RefundedByUserId",
    refunded_at AS "RefundedAt",
    refund_reason AS "RefundReason",
    created_at AS "CreatedAt",
    updated_at AS "UpdatedAt"
FROM app.print_jobs
WHERE is_deleted = 0;

CREATE TEMP VIEW legacy_product_orders AS
SELECT
    id AS "Id",
    branch_id AS "BranchId",
    user_id AS "UserId",
    total_amount AS "TotalAmount",
    delivery_method AS "DeliveryMethod",
    delivery_address AS "DeliveryAddress",
    notes AS "Notes",
    order_idempotency_key AS "OrderIdempotencyKey",
    status AS "Status",
    processed_by_operator_id AS "ProcessedByOperatorId",
    processed_at AS "ProcessedAt",
    process_note AS "ProcessNote",
    created_at AS "CreatedAt",
    updated_at AS "UpdatedAt"
FROM app.product_orders
WHERE is_deleted = 0;

CREATE TEMP VIEW legacy_product_order_items AS
SELECT
    id AS "Id",
    product_order_id AS "ProductOrderId",
    product_id AS "ProductId",
    quantity AS "Quantity",
    unit_price AS "UnitPrice",
    line_total AS "LineTotal",
    created_at AS "CreatedAt",
    updated_at AS "UpdatedAt"
FROM app.product_order_items
WHERE is_deleted = 0;

CREATE TEMP VIEW legacy_support_service_orders AS
SELECT
    id AS "Id",
    branch_id AS "BranchId",
    user_id AS "UserId",
    support_service_id AS "SupportServiceId",
    quantity AS "Quantity",
    unit_price AS "UnitPrice",
    total_amount AS "TotalAmount",
    notes AS "Notes",
    order_idempotency_key AS "OrderIdempotencyKey",
    status AS "Status",
    processed_by_operator_id AS "ProcessedByOperatorId",
    processed_at AS "ProcessedAt",
    process_note AS "ProcessNote",
    created_at AS "CreatedAt",
    updated_at AS "UpdatedAt"
FROM app.support_service_orders
WHERE is_deleted = 0;

CREATE TEMP VIEW legacy_product_stock_movements AS
SELECT
    id AS "Id",
    branch_id AS "BranchId",
    product_id AS "ProductId",
    actor_user_id AS "ActorUserId",
    movement_type AS "MovementType",
    quantity_changed AS "QuantityChanged",
    stock_before AS "StockBefore",
    stock_after AS "StockAfter",
    note AS "Note",
    created_at AS "CreatedAt",
    updated_at AS "UpdatedAt"
FROM app.product_stock_movements
WHERE is_deleted = 0;

CREATE TEMP VIEW legacy_wallet_transactions AS
SELECT
    id AS "Id",
    branch_id AS "BranchId",
    branch_wallet_id AS "BranchWalletId",
    user_id AS "UserId",
    transaction_type AS "TransactionType",
    amount AS "Amount",
    balance_before AS "BalanceBefore",
    balance_after AS "BalanceAfter",
    reference_type AS "ReferenceType",
    reference_id AS "ReferenceId",
    note AS "Note",
    idempotency_key AS "IdempotencyKey",
    performed_by_admin_id AS "PerformedByAdminId",
    created_at AS "CreatedAt",
    updated_at AS "UpdatedAt"
FROM app.wallet_transactions
WHERE is_deleted = 0;

CREATE TEMP VIEW legacy_audit_logs AS
SELECT
    id AS "Id",
    actor_user_id AS "ActorUserId",
    action AS "Action",
    entity_name AS "EntityName",
    entity_id AS "EntityId",
    details AS "Details",
    ip_address AS "IpAddress",
    previous_hash AS "PreviousHash",
    record_hash AS "RecordHash",
    created_at AS "CreatedAt",
    updated_at AS "UpdatedAt"
FROM audit.audit_logs
WHERE is_deleted = 0;

INSERT INTO tbl_dm_chi_nhanh
(
    ma_chi_nhanh,
    slug,
    ten_chi_nhanh,
    dia_chi,
    dien_thoai,
    email,
    gio_mo_cua,
    mo_ta_ngan,
    ghi_chu_khach_hang,
    dich_vu_pho_bien_text,
    tuy_chon_nhanh_text,
    is_hoat_dong,
    is_nhan_don,
    legacy_uuid,
    created,
    last_updated,
    created_by_function,
    last_updated_by_function
)
SELECT
    a."Code",
    a."Slug",
    a."Name",
    a."Address",
    a."PhoneNumber",
    a."Email",
    a."OpenHours",
    a."ShortDescription",
    a."CustomerNote",
    a."PopularServices",
    a."QuickOptions",
    CASE WHEN a."IsActive" THEN 1 ELSE 0 END,
    CASE WHEN a."IsAcceptingOrders" THEN 1 ELSE 0 END,
    a."Id",
    a."CreatedAt",
    COALESCE(a."UpdatedAt", a."CreatedAt"),
    'V20260704_002_migrate',
    'V20260704_002_migrate'
FROM legacy_branches a
ON CONFLICT (legacy_uuid) WHERE legacy_uuid IS NOT NULL DO UPDATE
SET ma_chi_nhanh = EXCLUDED.ma_chi_nhanh,
    slug = EXCLUDED.slug,
    ten_chi_nhanh = EXCLUDED.ten_chi_nhanh,
    dia_chi = EXCLUDED.dia_chi,
    dien_thoai = EXCLUDED.dien_thoai,
    email = EXCLUDED.email,
    is_hoat_dong = EXCLUDED.is_hoat_dong,
    is_nhan_don = EXCLUDED.is_nhan_don,
    last_updated_by_function = EXCLUDED.last_updated_by_function;

INSERT INTO tbl_sys_thanh_vien
(
    ma_dang_nhap,
    email,
    ho_ten,
    dia_chi,
    dien_thoai,
    current_balance_legacy,
    is_hoat_dong,
    identity_user_id,
    legacy_id,
    created,
    last_updated,
    created_by_function,
    last_updated_by_function
)
SELECT
    COALESCE(a."UserName", a."Email", a."Id"),
    a."Email",
    a."FullName",
    a."Address",
    a."PhoneNumber",
    a."CurrentBalance",
    CASE WHEN a."IsActive" THEN 1 ELSE 0 END,
    a."Id",
    a."Id",
    a."CreatedAt",
    a."CreatedAt",
    'V20260704_002_migrate',
    'V20260704_002_migrate'
FROM public."AspNetUsers" a
ON CONFLICT (identity_user_id) WHERE identity_user_id IS NOT NULL DO UPDATE
SET ma_dang_nhap = EXCLUDED.ma_dang_nhap,
    email = EXCLUDED.email,
    ho_ten = EXCLUDED.ho_ten,
    dia_chi = EXCLUDED.dia_chi,
    dien_thoai = EXCLUDED.dien_thoai,
    current_balance_legacy = EXCLUDED.current_balance_legacy,
    is_hoat_dong = EXCLUDED.is_hoat_dong,
    last_updated_by_function = EXCLUDED.last_updated_by_function;

INSERT INTO tbl_sys_nhom_thanh_vien
(
    ma_nhom,
    ten_nhom,
    mo_ta,
    is_system_role,
    is_hoat_dong,
    identity_role_id,
    created,
    last_updated,
    created_by_function,
    last_updated_by_function
)
SELECT
    COALESCE(a."Name", a."Id"),
    COALESCE(p."DisplayName", a."Name", a."Id"),
    p."Description",
    CASE WHEN COALESCE(p."IsSystemRole", false) THEN 1 ELSE 0 END,
    CASE WHEN COALESCE(p."IsActive", true) THEN 1 ELSE 0 END,
    a."Id",
    COALESCE(p."CreatedAt", CURRENT_TIMESTAMP),
    COALESCE(p."UpdatedAt", p."CreatedAt", CURRENT_TIMESTAMP),
    'V20260704_002_migrate',
    'V20260704_002_migrate'
FROM public."AspNetRoles" a
LEFT JOIN legacy_application_role_profiles p
    ON p."RoleId" = a."Id"
ON CONFLICT (identity_role_id) WHERE identity_role_id IS NOT NULL DO UPDATE
SET ma_nhom = EXCLUDED.ma_nhom,
    ten_nhom = EXCLUDED.ten_nhom,
    mo_ta = EXCLUDED.mo_ta,
    is_system_role = EXCLUDED.is_system_role,
    is_hoat_dong = EXCLUDED.is_hoat_dong,
    last_updated_by_function = EXCLUDED.last_updated_by_function;

INSERT INTO tbl_sys_nhom_thanh_vien_user
(
    nhom_thanh_vien_id,
    thanh_vien_id,
    created_by_function,
    last_updated_by_function
)
SELECT
    b.auto_id,
    c.auto_id,
    'V20260704_002_migrate',
    'V20260704_002_migrate'
FROM public."AspNetUserRoles" a
JOIN tbl_sys_nhom_thanh_vien b
    ON b.identity_role_id = a."RoleId"
JOIN tbl_sys_thanh_vien c
    ON c.identity_user_id = a."UserId"
WHERE NOT EXISTS (
    SELECT 1
    FROM tbl_sys_nhom_thanh_vien_user d
    WHERE d.deleted = 0
        AND d.nhom_thanh_vien_id = b.auto_id
        AND d.thanh_vien_id = c.auto_id
);

INSERT INTO tbl_sys_chuc_nang
(
    ma_chuc_nang,
    ten_chuc_nang,
    mo_ta,
    area,
    controller,
    action,
    icon_key,
    ma_tinh_nang_chi_nhanh,
    sort_order,
    is_chon_chi_nhanh,
    is_menu_item,
    is_hoat_dong,
    is_system_function,
    supports_view,
    supports_create,
    supports_edit,
    supports_delete,
    supports_export,
    legacy_uuid,
    created,
    last_updated,
    created_by_function,
    last_updated_by_function
)
SELECT
    a."Code",
    a."Name",
    a."Description",
    a."Area",
    a."Controller",
    a."Action",
    a."IconKey",
    a."RequiredBranchFeatureCode",
    a."SortOrder",
    CASE WHEN a."RequiresBranchSelection" THEN 1 ELSE 0 END,
    CASE WHEN a."IsMenuItem" THEN 1 ELSE 0 END,
    CASE WHEN a."IsActive" THEN 1 ELSE 0 END,
    CASE WHEN a."IsSystemFunction" THEN 1 ELSE 0 END,
    CASE WHEN a."SupportsView" THEN 1 ELSE 0 END,
    CASE WHEN a."SupportsCreate" THEN 1 ELSE 0 END,
    CASE WHEN a."SupportsEdit" THEN 1 ELSE 0 END,
    CASE WHEN a."SupportsDelete" THEN 1 ELSE 0 END,
    CASE WHEN a."SupportsExport" THEN 1 ELSE 0 END,
    a."Id",
    a."CreatedAt",
    COALESCE(a."UpdatedAt", a."CreatedAt"),
    'V20260704_002_migrate',
    'V20260704_002_migrate'
FROM legacy_system_functions a
ON CONFLICT (legacy_uuid) WHERE legacy_uuid IS NOT NULL DO UPDATE
SET ma_chuc_nang = EXCLUDED.ma_chuc_nang,
    ten_chuc_nang = EXCLUDED.ten_chuc_nang,
    mo_ta = EXCLUDED.mo_ta,
    area = EXCLUDED.area,
    controller = EXCLUDED.controller,
    action = EXCLUDED.action,
    icon_key = EXCLUDED.icon_key,
    ma_tinh_nang_chi_nhanh = EXCLUDED.ma_tinh_nang_chi_nhanh,
    sort_order = EXCLUDED.sort_order,
    is_chon_chi_nhanh = EXCLUDED.is_chon_chi_nhanh,
    is_menu_item = EXCLUDED.is_menu_item,
    is_hoat_dong = EXCLUDED.is_hoat_dong,
    is_system_function = EXCLUDED.is_system_function,
    supports_view = EXCLUDED.supports_view,
    supports_create = EXCLUDED.supports_create,
    supports_edit = EXCLUDED.supports_edit,
    supports_delete = EXCLUDED.supports_delete,
    supports_export = EXCLUDED.supports_export,
    last_updated_by_function = EXCLUDED.last_updated_by_function;

UPDATE tbl_sys_chuc_nang a
SET parent_id = c.auto_id,
    last_updated_by_function = 'V20260704_002_migrate_parent'
FROM legacy_system_functions b
JOIN tbl_sys_chuc_nang c
    ON c.legacy_uuid = b."ParentId"
WHERE a.legacy_uuid = b."Id"
    AND b."ParentId" IS NOT NULL;

INSERT INTO tbl_sys_phan_quyen_chuc_nang
(
    nhom_thanh_vien_id,
    chuc_nang_id,
    can_view,
    can_create,
    can_edit,
    can_delete,
    can_export,
    created_by_function,
    last_updated_by_function
)
SELECT
    b.auto_id,
    c.auto_id,
    CASE WHEN a."CanView" THEN 1 ELSE 0 END,
    CASE WHEN a."CanCreate" THEN 1 ELSE 0 END,
    CASE WHEN a."CanEdit" THEN 1 ELSE 0 END,
    CASE WHEN a."CanDelete" THEN 1 ELSE 0 END,
    CASE WHEN a."CanExport" THEN 1 ELSE 0 END,
    'V20260704_002_migrate',
    'V20260704_002_migrate'
FROM legacy_role_function_permissions a
JOIN tbl_sys_nhom_thanh_vien b
    ON b.identity_role_id = a."RoleId"
JOIN tbl_sys_chuc_nang c
    ON c.legacy_uuid = a."SystemFunctionId"
WHERE NOT EXISTS (
    SELECT 1
    FROM tbl_sys_phan_quyen_chuc_nang d
    WHERE d.deleted = 0
        AND d.nhom_thanh_vien_id = b.auto_id
        AND d.chuc_nang_id = c.auto_id
);

INSERT INTO tbl_sys_chi_nhanh_tinh_nang
(
    chi_nhanh_id,
    ma_tinh_nang,
    is_enabled,
    updated_by_thanh_vien_id,
    last_updated,
    created_by_function,
    last_updated_by_function
)
SELECT
    b.auto_id,
    a."FeatureCode",
    CASE WHEN a."IsEnabled" THEN 1 ELSE 0 END,
    c.auto_id,
    a."UpdatedAt",
    'V20260704_002_migrate',
    'V20260704_002_migrate'
FROM legacy_branch_features a
JOIN tbl_dm_chi_nhanh b
    ON b.legacy_uuid = a."BranchId"
LEFT JOIN tbl_sys_thanh_vien c
    ON c.identity_user_id = a."UpdatedByUserId"
WHERE NOT EXISTS (
    SELECT 1
    FROM tbl_sys_chi_nhanh_tinh_nang d
    WHERE d.deleted = 0
        AND d.chi_nhanh_id = b.auto_id
        AND lower(d.ma_tinh_nang) = lower(a."FeatureCode")
);

INSERT INTO tbl_sys_vai_tro_chi_nhanh
(
    chi_nhanh_id,
    ten_vai_tro,
    mo_ta,
    is_system_role,
    is_hoat_dong,
    legacy_uuid,
    created,
    last_updated,
    created_by_function,
    last_updated_by_function
)
SELECT
    b.auto_id,
    a."Name",
    a."Description",
    CASE WHEN a."IsSystemRole" THEN 1 ELSE 0 END,
    CASE WHEN a."IsActive" THEN 1 ELSE 0 END,
    a."Id",
    a."CreatedAt",
    COALESCE(a."UpdatedAt", a."CreatedAt"),
    'V20260704_002_migrate',
    'V20260704_002_migrate'
FROM legacy_branch_roles a
JOIN tbl_dm_chi_nhanh b
    ON b.legacy_uuid = a."BranchId"
ON CONFLICT (legacy_uuid) WHERE legacy_uuid IS NOT NULL DO UPDATE
SET ten_vai_tro = EXCLUDED.ten_vai_tro,
    mo_ta = EXCLUDED.mo_ta,
    is_system_role = EXCLUDED.is_system_role,
    is_hoat_dong = EXCLUDED.is_hoat_dong,
    last_updated_by_function = EXCLUDED.last_updated_by_function;

INSERT INTO tbl_sys_vai_tro_chi_nhanh_quyen
(
    vai_tro_chi_nhanh_id,
    ma_quyen,
    created_by_function,
    last_updated_by_function
)
SELECT
    b.auto_id,
    a."PermissionCode",
    'V20260704_002_migrate',
    'V20260704_002_migrate'
FROM legacy_branch_role_permissions a
JOIN tbl_sys_vai_tro_chi_nhanh b
    ON b.legacy_uuid = a."BranchRoleId"
WHERE NOT EXISTS (
    SELECT 1
    FROM tbl_sys_vai_tro_chi_nhanh_quyen c
    WHERE c.deleted = 0
        AND c.vai_tro_chi_nhanh_id = b.auto_id
        AND lower(c.ma_quyen) = lower(a."PermissionCode")
);

INSERT INTO tbl_sys_thanh_vien_chi_nhanh
(
    thanh_vien_id,
    chi_nhanh_id,
    vai_tro_chi_nhanh_id,
    is_primary,
    is_hoat_dong,
    assigned_by_thanh_vien_id,
    legacy_uuid,
    created,
    last_updated,
    created_by_function,
    last_updated_by_function
)
SELECT
    b.auto_id,
    c.auto_id,
    d.auto_id,
    CASE WHEN a."IsPrimary" THEN 1 ELSE 0 END,
    CASE WHEN a."IsActive" THEN 1 ELSE 0 END,
    e.auto_id,
    a."Id",
    a."CreatedAt",
    COALESCE(a."UpdatedAt", a."CreatedAt"),
    'V20260704_002_migrate',
    'V20260704_002_migrate'
FROM legacy_user_branch_memberships a
JOIN tbl_sys_thanh_vien b
    ON b.identity_user_id = a."UserId"
JOIN tbl_dm_chi_nhanh c
    ON c.legacy_uuid = a."BranchId"
JOIN tbl_sys_vai_tro_chi_nhanh d
    ON d.legacy_uuid = a."BranchRoleId"
LEFT JOIN tbl_sys_thanh_vien e
    ON e.identity_user_id = a."AssignedByUserId"
ON CONFLICT (legacy_uuid) WHERE legacy_uuid IS NOT NULL DO UPDATE
SET vai_tro_chi_nhanh_id = EXCLUDED.vai_tro_chi_nhanh_id,
    is_primary = EXCLUDED.is_primary,
    is_hoat_dong = EXCLUDED.is_hoat_dong,
    assigned_by_thanh_vien_id = EXCLUDED.assigned_by_thanh_vien_id,
    last_updated_by_function = EXCLUDED.last_updated_by_function;

INSERT INTO tbl_dm_bang_gia_in
(
    chi_nhanh_id,
    kho_giay_id,
    kieu_in_id,
    mau_in_id,
    is_photo,
    don_gia,
    is_hoat_dong,
    legacy_uuid,
    created,
    last_updated,
    created_by_function,
    last_updated_by_function
)
SELECT
    b.auto_id,
    c.auto_id,
    d.auto_id,
    e.auto_id,
    CASE WHEN a."IsPhoto" THEN 1 ELSE 0 END,
    a."UnitPrice",
    CASE WHEN a."IsActive" THEN 1 ELSE 0 END,
    a."Id",
    a."CreatedAt",
    COALESCE(a."UpdatedAt", a."CreatedAt"),
    'V20260704_002_migrate',
    'V20260704_002_migrate'
FROM legacy_pricing_rules a
JOIN tbl_dm_chi_nhanh b
    ON b.legacy_uuid = a."BranchId"
JOIN tbl_dm_kho_giay c
    ON c.type_id = a."PaperSize"
JOIN tbl_dm_kieu_in d
    ON d.type_id = a."PrintSide"
JOIN tbl_dm_mau_in e
    ON e.type_id = a."ColorMode"
ON CONFLICT (legacy_uuid) WHERE legacy_uuid IS NOT NULL DO UPDATE
SET don_gia = EXCLUDED.don_gia,
    is_hoat_dong = EXCLUDED.is_hoat_dong,
    last_updated_by_function = EXCLUDED.last_updated_by_function;

INSERT INTO tbl_dm_san_pham
(
    chi_nhanh_id,
    ma_san_pham,
    ten_san_pham,
    mo_ta,
    don_gia,
    image_url,
    is_hoat_dong,
    legacy_uuid,
    created,
    last_updated,
    created_by_function,
    last_updated_by_function
)
SELECT
    b.auto_id,
    'SP-' || upper(substr(replace(a."Id"::text, '-', ''), 1, 8)),
    a."Name",
    a."Description",
    a."Price",
    a."ImageUrl",
    CASE WHEN a."IsActive" THEN 1 ELSE 0 END,
    a."Id",
    a."CreatedAt",
    COALESCE(a."UpdatedAt", a."CreatedAt"),
    'V20260704_002_migrate',
    'V20260704_002_migrate'
FROM legacy_products a
JOIN tbl_dm_chi_nhanh b
    ON b.legacy_uuid = a."BranchId"
ON CONFLICT (legacy_uuid) WHERE legacy_uuid IS NOT NULL DO UPDATE
SET ten_san_pham = EXCLUDED.ten_san_pham,
    mo_ta = EXCLUDED.mo_ta,
    don_gia = EXCLUDED.don_gia,
    image_url = EXCLUDED.image_url,
    is_hoat_dong = EXCLUDED.is_hoat_dong,
    last_updated_by_function = EXCLUDED.last_updated_by_function;

INSERT INTO tbl_dm_dich_vu_ho_tro
(
    chi_nhanh_id,
    ma_dich_vu,
    ten_dich_vu,
    mo_ta,
    don_gia,
    loai_phi_id,
    is_hoat_dong,
    legacy_uuid,
    created,
    last_updated,
    created_by_function,
    last_updated_by_function
)
SELECT
    b.auto_id,
    'DV-' || upper(substr(replace(a."Id"::text, '-', ''), 1, 8)),
    a."Name",
    a."Description",
    a."UnitPrice",
    a."FeeType",
    CASE WHEN a."IsActive" THEN 1 ELSE 0 END,
    a."Id",
    a."CreatedAt",
    COALESCE(a."UpdatedAt", a."CreatedAt"),
    'V20260704_002_migrate',
    'V20260704_002_migrate'
FROM legacy_support_services a
JOIN tbl_dm_chi_nhanh b
    ON b.legacy_uuid = a."BranchId"
ON CONFLICT (legacy_uuid) WHERE legacy_uuid IS NOT NULL DO UPDATE
SET ten_dich_vu = EXCLUDED.ten_dich_vu,
    mo_ta = EXCLUDED.mo_ta,
    don_gia = EXCLUDED.don_gia,
    loai_phi_id = EXCLUDED.loai_phi_id,
    is_hoat_dong = EXCLUDED.is_hoat_dong,
    last_updated_by_function = EXCLUDED.last_updated_by_function;

INSERT INTO tbl_xnk_ton_kho_san_pham
(
    chi_nhanh_id,
    san_pham_id,
    key_lo_hang,
    so_luong_ton,
    created_by_function,
    last_updated_by_function
)
SELECT
    c.auto_id,
    b.auto_id,
    '',
    a."StockQuantity",
    'V20260704_002_migrate',
    'V20260704_002_migrate'
FROM legacy_products a
JOIN tbl_dm_san_pham b
    ON b.legacy_uuid = a."Id"
JOIN tbl_dm_chi_nhanh c
    ON c.legacy_uuid = a."BranchId"
WHERE NOT EXISTS (
    SELECT 1
    FROM tbl_xnk_ton_kho_san_pham d
    WHERE d.deleted = 0
        AND d.chi_nhanh_id = c.auto_id
        AND d.san_pham_id = b.auto_id
        AND d.key_lo_hang = ''
        AND d.lpn_id = 0
        AND d.carton_id = 0
        AND d.vi_tri_id = 0
        AND d.trang_thai_lo_hang_id = 1
);

INSERT INTO tbl_tc_file_upload
(
    chi_nhanh_id,
    thanh_vien_id,
    ten_file_goc,
    ten_file_luu,
    duong_dan_tuong_doi,
    dung_luong_byte,
    content_type,
    is_file_don_in,
    owner_identity_user_id,
    legacy_uuid,
    created,
    last_updated,
    created_by_function,
    last_updated_by_function
)
SELECT
    b.auto_id,
    c.auto_id,
    a."OriginalFileName",
    a."StoredFileName",
    a."RelativePath",
    a."Size",
    a."ContentType",
    CASE WHEN a."IsForPrintJob" THEN 1 ELSE 0 END,
    a."OwnerUserId",
    a."Id",
    a."CreatedAt",
    COALESCE(a."UpdatedAt", a."CreatedAt"),
    'V20260704_002_migrate',
    'V20260704_002_migrate'
FROM legacy_uploaded_files a
JOIN tbl_dm_chi_nhanh b
    ON b.legacy_uuid = a."BranchId"
JOIN tbl_sys_thanh_vien c
    ON c.identity_user_id = a."OwnerUserId"
ON CONFLICT (legacy_uuid) WHERE legacy_uuid IS NOT NULL DO UPDATE
SET ten_file_goc = EXCLUDED.ten_file_goc,
    ten_file_luu = EXCLUDED.ten_file_luu,
    duong_dan_tuong_doi = EXCLUDED.duong_dan_tuong_doi,
    dung_luong_byte = EXCLUDED.dung_luong_byte,
    content_type = EXCLUDED.content_type,
    is_file_don_in = EXCLUDED.is_file_don_in,
    last_updated_by_function = EXCLUDED.last_updated_by_function;

INSERT INTO tbl_tc_yeu_cau_nap_tien
(
    chi_nhanh_id,
    thanh_vien_id,
    file_minh_chung_id,
    duyet_boi_thanh_vien_id,
    admin_duyet_boi_thanh_vien_id,
    so_tien,
    noi_dung_chuyen_khoan,
    ma_giao_dich,
    create_idempotency_key,
    last_review_idempotency_key,
    kenh_nap_id,
    trang_thai_id,
    is_can_admin_duyet,
    ngay_gio_duyet,
    ghi_chu_duyet,
    ngay_gio_admin_duyet,
    ghi_chu_admin_duyet,
    identity_user_id,
    legacy_uuid,
    created,
    last_updated,
    created_by_function,
    last_updated_by_function
)
SELECT
    b.auto_id,
    c.auto_id,
    d.auto_id,
    e.auto_id,
    f.auto_id,
    a."Amount",
    a."TransferContent",
    a."TransactionReferenceCode",
    a."CreateIdempotencyKey",
    a."LastReviewIdempotencyKey",
    a."Channel",
    a."Status",
    CASE WHEN a."RequiresAdminApproval" THEN 1 ELSE 0 END,
    a."ReviewedAt",
    a."ReviewNote",
    a."SecondReviewedAt",
    a."SecondReviewNote",
    a."UserId",
    a."Id",
    a."CreatedAt",
    COALESCE(a."UpdatedAt", a."CreatedAt"),
    'V20260704_002_migrate',
    'V20260704_002_migrate'
FROM legacy_top_up_requests a
JOIN tbl_dm_chi_nhanh b
    ON b.legacy_uuid = a."BranchId"
JOIN tbl_sys_thanh_vien c
    ON c.identity_user_id = a."UserId"
LEFT JOIN tbl_tc_file_upload d
    ON d.legacy_uuid = a."ProofFileId"
LEFT JOIN tbl_sys_thanh_vien e
    ON e.identity_user_id = a."ReviewedByAdminId"
LEFT JOIN tbl_sys_thanh_vien f
    ON f.identity_user_id = a."SecondReviewedByAdminId"
ON CONFLICT (legacy_uuid) WHERE legacy_uuid IS NOT NULL DO UPDATE
SET trang_thai_id = EXCLUDED.trang_thai_id,
    duyet_boi_thanh_vien_id = EXCLUDED.duyet_boi_thanh_vien_id,
    admin_duyet_boi_thanh_vien_id = EXCLUDED.admin_duyet_boi_thanh_vien_id,
    ghi_chu_duyet = EXCLUDED.ghi_chu_duyet,
    ghi_chu_admin_duyet = EXCLUDED.ghi_chu_admin_duyet,
    last_updated_by_function = EXCLUDED.last_updated_by_function;

INSERT INTO tbl_tc_don_in
(
    chi_nhanh_id,
    thanh_vien_id,
    file_upload_id,
    kho_giay_id,
    kieu_in_id,
    mau_in_id,
    phuong_thuc_nhan_id,
    xac_nhan_boi_thanh_vien_id,
    phan_cong_boi_thanh_vien_id,
    xu_ly_boi_thanh_vien_id,
    hoan_tien_boi_thanh_vien_id,
    is_photo,
    so_ban,
    so_trang,
    ghi_chu,
    dia_chi_giao,
    don_gia,
    thanh_tien,
    phi_giao_hang,
    tong_tien,
    trang_thai_id,
    ngay_gio_xac_nhan,
    ghi_chu_trang_thai_cuoi,
    ngay_gio_thanh_toan,
    submit_idempotency_key,
    ngay_gio_hoan_tien,
    ly_do_hoan_tien,
    identity_user_id,
    legacy_uuid,
    created,
    last_updated,
    created_by_function,
    last_updated_by_function
)
SELECT
    b.auto_id,
    c.auto_id,
    d.auto_id,
    e.auto_id,
    f.auto_id,
    g.auto_id,
    h.auto_id,
    i.auto_id,
    j.auto_id,
    k.auto_id,
    l.auto_id,
    CASE WHEN a."IsPhoto" THEN 1 ELSE 0 END,
    a."Copies",
    a."TotalPages",
    a."Notes",
    a."DeliveryAddress",
    a."UnitPrice",
    a."SubTotal",
    a."ShippingFee",
    a."TotalAmount",
    a."Status",
    a."ConfirmedAt",
    a."LastStatusNote",
    a."PaidAt",
    a."SubmitIdempotencyKey",
    a."RefundedAt",
    a."RefundReason",
    a."UserId",
    a."Id",
    a."CreatedAt",
    COALESCE(a."UpdatedAt", a."CreatedAt"),
    'V20260704_002_migrate',
    'V20260704_002_migrate'
FROM legacy_print_jobs a
JOIN tbl_dm_chi_nhanh b
    ON b.legacy_uuid = a."BranchId"
JOIN tbl_sys_thanh_vien c
    ON c.identity_user_id = a."UserId"
JOIN tbl_tc_file_upload d
    ON d.legacy_uuid = a."UploadedFileId"
JOIN tbl_dm_kho_giay e
    ON e.type_id = a."PaperSize"
JOIN tbl_dm_kieu_in f
    ON f.type_id = a."PrintSide"
JOIN tbl_dm_mau_in g
    ON g.type_id = a."ColorMode"
JOIN tbl_dm_phuong_thuc_nhan h
    ON h.type_id = a."DeliveryMethod"
LEFT JOIN tbl_sys_thanh_vien i
    ON i.identity_user_id = a."ConfirmedByOperatorId"
LEFT JOIN tbl_sys_thanh_vien j
    ON j.identity_user_id = a."AssignedOperatorId"
LEFT JOIN tbl_sys_thanh_vien k
    ON k.identity_user_id = a."ProcessedByAdminId"
LEFT JOIN tbl_sys_thanh_vien l
    ON l.identity_user_id = a."RefundedByUserId"
ON CONFLICT (legacy_uuid) WHERE legacy_uuid IS NOT NULL DO UPDATE
SET trang_thai_id = EXCLUDED.trang_thai_id,
    ghi_chu_trang_thai_cuoi = EXCLUDED.ghi_chu_trang_thai_cuoi,
    ngay_gio_thanh_toan = EXCLUDED.ngay_gio_thanh_toan,
    ngay_gio_hoan_tien = EXCLUDED.ngay_gio_hoan_tien,
    ly_do_hoan_tien = EXCLUDED.ly_do_hoan_tien,
    last_updated_by_function = EXCLUDED.last_updated_by_function;

INSERT INTO tbl_tc_don_hang_san_pham
(
    chi_nhanh_id,
    thanh_vien_id,
    xu_ly_boi_thanh_vien_id,
    phuong_thuc_nhan_id,
    tong_tien,
    dia_chi_giao,
    ghi_chu,
    order_idempotency_key,
    trang_thai_id,
    ngay_gio_xu_ly,
    ghi_chu_xu_ly,
    identity_user_id,
    legacy_uuid,
    created,
    last_updated,
    created_by_function,
    last_updated_by_function
)
SELECT
    b.auto_id,
    c.auto_id,
    d.auto_id,
    e.auto_id,
    a."TotalAmount",
    a."DeliveryAddress",
    a."Notes",
    a."OrderIdempotencyKey",
    a."Status",
    a."ProcessedAt",
    a."ProcessNote",
    a."UserId",
    a."Id",
    a."CreatedAt",
    COALESCE(a."UpdatedAt", a."CreatedAt"),
    'V20260704_002_migrate',
    'V20260704_002_migrate'
FROM legacy_product_orders a
JOIN tbl_dm_chi_nhanh b
    ON b.legacy_uuid = a."BranchId"
JOIN tbl_sys_thanh_vien c
    ON c.identity_user_id = a."UserId"
LEFT JOIN tbl_sys_thanh_vien d
    ON d.identity_user_id = a."ProcessedByOperatorId"
JOIN tbl_dm_phuong_thuc_nhan e
    ON e.type_id = a."DeliveryMethod"
ON CONFLICT (legacy_uuid) WHERE legacy_uuid IS NOT NULL DO UPDATE
SET trang_thai_id = EXCLUDED.trang_thai_id,
    ngay_gio_xu_ly = EXCLUDED.ngay_gio_xu_ly,
    ghi_chu_xu_ly = EXCLUDED.ghi_chu_xu_ly,
    last_updated_by_function = EXCLUDED.last_updated_by_function;

INSERT INTO tbl_tc_don_hang_san_pham_chi_tiet
(
    don_hang_san_pham_id,
    san_pham_id,
    so_luong,
    don_gia,
    thanh_tien,
    legacy_uuid,
    created,
    last_updated,
    created_by_function,
    last_updated_by_function
)
SELECT
    b.auto_id,
    c.auto_id,
    a."Quantity",
    a."UnitPrice",
    a."LineTotal",
    a."Id",
    a."CreatedAt",
    COALESCE(a."UpdatedAt", a."CreatedAt"),
    'V20260704_002_migrate',
    'V20260704_002_migrate'
FROM legacy_product_order_items a
JOIN tbl_tc_don_hang_san_pham b
    ON b.legacy_uuid = a."ProductOrderId"
JOIN tbl_dm_san_pham c
    ON c.legacy_uuid = a."ProductId"
ON CONFLICT (legacy_uuid) WHERE legacy_uuid IS NOT NULL DO UPDATE
SET so_luong = EXCLUDED.so_luong,
    don_gia = EXCLUDED.don_gia,
    thanh_tien = EXCLUDED.thanh_tien,
    last_updated_by_function = EXCLUDED.last_updated_by_function;

INSERT INTO tbl_tc_don_dich_vu_ho_tro
(
    chi_nhanh_id,
    thanh_vien_id,
    dich_vu_ho_tro_id,
    xu_ly_boi_thanh_vien_id,
    so_luong,
    don_gia,
    tong_tien,
    ghi_chu,
    order_idempotency_key,
    trang_thai_id,
    ngay_gio_xu_ly,
    ghi_chu_xu_ly,
    identity_user_id,
    legacy_uuid,
    created,
    last_updated,
    created_by_function,
    last_updated_by_function
)
SELECT
    b.auto_id,
    c.auto_id,
    d.auto_id,
    e.auto_id,
    a."Quantity",
    a."UnitPrice",
    a."TotalAmount",
    a."Notes",
    a."OrderIdempotencyKey",
    a."Status",
    a."ProcessedAt",
    a."ProcessNote",
    a."UserId",
    a."Id",
    a."CreatedAt",
    COALESCE(a."UpdatedAt", a."CreatedAt"),
    'V20260704_002_migrate',
    'V20260704_002_migrate'
FROM legacy_support_service_orders a
JOIN tbl_dm_chi_nhanh b
    ON b.legacy_uuid = a."BranchId"
JOIN tbl_sys_thanh_vien c
    ON c.identity_user_id = a."UserId"
JOIN tbl_dm_dich_vu_ho_tro d
    ON d.legacy_uuid = a."SupportServiceId"
LEFT JOIN tbl_sys_thanh_vien e
    ON e.identity_user_id = a."ProcessedByOperatorId"
ON CONFLICT (legacy_uuid) WHERE legacy_uuid IS NOT NULL DO UPDATE
SET trang_thai_id = EXCLUDED.trang_thai_id,
    ngay_gio_xu_ly = EXCLUDED.ngay_gio_xu_ly,
    ghi_chu_xu_ly = EXCLUDED.ghi_chu_xu_ly,
    last_updated_by_function = EXCLUDED.last_updated_by_function;

INSERT INTO tbl_xnk_nhap_xuat_san_pham
(
    chi_nhanh_id,
    san_pham_id,
    actor_thanh_vien_id,
    ton_kho_san_pham_id,
    loai_nhap_xuat_id,
    so_luong_thay_doi,
    ton_truoc,
    ton_sau,
    ghi_chu,
    legacy_uuid,
    created,
    last_updated,
    created_by_function,
    last_updated_by_function
)
SELECT
    b.auto_id,
    c.auto_id,
    d.auto_id,
    e.auto_id,
    a."MovementType",
    a."QuantityChanged",
    a."StockBefore",
    a."StockAfter",
    a."Note",
    a."Id",
    a."CreatedAt",
    COALESCE(a."UpdatedAt", a."CreatedAt"),
    'V20260704_002_migrate',
    'V20260704_002_migrate'
FROM legacy_product_stock_movements a
JOIN tbl_dm_chi_nhanh b
    ON b.legacy_uuid = a."BranchId"
JOIN tbl_dm_san_pham c
    ON c.legacy_uuid = a."ProductId"
JOIN tbl_sys_thanh_vien d
    ON d.identity_user_id = a."ActorUserId"
LEFT JOIN tbl_xnk_ton_kho_san_pham e
    ON e.deleted = 0
    AND e.chi_nhanh_id = b.auto_id
    AND e.san_pham_id = c.auto_id
    AND e.key_lo_hang = ''
    AND e.lpn_id = 0
    AND e.carton_id = 0
    AND e.vi_tri_id = 0
    AND e.trang_thai_lo_hang_id = 1
ON CONFLICT (legacy_uuid) WHERE legacy_uuid IS NOT NULL DO UPDATE
SET so_luong_thay_doi = EXCLUDED.so_luong_thay_doi,
    ton_truoc = EXCLUDED.ton_truoc,
    ton_sau = EXCLUDED.ton_sau,
    ghi_chu = EXCLUDED.ghi_chu,
    last_updated_by_function = EXCLUDED.last_updated_by_function;

INSERT INTO tbl_xnk_nhap_xuat_san_pham
(
    chi_nhanh_id,
    san_pham_id,
    ton_kho_san_pham_id,
    loai_nhap_xuat_id,
    so_luong_thay_doi,
    ton_truoc,
    ton_sau,
    ref_type,
    ghi_chu,
    created_by_function,
    last_updated_by_function
)
SELECT
    a.chi_nhanh_id,
    a.san_pham_id,
    a.ton_kho_san_pham_id,
    1,
    a.chenh_lech,
    a.so_luong_theo_nhap_xuat,
    a.so_luong_ton,
    'MigrationOpeningStock',
    'Residual stock movement generated to preserve Product.StockQuantity during TKS canonical migration.',
    'V20260704_002_migrate',
    'V20260704_002_migrate'
FROM f2004_sp_sel_reconcile_ton_kho_san_pham() a
WHERE a.chenh_lech <> 0
    AND NOT EXISTS (
        SELECT 1
        FROM tbl_xnk_nhap_xuat_san_pham b
        WHERE b.deleted = 0
            AND b.ton_kho_san_pham_id = a.ton_kho_san_pham_id
            AND b.ref_type = 'MigrationOpeningStock'
    );

INSERT INTO tbl_tc_vi_chi_nhanh
(
    chi_nhanh_id,
    thanh_vien_id,
    so_du,
    version_no,
    identity_user_id,
    legacy_uuid,
    created,
    last_updated,
    created_by_function,
    last_updated_by_function
)
SELECT
    b.auto_id,
    c.auto_id,
    a."Balance",
    a."Version",
    a."UserId",
    a."Id",
    a."CreatedAt",
    COALESCE(a."UpdatedAt", a."CreatedAt"),
    'V20260704_002_migrate',
    'V20260704_002_migrate'
FROM legacy_branch_wallets a
JOIN tbl_dm_chi_nhanh b
    ON b.legacy_uuid = a."BranchId"
JOIN tbl_sys_thanh_vien c
    ON c.identity_user_id = a."UserId"
ON CONFLICT (legacy_uuid) WHERE legacy_uuid IS NOT NULL DO UPDATE
SET so_du = EXCLUDED.so_du,
    version_no = EXCLUDED.version_no,
    identity_user_id = EXCLUDED.identity_user_id,
    last_updated_by_function = EXCLUDED.last_updated_by_function;

WITH wallet_source AS
(
    SELECT a."BranchId" AS branch_legacy_uuid,
        a."UserId" AS identity_user_id
    FROM legacy_branch_wallets a
    UNION
    SELECT a."BranchId",
        a."UserId"
    FROM legacy_wallet_transactions a
    UNION
    SELECT a."BranchId",
        a."UserId"
    FROM legacy_top_up_requests a
    UNION
    SELECT a."BranchId",
        a."UserId"
    FROM legacy_print_jobs a
    UNION
    SELECT a."BranchId",
        a."UserId"
    FROM legacy_product_orders a
    UNION
    SELECT a."BranchId",
        a."UserId"
    FROM legacy_support_service_orders a
    UNION
    SELECT a."BranchId",
        a."UserId"
    FROM legacy_user_branch_memberships a
    UNION
    SELECT b.legacy_uuid,
        a.identity_user_id
    FROM tbl_sys_thanh_vien a
    CROSS JOIN (
        SELECT c.legacy_uuid
        FROM tbl_dm_chi_nhanh c
        WHERE c.deleted = 0
        ORDER BY c.auto_id
        LIMIT 1
    ) b
)
INSERT INTO tbl_tc_vi_chi_nhanh
(
    chi_nhanh_id,
    thanh_vien_id,
    so_du,
    identity_user_id,
    created_by_function,
    last_updated_by_function
)
SELECT
    b.auto_id,
    c.auto_id,
    0,
    a.identity_user_id,
    'V20260704_002_migrate',
    'V20260704_002_migrate'
FROM wallet_source a
JOIN tbl_dm_chi_nhanh b
    ON b.legacy_uuid = a.branch_legacy_uuid
JOIN tbl_sys_thanh_vien c
    ON c.identity_user_id = a.identity_user_id
WHERE NOT EXISTS (
    SELECT 1
    FROM tbl_tc_vi_chi_nhanh d
    WHERE d.deleted = 0
        AND d.chi_nhanh_id = b.auto_id
        AND d.thanh_vien_id = c.auto_id
);

INSERT INTO tbl_tc_giao_dich_vi
(
    chi_nhanh_id,
    vi_chi_nhanh_id,
    thanh_vien_id,
    thuc_hien_boi_thanh_vien_id,
    loai_giao_dich_id,
    so_tien,
    so_du_truoc,
    so_du_sau,
    ref_type,
    ref_id,
    ref_legacy_uuid,
    ghi_chu,
    idempotency_key,
    identity_user_id,
    performed_by_identity_user_id,
    legacy_uuid,
    created,
    last_updated,
    created_by_function,
    last_updated_by_function
)
SELECT
    b.auto_id,
    d.auto_id,
    c.auto_id,
    e.auto_id,
    a."TransactionType",
    a."Amount",
    a."BalanceBefore",
    a."BalanceAfter",
    a."ReferenceType",
    CASE
        WHEN a."ReferenceType" = 'TopUpRequest' THEN f.auto_id
        WHEN a."ReferenceType" = 'PrintJob' THEN g.auto_id
        WHEN a."ReferenceType" = 'ProductOrder' THEN h.auto_id
        WHEN a."ReferenceType" = 'SupportServiceOrder' THEN i.auto_id
        ELSE NULL
    END,
    a."ReferenceId",
    a."Note",
    a."IdempotencyKey",
    a."UserId",
    a."PerformedByAdminId",
    a."Id",
    a."CreatedAt",
    COALESCE(a."UpdatedAt", a."CreatedAt"),
    'V20260704_002_migrate',
    'V20260704_002_migrate'
FROM legacy_wallet_transactions a
JOIN tbl_dm_chi_nhanh b
    ON b.legacy_uuid = a."BranchId"
JOIN tbl_sys_thanh_vien c
    ON c.identity_user_id = a."UserId"
JOIN tbl_tc_vi_chi_nhanh d
    ON d.deleted = 0
    AND d.chi_nhanh_id = b.auto_id
    AND d.thanh_vien_id = c.auto_id
LEFT JOIN tbl_sys_thanh_vien e
    ON e.identity_user_id = a."PerformedByAdminId"
LEFT JOIN tbl_tc_yeu_cau_nap_tien f
    ON f.legacy_uuid = a."ReferenceId"
LEFT JOIN tbl_tc_don_in g
    ON g.legacy_uuid = a."ReferenceId"
LEFT JOIN tbl_tc_don_hang_san_pham h
    ON h.legacy_uuid = a."ReferenceId"
LEFT JOIN tbl_tc_don_dich_vu_ho_tro i
    ON i.legacy_uuid = a."ReferenceId"
ON CONFLICT (legacy_uuid) WHERE legacy_uuid IS NOT NULL DO UPDATE
SET so_tien = EXCLUDED.so_tien,
    so_du_truoc = EXCLUDED.so_du_truoc,
    so_du_sau = EXCLUDED.so_du_sau,
    ref_id = EXCLUDED.ref_id,
    ref_legacy_uuid = EXCLUDED.ref_legacy_uuid,
    ghi_chu = EXCLUDED.ghi_chu,
    last_updated_by_function = EXCLUDED.last_updated_by_function;

WITH legacy_balance AS
(
    SELECT
        a.auto_id AS thanh_vien_id,
        a.identity_user_id,
        a.current_balance_legacy,
        COALESCE(SUM(b.so_tien), 0)::numeric(18,2) AS ledger_balance
    FROM tbl_sys_thanh_vien a
    LEFT JOIN tbl_tc_giao_dich_vi b
        ON b.deleted = 0
        AND b.thanh_vien_id = a.auto_id
    WHERE a.deleted = 0
    GROUP BY
        a.auto_id,
        a.identity_user_id,
        a.current_balance_legacy
),
default_wallet AS
(
    SELECT
        b.thanh_vien_id,
        b.current_balance_legacy,
        b.ledger_balance,
        c.auto_id AS vi_chi_nhanh_id,
        c.chi_nhanh_id
    FROM legacy_balance b
    JOIN tbl_tc_vi_chi_nhanh c
        ON c.deleted = 0
        AND c.thanh_vien_id = b.thanh_vien_id
    WHERE c.chi_nhanh_id = (
        SELECT d.auto_id
        FROM tbl_dm_chi_nhanh d
        WHERE d.deleted = 0
        ORDER BY d.auto_id
        LIMIT 1
    )
)
INSERT INTO tbl_tc_giao_dich_vi
(
    chi_nhanh_id,
    vi_chi_nhanh_id,
    thanh_vien_id,
    loai_giao_dich_id,
    so_tien,
    so_du_truoc,
    so_du_sau,
    ref_type,
    ghi_chu,
    idempotency_key,
    created_by_function,
    last_updated_by_function
)
SELECT
    a.chi_nhanh_id,
    a.vi_chi_nhanh_id,
    a.thanh_vien_id,
    6,
    a.current_balance_legacy - a.ledger_balance,
    a.ledger_balance,
    a.current_balance_legacy,
    'MigrationResidual',
    'Residual transaction generated to preserve AspNetUsers.CurrentBalance during TKS canonical migration.',
    'migration-residual-' || a.thanh_vien_id::text,
    'V20260704_002_migrate',
    'V20260704_002_migrate'
FROM default_wallet a
WHERE a.current_balance_legacy <> a.ledger_balance
    AND NOT EXISTS (
        SELECT 1
        FROM tbl_tc_giao_dich_vi b
        WHERE b.deleted = 0
            AND b.vi_chi_nhanh_id = a.vi_chi_nhanh_id
            AND b.idempotency_key = 'migration-residual-' || a.thanh_vien_id::text
    );

UPDATE tbl_tc_vi_chi_nhanh a
SET so_du = COALESCE(b.ledger_balance, 0),
    last_updated_by_function = 'V20260704_002_migrate_reconcile'
FROM (
    SELECT
        c.vi_chi_nhanh_id,
        SUM(c.so_tien)::numeric(18,2) AS ledger_balance
    FROM tbl_tc_giao_dich_vi c
    WHERE c.deleted = 0
    GROUP BY c.vi_chi_nhanh_id
) b
WHERE b.vi_chi_nhanh_id = a.auto_id;

UPDATE tbl_tc_yeu_cau_nap_tien a
SET giao_dich_vi_duyet_id = b.auto_id,
    last_updated_by_function = 'V20260704_002_migrate_link_wallet'
FROM legacy_top_up_requests c
JOIN tbl_tc_giao_dich_vi b
    ON b.legacy_uuid = c."ApprovedWalletTransactionId"
WHERE a.legacy_uuid = c."Id"
    AND c."ApprovedWalletTransactionId" IS NOT NULL;

UPDATE tbl_tc_don_in a
SET giao_dich_vi_thanh_toan_id = b.auto_id,
    last_updated_by_function = 'V20260704_002_migrate_link_wallet'
FROM legacy_print_jobs c
JOIN tbl_tc_giao_dich_vi b
    ON b.legacy_uuid = c."PaidWalletTransactionId"
WHERE a.legacy_uuid = c."Id"
    AND c."PaidWalletTransactionId" IS NOT NULL;

INSERT INTO tbl_log_record_action_history
(
    ref_id,
    ten_hanh_dong,
    ten_moi_truong,
    ma_chuc_nang,
    ten_chuc_nang,
    noi_dung_action,
    created,
    last_updated,
    created_by_function,
    last_updated_by_function
)
SELECT
    0,
    a."Action",
    'LegacyAuditLog',
    a."EntityName",
    a."EntityName",
    concat_ws(' | ', a."EntityId", a."Details", a."IpAddress", a."RecordHash"),
    a."CreatedAt",
    COALESCE(a."UpdatedAt", a."CreatedAt"),
    'V20260704_002_migrate',
    'V20260704_002_migrate'
FROM legacy_audit_logs a
WHERE NOT EXISTS (
    SELECT 1
    FROM tbl_log_record_action_history b
    WHERE b.deleted = 0
        AND b.ten_hanh_dong = a."Action"
        AND b.ma_chuc_nang = a."EntityName"
        AND b.created = a."CreatedAt"
);

INSERT INTO tbl_sys_database_version
(
    patch_id,
    description,
    created_by_function,
    last_updated_by_function
)
VALUES
(
    'V20260704_002_migrate_app_schema_to_tks_canonical',
    'Migrate current app/system/audit schemas into TKS canonical schema.',
    'V20260704_002',
    'V20260704_002'
)
ON CONFLICT (patch_id) DO UPDATE
SET description = EXCLUDED.description,
    last_updated_by_function = EXCLUDED.last_updated_by_function;

COMMIT;
