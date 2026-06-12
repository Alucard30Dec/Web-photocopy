(function () {
    "use strict";

    var form = document.getElementById("printJobForm");
    if (!form) {
        return;
    }

    var maxFiles = 5;
    var maxFileSize = 20 * 1024 * 1024;
    var maxBatchSize = 100 * 1024 * 1024;
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
        delivery: document.getElementById("summaryDelivery")
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
    var previewDownloadLink = document.getElementById("previewDownloadLink");

    var selectedUploadFiles = [];
    var uploadPageCounts = new Map();
    var activeObjectUrl = null;

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

            if (imageExtensions.indexOf(extensionOf(file.name)) >= 0) {
                uploadPageCounts.set(fileKey(file), "1");
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

        uploadPageCounts.delete(fileKey(removed));
        selectedUploadFiles.splice(index, 1);
        synchronizeFileInput();
        renderUploadQueue();
        updateSummary();
        clearClientError();
    }

    function createUploadCard(file, index) {
        var extension = extensionOf(file.name);
        var kind = previewKindOf(file.name);
        var card = document.createElement("article");
        card.className = "upload-file-card";

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
        meta.textContent = formatSize(file.size) + (kind === "office" ? " • cần nhập số trang" : kind === "image" ? " • 1 trang" : " • tự đọc số trang PDF");

        text.appendChild(name);
        text.appendChild(meta);
        identity.appendChild(type);
        identity.appendChild(text);

        var pageLabel = document.createElement("label");
        pageLabel.className = "upload-file-card__pages";
        var pageTitle = document.createElement("span");
        pageTitle.textContent = "Số trang";
        var pageInput = document.createElement("input");
        pageInput.type = "number";
        pageInput.name = "UploadPageCounts";
        pageInput.min = "1";
        pageInput.max = "10000";
        pageInput.inputMode = "numeric";
        pageInput.className = "form-control form-control-sm file-page-input";
        pageInput.dataset.uploadKey = fileKey(file);
        pageInput.dataset.pageKind = kind;
        pageInput.value = uploadPageCounts.get(fileKey(file)) || "";
        pageInput.placeholder = kind === "pdf" ? "Tự nhận" : kind === "image" ? "1" : "Bắt buộc";
        if (kind === "image") {
            pageInput.value = "1";
            pageInput.readOnly = true;
        }
        pageInput.addEventListener("input", function () {
            uploadPageCounts.set(fileKey(file), pageInput.value);
            updateSummary();
        });
        pageLabel.appendChild(pageTitle);
        pageLabel.appendChild(pageInput);

        var actions = document.createElement("div");
        actions.className = "upload-file-card__actions";

        var previewButton = document.createElement("button");
        previewButton.type = "button";
        previewButton.className = "btn btn-sm btn-outline-primary";
        previewButton.textContent = "Xem trước";
        previewButton.setAttribute("aria-haspopup", "dialog");
        previewButton.setAttribute("aria-controls", "documentPreviewDialog");
        previewButton.addEventListener("click", function () {
            showLocalPreview(file);
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
        card.appendChild(pageLabel);
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
        });
    }

    function revokeActiveObjectUrl() {
        if (activeObjectUrl) {
            URL.revokeObjectURL(activeObjectUrl);
            activeObjectUrl = null;
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
        if (!previewDialog) {
            resetPreview();
            return;
        }

        if (previewDialog.open && typeof previewDialog.close === "function") {
            previewDialog.close();
            return;
        }

        previewDialog.removeAttribute("open");
        previewDialog.classList.remove("is-open");
        document.documentElement.classList.remove("preview-dialog-open");
        resetPreview();
    }

    function resetPreview() {
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
        if (previewEmpty) {
            previewEmpty.hidden = true;
        }
        if (previewDownloadLink) {
            previewDownloadLink.hidden = true;
            previewDownloadLink.removeAttribute("href");
        }
    }

    function showPreviewSource(kind, sourceUrl, fileName, fileMeta, downloadUrl, isObjectUrl) {
        resetPreview();
        if (isObjectUrl) {
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

    function showLocalPreview(file) {
        var kind = previewKindOf(file.name);
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
            var kind = previewKindOf(file.name);
            var pages = toPositiveNumber(uploadPageCounts.get(fileKey(file)) || "0");
            if (kind === "image") {
                pages = 1;
            }
            if (pages > 0) {
                knownPages += pages;
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

        var missingPageFile = "";
        selectedExistingCards().some(function (card) {
            var kind = card.dataset.previewKind || "office";
            var pageInput = card.querySelector(".file-page-input");
            if (kind === "office" && toPositiveNumber(pageInput ? pageInput.value : "0") === 0) {
                missingPageFile = card.dataset.fileName || "file Office";
                if (pageInput) {
                    pageInput.focus();
                }
                return true;
            }
            return false;
        });

        if (!missingPageFile) {
            selectedUploadFiles.some(function (file) {
                if (officeExtensions.indexOf(extensionOf(file.name)) >= 0 && toPositiveNumber(uploadPageCounts.get(fileKey(file)) || "0") === 0) {
                    missingPageFile = file.name;
                    var pageInput = null;
                    if (uploadQueue) {
                        pageInput = Array.prototype.slice.call(uploadQueue.querySelectorAll("[data-upload-key]")).find(function (input) {
                            return input.dataset.uploadKey === fileKey(file);
                        }) || null;
                    }
                    if (pageInput) {
                        pageInput.focus();
                    }
                    return true;
                }
                return false;
            });
        }

        if (missingPageFile) {
            showClientError("Vui lòng nhập số trang thực tế của file “" + missingPageFile + "”.");
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
            document.documentElement.classList.remove("preview-dialog-open");
            resetPreview();
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

    window.addEventListener("beforeunload", revokeActiveObjectUrl);

    renderUploadQueue();
    updateDeliveryVisibility();
    updateSummary();

    var firstSelectedExisting = selectedExistingCards()[0];
    if (firstSelectedExisting) {
        showExistingPreview(firstSelectedExisting);
    }
})();
