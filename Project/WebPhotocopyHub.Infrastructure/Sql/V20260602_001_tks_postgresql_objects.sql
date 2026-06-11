-- WebPhotocopyHub TKS PostgreSQL objects
-- Version: 20260602_001
-- Apply manually or through an approved deployment runner before enabling stored-routine DataAccess runtime.

        CREATE OR REPLACE VIEW "view_Sys_Application_User" AS
        SELECT
            A."Id",
            A."FullName",
            A."Email",
            A."PhoneNumber",
            A."Address",
            A."CurrentBalance",
            A."IsActive",
            A."CreatedAt"
        FROM "AspNetUsers" AS A;

        CREATE OR REPLACE VIEW "view_TC_Wallet_Transaction" AS
        SELECT
            A."Id",
            A."UserId",
            A."TransactionType",
            A."Amount",
            A."BalanceBefore",
            A."BalanceAfter",
            A."ReferenceType",
            A."ReferenceId",
            A."Note",
            A."IdempotencyKey",
            A."PerformedByAdminId",
            A."CreatedAt",
            A."UpdatedAt"
        FROM "WalletTransactions" AS A;

        CREATE OR REPLACE VIEW "view_TC_Top_Up_Request" AS
        SELECT
            A."Id",
            A."UserId",
            A."Amount",
            A."TransferContent",
            A."TransactionReferenceCode",
            A."CreateIdempotencyKey",
            A."LastReviewIdempotencyKey",
            A."Channel",
            A."ProofFileId",
            A."Status",
            A."RequiresAdminApproval",
            A."ReviewedByAdminId",
            A."ReviewedAt",
            A."ReviewNote",
            A."SecondReviewedByAdminId",
            A."SecondReviewedAt",
            A."SecondReviewNote",
            A."ApprovedWalletTransactionId",
            A."CreatedAt",
            A."UpdatedAt"
        FROM "TopUpRequests" AS A;

        CREATE OR REPLACE VIEW "view_XNK_Print_Job" AS
        SELECT
            A."Id",
            A."UserId",
            A."UploadedFileId",
            A."PaperSize",
            A."PrintSide",
            A."ColorMode",
            A."IsPhoto",
            A."Copies",
            A."TotalPages",
            A."Notes",
            A."DeliveryMethod",
            A."DeliveryAddress",
            A."UnitPrice",
            A."SubTotal",
            A."ShippingFee",
            A."TotalAmount",
            A."Status",
            A."ConfirmedByOperatorId",
            A."ConfirmedAt",
            A."AssignedOperatorId",
            A."LastStatusNote",
            A."PaidAt",
            A."PaidWalletTransactionId",
            A."SubmitIdempotencyKey",
            A."ProcessedByAdminId",
            A."RefundedByUserId",
            A."RefundedAt",
            A."RefundReason",
            A."CreatedAt",
            A."UpdatedAt"
        FROM "PrintJobs" AS A;

        CREATE OR REPLACE VIEW "view_DM_Product" AS
        SELECT
            A."Id",
            A."Name",
            A."Description",
            A."Price",
            A."StockQuantity",
            A."ImageUrl",
            A."IsActive",
            A."CreatedAt",
            A."UpdatedAt"
        FROM "Products" AS A;

        CREATE OR REPLACE VIEW "view_XNK_Product_Order" AS
        SELECT
            A."Id",
            A."UserId",
            A."TotalAmount",
            A."DeliveryMethod",
            A."DeliveryAddress",
            A."Notes",
            A."OrderIdempotencyKey",
            A."Status",
            A."ProcessedByOperatorId",
            A."ProcessedAt",
            A."ProcessNote",
            A."CreatedAt",
            A."UpdatedAt"
        FROM "ProductOrders" AS A;

        CREATE OR REPLACE VIEW "view_XNK_Product_Order_Item" AS
        SELECT
            A."Id",
            A."ProductOrderId",
            A."ProductId",
            A."Quantity",
            A."UnitPrice",
            A."LineTotal",
            A."CreatedAt",
            A."UpdatedAt"
        FROM "ProductOrderItems" AS A;

        CREATE OR REPLACE VIEW "view_XNK_Product_Stock_Movement" AS
        SELECT
            A."Id",
            A."ProductId",
            A."ActorUserId",
            A."MovementType",
            A."QuantityChanged",
            A."StockBefore",
            A."StockAfter",
            A."Note",
            A."CreatedAt",
            A."UpdatedAt"
        FROM "ProductStockMovements" AS A;

        CREATE OR REPLACE VIEW "view_DM_Support_Service" AS
        SELECT
            A."Id",
            A."Name",
            A."Description",
            A."UnitPrice",
            A."FeeType",
            A."IsActive",
            A."CreatedAt",
            A."UpdatedAt"
        FROM "SupportServices" AS A;

        CREATE OR REPLACE VIEW "view_XNK_Support_Service_Order" AS
        SELECT
            A."Id",
            A."UserId",
            A."SupportServiceId",
            A."Quantity",
            A."UnitPrice",
            A."TotalAmount",
            A."Notes",
            A."OrderIdempotencyKey",
            A."Status",
            A."ProcessedByOperatorId",
            A."ProcessedAt",
            A."ProcessNote",
            A."CreatedAt",
            A."UpdatedAt"
        FROM "SupportServiceOrders" AS A;

        CREATE OR REPLACE VIEW "view_Log_Audit_Log" AS
        SELECT
            A."Id",
            A."ActorUserId",
            A."Action",
            A."EntityName",
            A."EntityId",
            A."Details",
            A."IpAddress",
            A."PreviousHash",
            A."RecordHash",
            A."CreatedAt",
            A."UpdatedAt"
        FROM "AuditLogs" AS A;

        CREATE OR REPLACE VIEW "view_DM_Pricing_Rule" AS
        SELECT
            A."Id",
            A."PaperSize",
            A."PrintSide",
            A."ColorMode",
            A."IsPhoto",
            A."UnitPrice",
            A."IsActive",
            A."CreatedAt",
            A."UpdatedAt"
        FROM "PricingRules" AS A;

        CREATE OR REPLACE VIEW "view_Sys_Uploaded_File_Metadata" AS
        SELECT
            A."Id",
            A."OwnerUserId",
            A."OriginalFileName",
            A."StoredFileName",
            A."RelativePath",
            A."Size",
            A."ContentType",
            A."IsForPrintJob",
            A."CreatedAt",
            A."UpdatedAt"
        FROM "UploadedFileMetadatas" AS A;

        CREATE OR REPLACE FUNCTION "FQ_101_USR_sp_sel_List"()
        RETURNS SETOF "view_Sys_Application_User"
        LANGUAGE sql
        STABLE
        AS $$
            SELECT
                "Id",
                "FullName",
                "Email",
                "PhoneNumber",
                "Address",
                "CurrentBalance",
                "IsActive",
                "CreatedAt"
            FROM "view_Sys_Application_User"
            ORDER BY "CreatedAt" DESC;
        $$;

        CREATE OR REPLACE FUNCTION "FQ_101_USR_sp_sel_Get_By_ID"(p_strAuto_ID text)
        RETURNS SETOF "view_Sys_Application_User"
        LANGUAGE sql
        STABLE
        AS $$
            SELECT
                "Id",
                "FullName",
                "Email",
                "PhoneNumber",
                "Address",
                "CurrentBalance",
                "IsActive",
                "CreatedAt"
            FROM "view_Sys_Application_User"
            WHERE "Id" = p_strAuto_ID;
        $$;

        CREATE OR REPLACE FUNCTION "FQ_110_PRD_sp_sel_List"()
        RETURNS SETOF "view_DM_Product"
        LANGUAGE sql
        STABLE
        AS $$
            SELECT
                "Id",
                "Name",
                "Description",
                "Price",
                "StockQuantity",
                "ImageUrl",
                "IsActive",
                "CreatedAt",
                "UpdatedAt"
            FROM "view_DM_Product"
            ORDER BY "CreatedAt" DESC;
        $$;

        CREATE OR REPLACE FUNCTION "FQ_110_PRD_sp_sel_Get_By_ID"(p_iAuto_ID uuid)
        RETURNS SETOF "view_DM_Product"
        LANGUAGE sql
        STABLE
        AS $$
            SELECT
                "Id",
                "Name",
                "Description",
                "Price",
                "StockQuantity",
                "ImageUrl",
                "IsActive",
                "CreatedAt",
                "UpdatedAt"
            FROM "view_DM_Product"
            WHERE "Id" = p_iAuto_ID;
        $$;

        CREATE OR REPLACE FUNCTION "FQ_111_PRC_sp_sel_List"()
        RETURNS SETOF "view_DM_Pricing_Rule"
        LANGUAGE sql
        STABLE
        AS $$
            SELECT
                "Id",
                "PaperSize",
                "PrintSide",
                "ColorMode",
                "IsPhoto",
                "UnitPrice",
                "IsActive",
                "CreatedAt",
                "UpdatedAt"
            FROM "view_DM_Pricing_Rule"
            ORDER BY "PaperSize", "ColorMode", "PrintSide";
        $$;

        CREATE OR REPLACE FUNCTION "FQ_111_PRC_sp_sel_Get_By_ID"(p_iAuto_ID uuid)
        RETURNS SETOF "view_DM_Pricing_Rule"
        LANGUAGE sql
        STABLE
        AS $$
            SELECT
                "Id",
                "PaperSize",
                "PrintSide",
                "ColorMode",
                "IsPhoto",
                "UnitPrice",
                "IsActive",
                "CreatedAt",
                "UpdatedAt"
            FROM "view_DM_Pricing_Rule"
            WHERE "Id" = p_iAuto_ID;
        $$;

        CREATE OR REPLACE FUNCTION "FQ_112_SVS_sp_sel_List"()
        RETURNS SETOF "view_DM_Support_Service"
        LANGUAGE sql
        STABLE
        AS $$
            SELECT
                "Id",
                "Name",
                "Description",
                "UnitPrice",
                "FeeType",
                "IsActive",
                "CreatedAt",
                "UpdatedAt"
            FROM "view_DM_Support_Service"
            ORDER BY "CreatedAt" DESC;
        $$;

        CREATE OR REPLACE FUNCTION "FQ_112_SVS_sp_sel_Get_By_ID"(p_iAuto_ID uuid)
        RETURNS SETOF "view_DM_Support_Service"
        LANGUAGE sql
        STABLE
        AS $$
            SELECT
                "Id",
                "Name",
                "Description",
                "UnitPrice",
                "FeeType",
                "IsActive",
                "CreatedAt",
                "UpdatedAt"
            FROM "view_DM_Support_Service"
            WHERE "Id" = p_iAuto_ID;
        $$;

        CREATE OR REPLACE FUNCTION "FQ_201_WLT_sp_sel_List"()
        RETURNS SETOF "view_TC_Wallet_Transaction"
        LANGUAGE sql
        STABLE
        AS $$
            SELECT
                "Id",
                "UserId",
                "TransactionType",
                "Amount",
                "BalanceBefore",
                "BalanceAfter",
                "ReferenceType",
                "ReferenceId",
                "Note",
                "IdempotencyKey",
                "PerformedByAdminId",
                "CreatedAt",
                "UpdatedAt"
            FROM "view_TC_Wallet_Transaction"
            ORDER BY "CreatedAt" DESC;
        $$;

        CREATE OR REPLACE FUNCTION "FQ_201_WLT_sp_sel_List_By_User_ID"(p_strUser_ID text)
        RETURNS SETOF "view_TC_Wallet_Transaction"
        LANGUAGE sql
        STABLE
        AS $$
            SELECT
                "Id",
                "UserId",
                "TransactionType",
                "Amount",
                "BalanceBefore",
                "BalanceAfter",
                "ReferenceType",
                "ReferenceId",
                "Note",
                "IdempotencyKey",
                "PerformedByAdminId",
                "CreatedAt",
                "UpdatedAt"
            FROM "view_TC_Wallet_Transaction"
            WHERE "UserId" = p_strUser_ID
            ORDER BY "CreatedAt" DESC;
        $$;

        CREATE OR REPLACE FUNCTION "FQ_202_TOP_sp_sel_List"()
        RETURNS SETOF "view_TC_Top_Up_Request"
        LANGUAGE sql
        STABLE
        AS $$
            SELECT
                "Id",
                "UserId",
                "Amount",
                "TransferContent",
                "TransactionReferenceCode",
                "CreateIdempotencyKey",
                "LastReviewIdempotencyKey",
                "Channel",
                "ProofFileId",
                "Status",
                "RequiresAdminApproval",
                "ReviewedByAdminId",
                "ReviewedAt",
                "ReviewNote",
                "SecondReviewedByAdminId",
                "SecondReviewedAt",
                "SecondReviewNote",
                "ApprovedWalletTransactionId",
                "CreatedAt",
                "UpdatedAt"
            FROM "view_TC_Top_Up_Request"
            ORDER BY "CreatedAt" DESC;
        $$;

        CREATE OR REPLACE FUNCTION "FQ_202_TOP_sp_sel_Get_By_ID"(p_iAuto_ID uuid)
        RETURNS SETOF "view_TC_Top_Up_Request"
        LANGUAGE sql
        STABLE
        AS $$
            SELECT
                "Id",
                "UserId",
                "Amount",
                "TransferContent",
                "TransactionReferenceCode",
                "CreateIdempotencyKey",
                "LastReviewIdempotencyKey",
                "Channel",
                "ProofFileId",
                "Status",
                "RequiresAdminApproval",
                "ReviewedByAdminId",
                "ReviewedAt",
                "ReviewNote",
                "SecondReviewedByAdminId",
                "SecondReviewedAt",
                "SecondReviewNote",
                "ApprovedWalletTransactionId",
                "CreatedAt",
                "UpdatedAt"
            FROM "view_TC_Top_Up_Request"
            WHERE "Id" = p_iAuto_ID;
        $$;

        CREATE OR REPLACE FUNCTION "FQ_301_PRJ_sp_sel_List"()
        RETURNS SETOF "view_XNK_Print_Job"
        LANGUAGE sql
        STABLE
        AS $$
            SELECT
                "Id",
                "UserId",
                "UploadedFileId",
                "PaperSize",
                "PrintSide",
                "ColorMode",
                "IsPhoto",
                "Copies",
                "TotalPages",
                "Notes",
                "DeliveryMethod",
                "DeliveryAddress",
                "UnitPrice",
                "SubTotal",
                "ShippingFee",
                "TotalAmount",
                "Status",
                "ConfirmedByOperatorId",
                "ConfirmedAt",
                "AssignedOperatorId",
                "LastStatusNote",
                "PaidAt",
                "PaidWalletTransactionId",
                "SubmitIdempotencyKey",
                "ProcessedByAdminId",
                "RefundedByUserId",
                "RefundedAt",
                "RefundReason",
                "CreatedAt",
                "UpdatedAt"
            FROM "view_XNK_Print_Job"
            ORDER BY "CreatedAt" DESC;
        $$;

        CREATE OR REPLACE FUNCTION "FQ_301_PRJ_sp_sel_Get_By_ID"(p_iAuto_ID uuid)
        RETURNS SETOF "view_XNK_Print_Job"
        LANGUAGE sql
        STABLE
        AS $$
            SELECT
                "Id",
                "UserId",
                "UploadedFileId",
                "PaperSize",
                "PrintSide",
                "ColorMode",
                "IsPhoto",
                "Copies",
                "TotalPages",
                "Notes",
                "DeliveryMethod",
                "DeliveryAddress",
                "UnitPrice",
                "SubTotal",
                "ShippingFee",
                "TotalAmount",
                "Status",
                "ConfirmedByOperatorId",
                "ConfirmedAt",
                "AssignedOperatorId",
                "LastStatusNote",
                "PaidAt",
                "PaidWalletTransactionId",
                "SubmitIdempotencyKey",
                "ProcessedByAdminId",
                "RefundedByUserId",
                "RefundedAt",
                "RefundReason",
                "CreatedAt",
                "UpdatedAt"
            FROM "view_XNK_Print_Job"
            WHERE "Id" = p_iAuto_ID;
        $$;

        CREATE OR REPLACE FUNCTION "FQ_401_POR_sp_sel_List"()
        RETURNS SETOF "view_XNK_Product_Order"
        LANGUAGE sql
        STABLE
        AS $$
            SELECT
                "Id",
                "UserId",
                "TotalAmount",
                "DeliveryMethod",
                "DeliveryAddress",
                "Notes",
                "OrderIdempotencyKey",
                "Status",
                "ProcessedByOperatorId",
                "ProcessedAt",
                "ProcessNote",
                "CreatedAt",
                "UpdatedAt"
            FROM "view_XNK_Product_Order"
            ORDER BY "CreatedAt" DESC;
        $$;

        CREATE OR REPLACE FUNCTION "FQ_401_POR_sp_sel_Get_By_ID"(p_iAuto_ID uuid)
        RETURNS SETOF "view_XNK_Product_Order"
        LANGUAGE sql
        STABLE
        AS $$
            SELECT
                "Id",
                "UserId",
                "TotalAmount",
                "DeliveryMethod",
                "DeliveryAddress",
                "Notes",
                "OrderIdempotencyKey",
                "Status",
                "ProcessedByOperatorId",
                "ProcessedAt",
                "ProcessNote",
                "CreatedAt",
                "UpdatedAt"
            FROM "view_XNK_Product_Order"
            WHERE "Id" = p_iAuto_ID;
        $$;

        CREATE OR REPLACE FUNCTION "FQ_402_POI_sp_sel_List_By_Order_ID"(p_iOrder_ID uuid)
        RETURNS SETOF "view_XNK_Product_Order_Item"
        LANGUAGE sql
        STABLE
        AS $$
            SELECT
                "Id",
                "ProductOrderId",
                "ProductId",
                "Quantity",
                "UnitPrice",
                "LineTotal",
                "CreatedAt",
                "UpdatedAt"
            FROM "view_XNK_Product_Order_Item"
            WHERE "ProductOrderId" = p_iOrder_ID
            ORDER BY "CreatedAt";
        $$;

        CREATE OR REPLACE FUNCTION "FQ_501_SVO_sp_sel_List"()
        RETURNS SETOF "view_XNK_Support_Service_Order"
        LANGUAGE sql
        STABLE
        AS $$
            SELECT
                "Id",
                "UserId",
                "SupportServiceId",
                "Quantity",
                "UnitPrice",
                "TotalAmount",
                "Notes",
                "OrderIdempotencyKey",
                "Status",
                "ProcessedByOperatorId",
                "ProcessedAt",
                "ProcessNote",
                "CreatedAt",
                "UpdatedAt"
            FROM "view_XNK_Support_Service_Order"
            ORDER BY "CreatedAt" DESC;
        $$;

        CREATE OR REPLACE FUNCTION "FQ_501_SVO_sp_sel_Get_By_ID"(p_iAuto_ID uuid)
        RETURNS SETOF "view_XNK_Support_Service_Order"
        LANGUAGE sql
        STABLE
        AS $$
            SELECT
                "Id",
                "UserId",
                "SupportServiceId",
                "Quantity",
                "UnitPrice",
                "TotalAmount",
                "Notes",
                "OrderIdempotencyKey",
                "Status",
                "ProcessedByOperatorId",
                "ProcessedAt",
                "ProcessNote",
                "CreatedAt",
                "UpdatedAt"
            FROM "view_XNK_Support_Service_Order"
            WHERE "Id" = p_iAuto_ID;
        $$;

        CREATE OR REPLACE FUNCTION "FQ_601_FIL_sp_sel_List_By_User_ID"(p_strUser_ID text)
        RETURNS SETOF "view_Sys_Uploaded_File_Metadata"
        LANGUAGE sql
        STABLE
        AS $$
            SELECT
                "Id",
                "OwnerUserId",
                "OriginalFileName",
                "StoredFileName",
                "RelativePath",
                "Size",
                "ContentType",
                "IsForPrintJob",
                "CreatedAt",
                "UpdatedAt"
            FROM "view_Sys_Uploaded_File_Metadata"
            WHERE "OwnerUserId" = p_strUser_ID
            ORDER BY "CreatedAt" DESC;
        $$;

        CREATE OR REPLACE PROCEDURE "FQ_110_PRD_sp_del_Deactivate"(p_iAuto_ID uuid)
        LANGUAGE plpgsql
        AS $$
        BEGIN
            UPDATE "Products"
            SET "IsActive" = FALSE, "UpdatedAt" = NOW()
            WHERE "Id" = p_iAuto_ID;
        END;
        $$;

        CREATE OR REPLACE PROCEDURE "FQ_112_SVS_sp_del_Deactivate"(p_iAuto_ID uuid)
        LANGUAGE plpgsql
        AS $$
        BEGIN
            UPDATE "SupportServices"
            SET "IsActive" = FALSE, "UpdatedAt" = NOW()
            WHERE "Id" = p_iAuto_ID;
        END;
        $$;

