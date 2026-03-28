(function () {
    var form = document.getElementById("printJobForm");
    if (!form) {
        return;
    }

    var controls = {
        paperSize: document.getElementById("PaperSize"),
        printSide: document.getElementById("PrintSide"),
        colorMode: document.getElementById("ColorMode"),
        copies: document.getElementById("Copies"),
        totalPages: document.getElementById("TotalPages"),
        deliveryMethod: document.getElementById("DeliveryMethod"),
        isPhoto: document.getElementById("IsPhoto"),
        deliveryAddress: document.getElementById("DeliveryAddress")
    };

    var summary = {
        paperSize: document.getElementById("summaryPaperSize"),
        printSide: document.getElementById("summaryPrintSide"),
        colorMode: document.getElementById("summaryColorMode"),
        copies: document.getElementById("summaryCopies"),
        pages: document.getElementById("summaryPages"),
        volume: document.getElementById("summaryVolume"),
        delivery: document.getElementById("summaryDelivery")
    };

    var deliveryAddressWrap = document.getElementById("deliveryAddressWrap");
    var presetButtons = Array.prototype.slice.call(document.querySelectorAll(".quick-preset"));

    function getSelectedLabel(selectElement) {
        if (!selectElement) {
            return "-";
        }

        var index = selectElement.selectedIndex;
        if (index < 0) {
            return "-";
        }

        var option = selectElement.options[index];
        return option ? option.text : "-";
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

        if (!isShipping && !controls.deliveryAddress.value.trim()) {
            controls.deliveryAddress.value = "";
        }
    }

    function updateSummary() {
        var copies = toPositiveNumber(controls.copies ? controls.copies.value : "0");
        var pages = toPositiveNumber(controls.totalPages ? controls.totalPages.value : "0");
        var volume = copies * pages;

        if (summary.paperSize) {
            summary.paperSize.textContent = getSelectedLabel(controls.paperSize);
        }

        if (summary.printSide) {
            summary.printSide.textContent = getSelectedLabel(controls.printSide);
        }

        if (summary.colorMode) {
            var colorLabel = getSelectedLabel(controls.colorMode);
            if (controls.isPhoto && controls.isPhoto.checked) {
                colorLabel += " + In ảnh";
            }
            summary.colorMode.textContent = colorLabel;
        }

        if (summary.copies) {
            summary.copies.textContent = copies.toString();
        }

        if (summary.pages) {
            summary.pages.textContent = pages.toString();
        }

        if (summary.volume) {
            summary.volume.textContent = volume.toString();
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

    [
        controls.paperSize,
        controls.printSide,
        controls.colorMode,
        controls.copies,
        controls.totalPages,
        controls.deliveryMethod,
        controls.isPhoto
    ].forEach(function (input) {
        if (!input) {
            return;
        }

        input.addEventListener("change", function () {
            updateDeliveryVisibility();
            updateSummary();
        });

        input.addEventListener("input", function () {
            updateSummary();
        });
    });

    presetButtons.forEach(function (button) {
        button.addEventListener("click", function () {
            activatePreset(button);
        });
    });

    updateDeliveryVisibility();
    updateSummary();
})();
