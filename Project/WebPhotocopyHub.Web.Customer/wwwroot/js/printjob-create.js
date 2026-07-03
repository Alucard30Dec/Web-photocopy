(function () {
    "use strict";

    var form = document.getElementById("printJobForm");
    if (!form) {
        return;
    }

    var maxFiles = 5;
    var maxFileSize = 20 * 1024 * 1024;
    var maxBatchSize = 100 * 1024 * 1024;
    var pageCountUrl = form.dataset.pageCountUrl || "";
    var officePreviewUrl = form.dataset.officePreviewUrl || "";
    var calculatePriceUrl = form.dataset.calculatePriceUrl || "";
    var antiForgeryToken = form.querySelector('input[name="__RequestVerificationToken"]');
    var allowedExtensions = ["pdf", "jpg", "jpeg", "png", "doc", "docx", "xls", "xlsx", "ppt", "pptx"];
    var imageExtensions = ["jpg", "jpeg", "png"];
    var officeExtensions = ["doc", "docx", "xls", "xlsx", "ppt", "pptx"];

    var fileInput = document.getElementById("UploadFiles");
    var dropzone = document.getElementById("uploadDropzone");
    var uploadQueue = document.getElementById("uploadQueue");
    var uploadQueueEmpty = document.getElementById("uploadQueueEmpty");
    var clientError = document.getElementById("fileClientError");
    var submitButton = document.getElementById("submitPrintJobButton");
    var existingCards = Array.prototype.slice.call(document.querySelectorAll("[data-existing-file]"));
    var presetButtons = Array.prototype.slice.call(document.querySelectorAll(".quick-preset"));

    var controls = {
        paperSize: document.getElementById("PaperSize"),
        printSide: document.getElementById("PrintSide"),
        colorMode: document.getElementById("ColorMode"),
        copies: document.getElementById("Copies"),
        deliveryMethod: document.getElementById("DeliveryMethod"),
        isPhoto: document.getElementById("IsPhoto"),
        deliveryAddress: document.getElementById("DeliveryAddress")
    };

    var summary = {
        fileCount: document.getElementById("summaryFileCount"),
        uploadSize: document.getElementById("summaryUploadSize"),
        paperSize: document.getElementById("summaryPaperSize"),
        printSide: document.getElementById("summaryPrintSide"),
        colorMode: document.getElementById("summaryColorMode"),
        copies: document.getElementById("summaryCopies"),
        pages: document.getElementById("summaryPages"),
        volume: document.getElementById("summaryVolume"),
        delivery: document.getElementById("summaryDelivery"),
        totalAmount: document.getElementById("summaryTotalAmount")
    };

    var deliveryAddressWrap = document.getElementById("deliveryAddressWrap");
    var previewDialog = document.getElementById("documentPreviewDialog");
    var previewCloseButton = document.getElementById("closeDocumentPreviewButton");
    var previewFileName = document.getElementById("previewFileName");
    var previewFrame = document.getElementById("documentPreviewFrame");
    var previewImage = document.getElementById("documentPreviewImage");
    var previewEmpty = document.getElementById("previewEmptyState");
    var previewFallback = document.getElementById("documentPreviewFallback");
    var previewFallbackType = document.getElementById("previewFallbackType");
    var previewFallbackName = document.getElementById("previewFallbackName");
    var previewFallbackMeta = document.getElementById("previewFallbackMeta");
    var previewFallbackMessage = document.getElementById("previewFallbackMessage");
    var previewDownloadLink = document.getElementById("previewDownloadLink");

    var selectedUploadFiles = [];
    var uploadPageCounts = new Map();
    var uploadPageFroms = new Map();
    var uploadPageTos = new Map();
    var uploadPageStatuses = new Map();
    var uploadPageErrors = new Map();
    var pageCountRequests = new Map();
    var activeObjectUrl = null;
    var activePreviewKey = null;
    var officePreviewCache = new Map();
    var officePreviewRequests = new Map();
    var officeWarmupQueue = [];
    var officeWarmupActive = false;

    function extensionOf(fileName) {
        var pieces = String(fileName || "").toLowerCase().split(".");
        return pieces.length > 1 ? pieces.pop() : "";
    }

    function previewKindOf(fileName) {
        var extension = extensionOf(fileName);
        if (extension === "pdf") {
            return "pdf";
        }

        if (imageExtensions.indexOf(extension) >= 0) {
            return "image";
        }

        return "office";
    }

    function fileKey(file) {
        return [file.name, file.size, file.lastModified].join("|");
    }

    function formatSize(bytes) {
        if (!bytes) {
            return "0 MB";
        }

        if (bytes >= 1024 * 1024) {
            return (bytes / 1024 / 1024).toLocaleString("vi-VN", { maximumFractionDigits: 1 }) + " MB";
        }

        return Math.max(1, bytes / 1024).toLocaleString("vi-VN", { maximumFractionDigits: 0 }) + " KB";
    }

    function showClientError(message) {
        if (!clientError) {
            return;
        }

        clientError.textContent = message;
        clientError.hidden = false;
    }

    function clearClientError() {
        if (!clientError) {
            return;
        }

        clientError.textContent = "";
        clientError.hidden = true;
    }

    function selectedExistingCards() {
        return existingCards.filter(function (card) {
            var checkbox = card.querySelector('input[type="checkbox"][name="ExistingFileIds"]');
            return checkbox && checkbox.checked;
        });
    }

    function totalSelectedCount() {
        return selectedUploadFiles.length + selectedExistingCards().length;
    }

    function totalUploadSize() {
        return selectedUploadFiles.reduce(function (total, file) {
            return total + file.size;
        }, 0);
    }

    function synchronizeFileInput() {
        if (!fileInput || typeof DataTransfer === "undefined") {
            return;
        }

        var transfer = new DataTransfer();
        selectedUploadFiles.forEach(function (file) {
            transfer.items.add(file);
        });
        fileInput.files = transfer.files;
    }

    function validateIncomingFile(file) {
        var extension = extensionOf(file.name);
        if (allowedExtensions.indexOf(extension) < 0) {
            return "File “" + file.name + "” không thuộc định dạng được hỗ trợ.";
        }

        if (file.size <= 0) {
            return "File “" + file.name + "” không có dữ liệu.";
        }

        if (file.size > maxFileSize) {
            return "File “" + file.name + "” vượt quá giới hạn 20 MB.";
        }

        return "";
    }

    function findUploadCard(file) {
        if (!uploadQueue) {
            return null;
        }

        var key = fileKey(file);
        return Array.prototype.slice.call(uploadQueue.querySelectorAll("[data-upload-card-key]")).find(function (card) {
            return card.dataset.uploadCardKey === key;
        }) || null;
    }

    function readPageNumber(value) {
        var number = Number(value);
        return Number.isInteger(number) ? number : 0;
    }

    function setDetectedPageCount(file, pageCount) {
        var totalPages = readPageNumber(pageCount);
        if (totalPages < 1 || totalPages > 10000) {
            setPageReadError(file, "Số trang đọc được không hợp lệ.");
            return;
        }

        var key = fileKey(file);
        uploadPageCounts.set(key, totalPages.toString());
        uploadPageStatuses.set(key, "ready");
        uploadPageErrors.delete(key);

        var currentFrom = readPageNumber(uploadPageFroms.get(key) || "0");
        var currentTo = readPageNumber(uploadPageTos.get(key) || "0");
        uploadPageFroms.set(key, currentFrom >= 1 && currentFrom <= totalPages ? currentFrom.toString() : "1");
        uploadPageTos.set(key, currentTo >= 1 && currentTo <= totalPages ? currentTo.toString() : totalPages.toString());

        applyUploadPageState(file);
        updateSummary();
    }

    function setPageReadLoading(file) {
        var key = fileKey(file);
        uploadPageStatuses.set(key, "loading");
        uploadPageErrors.delete(key);
        applyUploadPageState(file);
        updateSummary();
    }

    function setPageReadError(file, message) {
        var key = fileKey(file);
        uploadPageStatuses.set(key, "error");
        uploadPageCounts.delete(key);
        uploadPageErrors.set(key, message || "Không đọc được số trang.");
        applyUploadPageState(file);
        updateSummary();
    }

    function setRangeError(card, fromInput, toInput, message, invalidTarget) {
        var errorElement = card ? card.querySelector(".file-range-error") : null;
        var fromInvalid = invalidTarget === "from" || invalidTarget === "both";
        var toInvalid = invalidTarget === "to" || invalidTarget === "both";

        if (fromInput) {
            fromInput.classList.toggle("is-invalid", Boolean(message) && fromInvalid);
            fromInput.setCustomValidity(Boolean(message) && fromInvalid ? message : "");
        }
        if (toInput) {
            toInput.classList.toggle("is-invalid", Boolean(message) && toInvalid);
            toInput.setCustomValidity(Boolean(message) && toInvalid ? message : "");
        }
        if (errorElement) {
            errorElement.textContent = message || "";
            errorElement.hidden = !message;
        }
    }

    function validateUploadPageRange(file, focusInvalid) {
        var key = fileKey(file);
        var status = uploadPageStatuses.get(key) || "loading";
        var totalPages = readPageNumber(uploadPageCounts.get(key) || "0");
        var card = findUploadCard(file);
        var fromInput = card ? card.querySelector('[data-page-role="from"]') : null;
        var toInput = card ? card.querySelector('[data-page-role="to"]') : null;

        if (status !== "ready" || totalPages < 1) {
            setRangeError(card, fromInput, toInput, "", "");
            return false;
        }

        var fromValue = fromInput ? fromInput.value : (uploadPageFroms.get(key) || "");
        var toValue = toInput ? toInput.value : (uploadPageTos.get(key) || "");
        var pageFrom = readPageNumber(fromValue);
        var pageTo = readPageNumber(toValue);
        var message = "";
        var invalidTarget = "";

        if (pageFrom < 1 || pageFrom > totalPages) {
            message = "Trang bắt đầu phải từ 1 đến " + totalPages + ".";
            invalidTarget = "from";
        } else if (pageTo < 1 || pageTo > totalPages) {
            message = "Trang kết thúc phải từ 1 đến " + totalPages + ".";
            invalidTarget = "to";
        } else if (pageFrom > pageTo) {
            message = "Trang bắt đầu không được lớn hơn trang kết thúc.";
            invalidTarget = "both";
        }

        uploadPageFroms.set(key, fromValue);
        uploadPageTos.set(key, toValue);
        setRangeError(card, fromInput, toInput, message, invalidTarget);

        if (message && focusInvalid) {
            var focusTarget = invalidTarget === "to" ? toInput : fromInput;
            if (focusTarget) {
                focusTarget.focus();
            }
        }

        return !message;
    }

    function applyUploadPageState(file) {
        var card = findUploadCard(file);
        if (!card) {
            return;
        }

        var key = fileKey(file);
        var status = uploadPageStatuses.get(key) || "loading";
        var totalPages = readPageNumber(uploadPageCounts.get(key) || "0");
        var totalLabel = card.querySelector("[data-page-total-label]");
        var totalInput = card.querySelector("[data-page-total-input]");
        var fromInput = card.querySelector('[data-page-role="from"]');
        var toInput = card.querySelector('[data-page-role="to"]');
        var meta = card.querySelector("[data-file-meta]");

        if (totalInput) {
            totalInput.value = totalPages > 0 ? totalPages.toString() : "";
        }

        if (totalLabel) {
            totalLabel.dataset.status = status;
            totalLabel.textContent = status === "ready"
                ? totalPages + " trang"
                : status === "error"
                    ? "Không đọc được"
                    : "Đang đọc...";
        }

        if (meta) {
            meta.textContent = status === "ready"
                ? formatSize(file.size) + " • " + totalPages + " trang"
                : status === "error"
                    ? formatSize(file.size) + " • " + (uploadPageErrors.get(key) || "không đọc được số trang")
                    : formatSize(file.size) + " • đang đọc số trang";
        }

        [fromInput, toInput].forEach(function (input) {
            if (input) {
                input.disabled = status !== "ready";
                input.max = totalPages > 0 ? totalPages.toString() : "10000";
            }
        });

        if (fromInput && document.activeElement !== fromInput) {
            fromInput.value = uploadPageFroms.get(key) || "";
        }
        if (toInput && document.activeElement !== toInput) {
            toInput.value = uploadPageTos.get(key) || "";
        }

        validateUploadPageRange(file, false);
    }

    function updateSubmitAvailability() {
        if (!submitButton) {
            return;
        }

        var canSubmit = totalSelectedCount() > 0 && totalSelectedCount() <= maxFiles;
        selectedUploadFiles.forEach(function (file) {
            var status = uploadPageStatuses.get(fileKey(file)) || "loading";
            if (status !== "ready" || !validateUploadPageRange(file, false)) {
                canSubmit = false;
            }
        });

        submitButton.disabled = !canSubmit;
    }

    function addFiles(fileList) {
        clearClientError();
        var incoming = Array.prototype.slice.call(fileList || []);
        var existingKeys = new Set(selectedUploadFiles.map(fileKey));

        for (var index = 0; index < incoming.length; index++) {
            var file = incoming[index];
            var validationMessage = validateIncomingFile(file);
            if (validationMessage) {
                showClientError(validationMessage);
                continue;
            }

            if (existingKeys.has(fileKey(file))) {
                continue;
            }

            if (totalSelectedCount() >= maxFiles) {
                showClientError("Mỗi lần chỉ được chọn tối đa 5 tài liệu, gồm cả file mới và file đã upload.");
                break;
            }

            if (totalUploadSize() + file.size > maxBatchSize) {
                showClientError("Tổng dung lượng file mới không được vượt quá 100 MB.");
                break;
            }

            selectedUploadFiles.push(file);
            existingKeys.add(fileKey(file));
            setPageReadLoading(file);

            var kind = previewKindOf(file.name);
            if (kind === "image") {
                setDetectedPageCount(file, 1);
            }
            if (kind === "pdf") {
                queuePdfPageCount(file);
            }
            if (kind === "office") {
                queueOfficePreviewWarmup(file);
            }
        }

        synchronizeFileInput();
        renderUploadQueue();
        updateSummary();
    }

    function removeUploadFile(index) {
        var removed = selectedUploadFiles[index];
        if (!removed) {
            return;
        }

        var removedKey = fileKey(removed);
        uploadPageCounts.delete(removedKey);
        uploadPageFroms.delete(removedKey);
        uploadPageTos.delete(removedKey);
        uploadPageStatuses.delete(removedKey);
        uploadPageErrors.delete(removedKey);
        disposePageCountRequestForFile(removed);
        disposeOfficePreviewForFile(removed);
        selectedUploadFiles.splice(index, 1);
        synchronizeFileInput();
        renderUploadQueue();
        updateSummary();
        clearClientError();
    }

    function createUploadCard(file, index) {
        var extension = extensionOf(file.name);
        var card = document.createElement("article");
        card.className = "upload-file-card";
        card.dataset.uploadCardKey = fileKey(file);

        var identity = document.createElement("div");
        identity.className = "upload-file-card__identity";

        var type = document.createElement("span");
        type.className = "file-type-pill";
        type.textContent = extension ? extension.toUpperCase() : "FILE";

        var text = document.createElement("span");
        text.className = "upload-file-card__text";

        var name = document.createElement("strong");
        name.textContent = file.name;
        name.title = file.name;

        var meta = document.createElement("small");
        meta.dataset.fileMeta = "true";
        meta.textContent = formatSize(file.size) + " • đang đọc số trang";

        text.appendChild(name);
        text.appendChild(meta);
        identity.appendChild(type);
        identity.appendChild(text);

        var pageSettings = document.createElement("div");
        pageSettings.className = "upload-file-card__page-settings";

        var pageTotal = document.createElement("div");
        pageTotal.className = "file-page-total";
        var pageTotalTitle = document.createElement("span");
        pageTotalTitle.textContent = "Tổng số trang";
        var pageTotalValue = document.createElement("strong");
        pageTotalValue.dataset.pageTotalLabel = "true";
        pageTotalValue.dataset.status = "loading";
        pageTotalValue.textContent = "Đang đọc...";
        pageTotal.appendChild(pageTotalTitle);
        pageTotal.appendChild(pageTotalValue);

        var totalInput = document.createElement("input");
        totalInput.type = "hidden";
        totalInput.name = "UploadPageCounts";
        totalInput.dataset.pageTotalInput = "true";

        var range = document.createElement("div");
        range.className = "file-page-range";

        function createRangeInput(role, fieldName, labelText) {
            var label = document.createElement("label");
            var title = document.createElement("span");
            title.textContent = labelText;
            var input = document.createElement("input");
            input.type = "number";
            input.name = fieldName;
            input.min = "1";
            input.max = "10000";
            input.step = "1";
            input.inputMode = "numeric";
            input.className = "form-control form-control-sm file-page-input";
            input.dataset.pageRole = role;
            input.disabled = true;
            input.addEventListener("input", function () {
                var key = fileKey(file);
                if (role === "from") {
                    uploadPageFroms.set(key, input.value);
                } else {
                    uploadPageTos.set(key, input.value);
                }
                validateUploadPageRange(file, false);
                updateSummary();
            });
            input.addEventListener("blur", function () {
                validateUploadPageRange(file, false);
                updateSubmitAvailability();
            });
            label.appendChild(title);
            label.appendChild(input);
            return label;
        }

        var fromLabel = createRangeInput("from", "UploadPageFroms", "Từ trang");
        var separator = document.createElement("span");
        separator.className = "file-page-range__separator";
        separator.textContent = "–";
        var toLabel = createRangeInput("to", "UploadPageTos", "Đến trang");
        range.appendChild(fromLabel);
        range.appendChild(separator);
        range.appendChild(toLabel);

        var rangeError = document.createElement("div");
        rangeError.className = "file-range-error";
        rangeError.setAttribute("role", "alert");
        rangeError.hidden = true;

        pageSettings.appendChild(pageTotal);
        pageSettings.appendChild(totalInput);
        pageSettings.appendChild(range);
        pageSettings.appendChild(rangeError);

        var actions = document.createElement("div");
        actions.className = "upload-file-card__actions";

        var previewButton = document.createElement("button");
        previewButton.type = "button";
        previewButton.className = "btn btn-sm btn-outline-primary";
        previewButton.textContent = "Xem trước";
        previewButton.setAttribute("aria-haspopup", "dialog");
        previewButton.setAttribute("aria-controls", "documentPreviewDialog");
        previewButton.addEventListener("click", function () {
            showLocalPreview(file, previewButton);
        });

        var removeButton = document.createElement("button");
        removeButton.type = "button";
        removeButton.className = "btn btn-sm btn-outline-danger";
        removeButton.textContent = "Bỏ file";
        removeButton.addEventListener("click", function () {
            removeUploadFile(index);
        });

        actions.appendChild(previewButton);
        actions.appendChild(removeButton);

        card.appendChild(identity);
        card.appendChild(pageSettings);
        card.appendChild(actions);
        return card;
    }

    function renderUploadQueue() {
        if (!uploadQueue) {
            return;
        }

        Array.prototype.slice.call(uploadQueue.querySelectorAll(".upload-file-card")).forEach(function (card) {
            card.remove();
        });

        if (uploadQueueEmpty) {
            uploadQueueEmpty.hidden = selectedUploadFiles.length > 0;
        }

        selectedUploadFiles.forEach(function (file, index) {
            uploadQueue.appendChild(createUploadCard(file, index));
            applyUploadPageState(file);
        });
    }

    function revokeActiveObjectUrl() {
        if (activeObjectUrl) {
            URL.revokeObjectURL(activeObjectUrl);
            activeObjectUrl = null;
        }
    }

    function revokeOfficePreviewEntry(entry) {
        if (entry && entry.objectUrl) {
            URL.revokeObjectURL(entry.objectUrl);
        }
    }

    function clearPreviewSurface() {
        revokeActiveObjectUrl();

        if (previewFrame) {
            previewFrame.hidden = true;
            previewFrame.removeAttribute("src");
        }
        if (previewImage) {
            previewImage.hidden = true;
            previewImage.removeAttribute("src");
        }
        if (previewFallback) {
            previewFallback.hidden = true;
        }
        if (previewFallbackMessage) {
            previewFallbackMessage.textContent = "Đang chuẩn bị bản xem trước tài liệu.";
        }
        if (previewEmpty) {
            previewEmpty.hidden = true;
        }
        if (previewDownloadLink) {
            previewDownloadLink.hidden = true;
            previewDownloadLink.removeAttribute("href");
        }
    }

    function openPreviewDialog() {
        if (!previewDialog) {
            return;
        }

        if (typeof previewDialog.showModal === "function") {
            if (!previewDialog.open) {
                previewDialog.showModal();
            }
        } else {
            previewDialog.setAttribute("open", "");
            previewDialog.classList.add("is-open");
        }

        document.documentElement.classList.add("preview-dialog-open");
    }

    function closePreviewDialog() {
        activePreviewKey = null;

        if (!previewDialog) {
            clearPreviewSurface();
            return;
        }

        if (previewDialog.open && typeof previewDialog.close === "function") {
            previewDialog.close();
            return;
        }

        previewDialog.removeAttribute("open");
        previewDialog.classList.remove("is-open");
        document.documentElement.classList.remove("preview-dialog-open");
        clearPreviewSurface();
    }

    function showPreviewSource(kind, sourceUrl, fileName, fileMeta, downloadUrl, revokeOnClose) {
        clearPreviewSurface();

        if (revokeOnClose) {
            activeObjectUrl = sourceUrl;
        }

        if (previewFileName) {
            previewFileName.textContent = fileName;
        }

        openPreviewDialog();

        if (downloadUrl && previewDownloadLink) {
            previewDownloadLink.href = downloadUrl;
            previewDownloadLink.hidden = false;
        }

        if (kind === "pdf" && previewFrame) {
            previewFrame.src = sourceUrl;
            previewFrame.hidden = false;
            return;
        }

        if (kind === "image" && previewImage) {
            previewImage.src = sourceUrl;
            previewImage.alt = "Xem trước " + fileName;
            previewImage.hidden = false;
            return;
        }

        if (previewFallback) {
            if (previewFallbackType) {
                previewFallbackType.textContent = extensionOf(fileName).toUpperCase() || "FILE";
            }
            if (previewFallbackName) {
                previewFallbackName.textContent = fileName;
            }
            if (previewFallbackMeta) {
                previewFallbackMeta.textContent = fileMeta || "Giữ nguyên định dạng gốc";
            }
            previewFallback.hidden = false;
        }
    }

    function showPreviewFallback(fileName, fileMeta, message) {
        clearPreviewSurface();

        if (previewFileName) {
            previewFileName.textContent = fileName;
        }

        if (previewFallbackType) {
            previewFallbackType.textContent = extensionOf(fileName).toUpperCase() || "FILE";
        }

        if (previewFallbackName) {
            previewFallbackName.textContent = fileName;
        }

        if (previewFallbackMeta) {
            previewFallbackMeta.textContent = fileMeta || "";
        }

        if (previewFallbackMessage) {
            previewFallbackMessage.textContent = message;
        }

        if (previewFallback) {
            previewFallback.hidden = false;
        }

        openPreviewDialog();
    }

    async function readPreviewError(response) {
        var fallbackMessage = "Không thể tạo bản xem trước file Office.";
        var correlationId = response.headers.get("X-Correlation-ID") || "";
        var statusSuffix = " (HTTP " + response.status + ")";
        var correlationSuffix = correlationId
            ? " Mã theo dõi: " + correlationId + "."
            : "";

        try {
            var contentType = (response.headers.get("content-type") || "").toLowerCase();
            if (contentType.indexOf("application/json") >= 0) {
                var payload = await response.json();
                var jsonMessage = payload && payload.message
                    ? payload.message
                    : fallbackMessage + statusSuffix;
                return jsonMessage + correlationSuffix;
            }

            var text = (await response.text()).trim();
            if (text && text.charAt(0) !== "<") {
                return text + correlationSuffix;
            }

            return fallbackMessage + statusSuffix + correlationSuffix;
        } catch (error) {
            return fallbackMessage + statusSuffix + correlationSuffix;
        }
    }

    function updateOfficePageCount(file, pageCount) {
        setDetectedPageCount(file, pageCount);
    }

    async function readPageCountError(response) {
        var fallbackMessage = "Không thể đọc số trang của file.";
        try {
            var payload = await response.json();
            return payload && payload.message ? payload.message : fallbackMessage;
        } catch (error) {
            return fallbackMessage + " (HTTP " + response.status + ")";
        }
    }

    function createPageCountRequest(file) {
        var key = fileKey(file);
        var existingRequest = pageCountRequests.get(key);
        if (existingRequest) {
            return existingRequest.promise;
        }

        if (!pageCountUrl) {
            return Promise.reject(new Error("Chưa cấu hình endpoint đọc số trang."));
        }

        var requestController = typeof AbortController !== "undefined" ? new AbortController() : null;
        var requestData = new FormData();
        requestData.append("file", file, file.name);
        if (antiForgeryToken && antiForgeryToken.value) {
            requestData.append("__RequestVerificationToken", antiForgeryToken.value);
        }

        var requestPromise = (async function () {
            var response = await fetch(pageCountUrl, {
                method: "POST",
                body: requestData,
                credentials: "same-origin",
                signal: requestController ? requestController.signal : undefined
            });

            if (!response.ok) {
                throw new Error(await readPageCountError(response));
            }

            var payload = await response.json();
            var pageCount = payload ? readPageNumber(payload.pageCount) : 0;
            if (pageCount < 1 || pageCount > 10000) {
                throw new Error("Số trang máy chủ trả về không hợp lệ.");
            }

            return pageCount;
        })();

        pageCountRequests.set(key, { promise: requestPromise, controller: requestController });
        var clearRequest = function () {
            var currentRequest = pageCountRequests.get(key);
            if (currentRequest && currentRequest.promise === requestPromise) {
                pageCountRequests.delete(key);
            }
        };
        requestPromise.then(clearRequest, clearRequest);
        return requestPromise;
    }

    function queuePdfPageCount(file) {
        setPageReadLoading(file);
        createPageCountRequest(file)
            .then(function (pageCount) {
                setDetectedPageCount(file, pageCount);
            })
            .catch(function (error) {
                if (error && error.name === "AbortError") {
                    return;
                }
                setPageReadError(file, error && error.message ? error.message : "Không đọc được số trang PDF.");
            });
    }

    function disposePageCountRequestForFile(file) {
        var key = fileKey(file);
        var request = pageCountRequests.get(key);
        if (request && request.controller) {
            request.controller.abort();
        }
        pageCountRequests.delete(key);
    }

    function createOfficePreviewRequest(file) {
        var key = fileKey(file);
        var existingRequest = officePreviewRequests.get(key);
        if (existingRequest) {
            return existingRequest.promise;
        }

        var requestController = typeof AbortController !== "undefined"
            ? new AbortController()
            : null;

        var requestData = new FormData();
        requestData.append("file", file, file.name);
        if (antiForgeryToken && antiForgeryToken.value) {
            requestData.append("__RequestVerificationToken", antiForgeryToken.value);
        }

        var requestPromise = (async function () {
            var response = await fetch(officePreviewUrl, {
                method: "POST",
                body: requestData,
                credentials: "same-origin",
                signal: requestController ? requestController.signal : undefined
            });

            if (!response.ok) {
                throw new Error(await readPreviewError(response));
            }

            var responseContentType = (response.headers.get("content-type") || "").toLowerCase();
            if (responseContentType.indexOf("application/pdf") < 0) {
                throw new Error("Máy chủ không trả về PDF hợp lệ cho bản xem trước Office.");
            }

            var pageCount = parseInt(response.headers.get("X-Preview-Page-Count") || "0", 10);
            if (Number.isNaN(pageCount) || pageCount < 1 || pageCount > 10000) {
                throw new Error("Không đọc được số trang của file Office sau khi chuyển đổi.");
            }
            var pdfBlob = await response.blob();

            if (!pdfBlob || pdfBlob.size <= 0) {
                throw new Error("Bản xem trước PDF không có dữ liệu.");
            }

            var entry = {
                objectUrl: URL.createObjectURL(pdfBlob),
                pageCount: Number.isNaN(pageCount) ? 0 : pageCount,
                byteSize: pdfBlob.size
            };

            var previousEntry = officePreviewCache.get(key);
            if (previousEntry) {
                revokeOfficePreviewEntry(previousEntry);
            }

            officePreviewCache.set(key, entry);
            return entry;
        })();

        officePreviewRequests.set(key, {
            promise: requestPromise,
            controller: requestController
        });

        var clearRequest = function () {
            var currentRequest = officePreviewRequests.get(key);
            if (currentRequest && currentRequest.promise === requestPromise) {
                officePreviewRequests.delete(key);
            }
        };

        requestPromise.then(clearRequest, clearRequest);
        return requestPromise;
    }

    function queueOfficePreviewWarmup(file) {
        if (!officePreviewUrl || previewKindOf(file.name) !== "office") {
            return;
        }

        var key = fileKey(file);
        if (officePreviewCache.has(key) || officePreviewRequests.has(key)) {
            return;
        }

        var alreadyQueued = officeWarmupQueue.some(function (queuedFile) {
            return fileKey(queuedFile) === key;
        });

        if (!alreadyQueued) {
            officeWarmupQueue.push(file);
        }

        runOfficePreviewWarmup();
    }

    function runOfficePreviewWarmup() {
        if (officeWarmupActive || officeWarmupQueue.length === 0) {
            return;
        }

        var file = officeWarmupQueue.shift();
        var key = fileKey(file);
        var isStillSelected = selectedUploadFiles.some(function (selectedFile) {
            return fileKey(selectedFile) === key;
        });

        if (!isStillSelected || officePreviewCache.has(key)) {
            runOfficePreviewWarmup();
            return;
        }

        officeWarmupActive = true;

        createOfficePreviewRequest(file)
            .then(function (entry) {
                updateOfficePageCount(file, entry.pageCount);
            })
            .catch(function (error) {
                if (!error || error.name !== "AbortError") {
                    console.warn("Office preview warmup failed.", error);
                    setPageReadError(
                        file,
                        error && error.message ? error.message : "Không đọc được số trang file Office."
                    );
                }
            })
            .finally(function () {
                officeWarmupActive = false;
                runOfficePreviewWarmup();
            });
    }

    function disposeOfficePreviewForFile(file) {
        var key = fileKey(file);
        officeWarmupQueue = officeWarmupQueue.filter(function (queuedFile) {
            return fileKey(queuedFile) !== key;
        });

        var request = officePreviewRequests.get(key);
        if (request && request.controller) {
            request.controller.abort();
        }
        officePreviewRequests.delete(key);

        var cachedEntry = officePreviewCache.get(key);
        if (cachedEntry) {
            revokeOfficePreviewEntry(cachedEntry);
            officePreviewCache.delete(key);
        }

        if (activePreviewKey === key) {
            closePreviewDialog();
        }
    }

    function disposeAllPreviewResources() {
        pageCountRequests.forEach(function (request) {
            if (request.controller) {
                request.controller.abort();
            }
        });
        pageCountRequests.clear();

        officePreviewRequests.forEach(function (request) {
            if (request.controller) {
                request.controller.abort();
            }
        });
        officePreviewRequests.clear();

        officePreviewCache.forEach(revokeOfficePreviewEntry);
        officePreviewCache.clear();
        officeWarmupQueue = [];

        revokeActiveObjectUrl();
    }

    async function showOfficePreview(file, previewButton) {
        if (!officePreviewUrl) {
            showPreviewFallback(
                file.name,
                formatSize(file.size),
                "Chưa cấu hình endpoint chuyển đổi Office sang PDF."
            );
            return;
        }

        var key = fileKey(file);
        activePreviewKey = key;

        var cachedEntry = officePreviewCache.get(key);
        if (cachedEntry) {
            updateOfficePageCount(file, cachedEntry.pageCount);
            showPreviewSource(
                "pdf",
                cachedEntry.objectUrl,
                file.name,
                formatSize(file.size),
                "",
                false
            );
            return;
        }

        showPreviewFallback(
            file.name,
            formatSize(file.size),
            officePreviewRequests.has(key)
                ? "Bản xem trước đang được chuẩn bị. Bạn có thể đóng cửa sổ; quá trình vẫn tiếp tục."
                : "Đang chuyển đổi tài liệu Office sang PDF để xem trước..."
        );

        var originalButtonText = previewButton ? previewButton.textContent : "";
        if (previewButton) {
            previewButton.disabled = true;
            previewButton.textContent = "Đang chuẩn bị...";
        }

        try {
            setPageReadLoading(file);
            var entry = await createOfficePreviewRequest(file);
            updateOfficePageCount(file, entry.pageCount);

            if (activePreviewKey === key && previewDialog && previewDialog.open) {
                showPreviewSource(
                    "pdf",
                    entry.objectUrl,
                    file.name,
                    formatSize(file.size),
                    "",
                    false
                );
            }
        } catch (error) {
            if (error && error.name === "AbortError") {
                return;
            }

            console.error("Office preview failed.", error);
            setPageReadError(
                file,
                error && error.message ? error.message : "Không đọc được số trang file Office."
            );

            if (activePreviewKey === key && previewDialog && previewDialog.open) {
                showPreviewFallback(
                    file.name,
                    formatSize(file.size),
                    error && error.message
                        ? error.message
                        : "Không thể tạo bản xem trước file Office."
                );
            }
        } finally {
            if (previewButton) {
                previewButton.disabled = false;
                previewButton.textContent = originalButtonText || "Xem trước";
            }
        }
    }

    function showLocalPreview(file, previewButton) {
        var kind = previewKindOf(file.name);

        if (kind === "office") {
            showOfficePreview(file, previewButton);
            return;
        }

        var objectUrl = URL.createObjectURL(file);
        showPreviewSource(kind, objectUrl, file.name, formatSize(file.size), "", true);
    }

    function showExistingPreview(card) {
        var kind = card.dataset.previewKind || "office";
        var previewUrl = card.dataset.previewUrl || "";
        var downloadUrl = card.dataset.downloadUrl || previewUrl;
        var fileName = card.dataset.fileName || "Tài liệu";
        var fileSize = card.dataset.fileSize || "";

        if (kind === "office") {
            showPreviewSource(kind, "", fileName, fileSize, downloadUrl, false);
            return;
        }

        showPreviewSource(kind, previewUrl, fileName, fileSize, downloadUrl, false);
    }

    function getSelectedLabel(selectElement) {
        if (!selectElement || selectElement.selectedIndex < 0) {
            return "-";
        }

        return selectElement.options[selectElement.selectedIndex].text || "-";
    }

    function toPositiveNumber(value) {
        var number = parseInt(value || "0", 10);
        return Number.isNaN(number) || number < 0 ? 0 : number;
    }

    function updateDeliveryVisibility() {
        if (!controls.deliveryMethod || !deliveryAddressWrap || !controls.deliveryAddress) {
            return;
        }

        var isShipping = controls.deliveryMethod.value === "2";
        deliveryAddressWrap.classList.toggle("is-hidden", !isShipping);
        controls.deliveryAddress.required = isShipping;
    }

    function pageSummary() {
        var knownPages = 0;
        var unknownFiles = 0;

        selectedExistingCards().forEach(function (card) {
            var input = card.querySelector(".file-page-input");
            var kind = card.dataset.previewKind || "office";
            var pages = toPositiveNumber(input ? input.value : "0");
            if (kind === "image") {
                pages = 1;
            }
            if (pages > 0) {
                knownPages += pages;
            } else {
                unknownFiles++;
            }
        });

        selectedUploadFiles.forEach(function (file) {
            var key = fileKey(file);
            var status = uploadPageStatuses.get(key) || "loading";
            var totalPages = readPageNumber(uploadPageCounts.get(key) || "0");
            var pageFrom = readPageNumber(uploadPageFroms.get(key) || "0");
            var pageTo = readPageNumber(uploadPageTos.get(key) || "0");
            if (status === "ready" && totalPages > 0 && pageFrom >= 1 && pageTo <= totalPages && pageFrom <= pageTo) {
                knownPages += pageTo - pageFrom + 1;
            } else {
                unknownFiles++;
            }
        });

        return { knownPages: knownPages, unknownFiles: unknownFiles };
    }

    function updateSummary() {
        var fileCount = totalSelectedCount();
        var copies = toPositiveNumber(controls.copies ? controls.copies.value : "1");
        var pages = pageSummary();

        if (summary.fileCount) {
            summary.fileCount.textContent = fileCount.toString();
        }
        if (summary.uploadSize) {
            summary.uploadSize.textContent = formatSize(totalUploadSize());
        }
        if (summary.paperSize) {
            summary.paperSize.textContent = getSelectedLabel(controls.paperSize);
        }
        if (summary.printSide) {
            summary.printSide.textContent = getSelectedLabel(controls.printSide);
        }
        if (summary.colorMode) {
            var colorLabel = getSelectedLabel(controls.colorMode);
            if (controls.isPhoto && controls.isPhoto.checked) {
                colorLabel += " + in ảnh";
            }
            summary.colorMode.textContent = colorLabel;
        }
        if (summary.copies) {
            summary.copies.textContent = copies.toString();
        }
        if (summary.pages) {
            summary.pages.textContent = pages.unknownFiles > 0
                ? pages.knownPages + " + " + pages.unknownFiles + " file chờ đọc"
                : pages.knownPages.toString();
        }
        if (summary.volume) {
            var volume = pages.knownPages * copies;
            summary.volume.textContent = pages.unknownFiles > 0 ? "Từ " + volume : volume.toString();
        }
        if (summary.delivery) {
            summary.delivery.textContent = getSelectedLabel(controls.deliveryMethod);
        }

        if (summary.totalAmount) {
            calculatePriceDebounced(fileCount, pages.knownPages, copies);
        }

        updateSubmitAvailability();
    }

    var priceCalcTimeout = null;
    function calculatePriceDebounced(fileCount, knownPages, copies) {
        if (priceCalcTimeout) {
            clearTimeout(priceCalcTimeout);
        }

        if (fileCount === 0 || knownPages === 0 || !calculatePriceUrl) {
            if (summary.totalAmount) summary.totalAmount.textContent = "0 đ";
            return;
        }

        if (summary.totalAmount) {
            summary.totalAmount.textContent = "Đang tính...";
        }

        priceCalcTimeout = setTimeout(function () {
            var avgPages = Math.max(1, Math.round(knownPages / fileCount));
            var requestData = {
                paperSize: parseInt(controls.paperSize.value, 10),
                printSide: parseInt(controls.printSide.value, 10),
                colorMode: parseInt(controls.colorMode.value, 10),
                isPhoto: controls.isPhoto.checked,
                copies: copies,
                totalPages: avgPages,
                deliveryMethod: parseInt(controls.deliveryMethod.value, 10)
            };

            fetch(calculatePriceUrl, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": antiForgeryToken ? antiForgeryToken.value : ""
                },
                body: JSON.stringify(requestData)
            })
            .then(function (response) {
                if (!response.ok) throw new Error("Price calculation failed");
                return response.json();
            })
            .then(function (data) {
                if (summary.totalAmount && data.totalAmount !== undefined) {
                    summary.totalAmount.textContent = "Từ " + data.totalAmount.toLocaleString("vi-VN") + " đ";
                }
            })
            .catch(function (error) {
                if (summary.totalAmount) summary.totalAmount.textContent = "Không tính được";
            });
        }, 500);
    }

    function activatePreset(button) {
        presetButtons.forEach(function (item) {
            item.classList.toggle("is-active", item === button);
        });

        if (controls.paperSize && button.dataset.paperSize) {
            controls.paperSize.value = button.dataset.paperSize;
        }
        if (controls.printSide && button.dataset.printSide) {
            controls.printSide.value = button.dataset.printSide;
        }
        if (controls.colorMode && button.dataset.colorMode) {
            controls.colorMode.value = button.dataset.colorMode;
        }
        if (controls.isPhoto) {
            controls.isPhoto.checked = button.dataset.isPhoto === "true";
        }
        updateSummary();
    }

    function validateBeforeSubmit() {
        clearClientError();
        if (totalSelectedCount() === 0) {
            showClientError("Vui lòng chọn ít nhất một tài liệu để in.");
            if (dropzone) {
                dropzone.focus();
            }
            return false;
        }

        if (totalSelectedCount() > maxFiles) {
            showClientError("Mỗi lần chỉ được chọn tối đa 5 tài liệu.");
            return false;
        }

        var invalidFile = null;
        var invalidMessage = "";
        selectedUploadFiles.some(function (file) {
            var key = fileKey(file);
            var status = uploadPageStatuses.get(key) || "loading";
            if (status === "loading") {
                invalidFile = file;
                invalidMessage = "Hệ thống đang đọc số trang của file “" + file.name + "”. Vui lòng chờ hoàn tất.";
                return true;
            }
            if (status === "error") {
                invalidFile = file;
                invalidMessage = "Không thể đọc số trang của file “" + file.name + "”: " + (uploadPageErrors.get(key) || "lỗi không xác định");
                return true;
            }
            if (!validateUploadPageRange(file, true)) {
                invalidFile = file;
                invalidMessage = "Vui lòng sửa phạm vi trang của file “" + file.name + "”.";
                return true;
            }
            return false;
        });

        if (invalidFile) {
            showClientError(invalidMessage);
            return false;
        }

        return true;
    }

    if (fileInput) {
        fileInput.addEventListener("change", function () {
            addFiles(fileInput.files);
        });
    }

    if (dropzone && fileInput) {
        dropzone.addEventListener("click", function (event) {
            if (event.target === fileInput) {
                return;
            }
            fileInput.click();
        });
        dropzone.addEventListener("keydown", function (event) {
            if (event.key === "Enter" || event.key === " ") {
                event.preventDefault();
                fileInput.click();
            }
        });
        ["dragenter", "dragover"].forEach(function (eventName) {
            dropzone.addEventListener(eventName, function (event) {
                event.preventDefault();
                dropzone.classList.add("is-dragging");
            });
        });
        ["dragleave", "drop"].forEach(function (eventName) {
            dropzone.addEventListener(eventName, function (event) {
                event.preventDefault();
                dropzone.classList.remove("is-dragging");
            });
        });
        dropzone.addEventListener("drop", function (event) {
            addFiles(event.dataTransfer ? event.dataTransfer.files : []);
        });
    }

    existingCards.forEach(function (card) {
        var checkbox = card.querySelector('input[type="checkbox"][name="ExistingFileIds"]');
        var pageInput = card.querySelector(".file-page-input");
        var previewButton = card.querySelector(".existing-preview-button");
        var kind = card.dataset.previewKind || "office";

        if (kind === "image" && pageInput) {
            pageInput.value = "1";
            pageInput.readOnly = true;
        }

        if (checkbox) {
            checkbox.addEventListener("change", function () {
                if (checkbox.checked && totalSelectedCount() > maxFiles) {
                    checkbox.checked = false;
                    showClientError("Mỗi lần chỉ được chọn tối đa 5 tài liệu, gồm cả file mới và file đã upload.");
                }
                card.classList.toggle("is-selected", checkbox.checked);
                if (checkbox.checked) {
                    showExistingPreview(card);
                }
                updateSummary();
            });
        }

        if (pageInput) {
            pageInput.addEventListener("input", updateSummary);
        }

        if (previewButton) {
            previewButton.addEventListener("click", function () {
                showExistingPreview(card);
            });
        }
    });

    if (previewCloseButton) {
        previewCloseButton.addEventListener("click", closePreviewDialog);
    }

    if (previewDialog) {
        previewDialog.addEventListener("click", function (event) {
            if (event.target === previewDialog) {
                closePreviewDialog();
            }
        });

        previewDialog.addEventListener("cancel", function (event) {
            event.preventDefault();
            closePreviewDialog();
        });

        previewDialog.addEventListener("close", function () {
            activePreviewKey = null;
            document.documentElement.classList.remove("preview-dialog-open");
            clearPreviewSurface();
        });
    }

    [controls.paperSize, controls.printSide, controls.colorMode, controls.copies, controls.deliveryMethod, controls.isPhoto].forEach(function (control) {
        if (!control) {
            return;
        }
        control.addEventListener("change", function () {
            updateDeliveryVisibility();
            updateSummary();
        });
        control.addEventListener("input", updateSummary);
    });

    presetButtons.forEach(function (button) {
        button.addEventListener("click", function () {
            activatePreset(button);
        });
    });

    form.addEventListener("submit", function (event) {
        if (!validateBeforeSubmit()) {
            event.preventDefault();
            return;
        }

        if (window.jQuery && window.jQuery.validator && !window.jQuery(form).valid()) {
            event.preventDefault();
            return;
        }

        if (submitButton) {
            submitButton.disabled = true;
            var text = submitButton.querySelector("span");
            if (text) {
                text.textContent = "Đang gửi tài liệu...";
            }
        }
    });

    window.addEventListener("beforeunload", disposeAllPreviewResources);

    renderUploadQueue();
    updateDeliveryVisibility();
    updateSummary();

    var firstSelectedExisting = selectedExistingCards()[0];
    if (firstSelectedExisting) {
        showExistingPreview(firstSelectedExisting);
    }
})();
