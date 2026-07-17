
const canManagePRAndPriority = window.AppConfig?.canManagePRAndPriority || false;
let ewsCurrentPage = 1;
const ewsItemsPerPage = 50;
let ewsCombinedListGlobal = [];
let datasetSparepart = [];
let currentTableData = [];
let sortState = { col: -1, asc: true };
let currentPage = 1;
const itemsPerPage = 100
let isPRModeActive = false;
let isPriorityModeActive = false;
let prSelections = {};

document.addEventListener("DOMContentLoaded", () => {
    injectControlButtons();
    fetchDataDariServer();
});

function injectControlButtons() {
    const ewsCardHeader = document.querySelector("#ewsCount").parentElement;
    if (ewsCardHeader) {
        const btnContainer = document.createElement("div");
        btnContainer.className = "float-end d-flex gap-2 align-items-center";

        let buttonsHtml = `
                <div class="input-group input-group-sm d-none shadow-sm" id="ewsSearchContainer" style="max-width: 220px;">
                    <span class="input-group-text bg-white border-end-0"><i class="bi bi-search text-muted"></i></span>
                    <input type="text" id="ewsSearchInput" class="form-control border-start-0 ps-0" placeholder="Cari Part di EWS..." oninput="renderEWSContainer()">
                </div>
            `;

        if (canManagePRAndPriority) {
            buttonsHtml += `
                <button id="btnTogglePriority" class="btn btn-sm btn-outline-warning fw-bold text-dark" onclick="togglePriorityMode()">
                    <i class="bi bi-star-fill me-1 text-warning"></i> Set Priority Mode
                </button>
                <button id="btnSavePriority" class="btn btn-sm btn-warning fw-bold d-none text-dark" onclick="executeSavePriorities()">
                    <i class="bi bi-save2 me-1"></i> Simpan List Prioritas
                </button>
                <button id="btnTogglePR" class="btn btn-sm btn-outline-primary fw-bold" onclick="togglePRMode()">
                    <i class="bi bi-file-earmark-spreadsheet me-1"></i> Buat Manual PR
                </button>
                <button id="btnDownloadPR" class="btn btn-sm btn-success fw-bold d-none" onclick="executeDownloadPR()">
                    <i class="bi bi-download me-1"></i> Unduh PR Excel
                </button>
                `;
        }

        btnContainer.innerHTML = buttonsHtml;
        ewsCardHeader.appendChild(btnContainer);
    }
}

async function fetchDataDariServer() {
    try {
        const response = await fetch('/Sparepart/GetApiData');
        const dbDataRaw = await response.json();

        // MAPPING BARU DARI JSON (Tanpa Dormant dan Bin Lokasi)
        datasetSparepart = dbDataRaw.map(item => {
            return {
                materialNo: item.Material ? item.Material.trim() : "-",
                description: item.MaterialDescription ? item.MaterialDescription.trim() : "-",
                uom: item.UoM ? item.UoM.trim() : "PC",
                actualStock: item.CurrentStock || 0,
                safetyStock: item.SafetyStock || 0,
                storLoct: item.StorLoct ? item.StorLoct.trim() : "-",
                priority: item.Priority ? item.Priority.trim() : ""
            };
        });
        currentTableData = [...datasetSparepart];
        document.getElementById('lblTotalCount').innerText = datasetSparepart.length;
        renderEWSContainer();
        renderMainTableRows();
    } catch (error) { console.error("Gagal menarik data:", error); }
}

function togglePriorityMode() {
    if (isPRModeActive) togglePRMode();
    isPriorityModeActive = !isPriorityModeActive;
    const btnToggle = document.getElementById("btnTogglePriority");
    const btnSave = document.getElementById("btnSavePriority");
    const searchContainer = document.getElementById("ewsSearchContainer");
    const searchInput = document.getElementById("ewsSearchInput");

    if (isPriorityModeActive) {
        btnToggle.innerHTML = `<i class="bi bi-x-circle me-1"></i> Batal Prioritas`;
        btnToggle.className = "btn btn-sm btn-danger fw-bold";
        btnSave.classList.remove("d-none");
        searchContainer.classList.remove("d-none"); // Tampilkan pencarian khusus EWS
        searchInput.focus();
    } else {
        btnToggle.innerHTML = `<i class="bi bi-star-fill me-1 text-warning"></i> Set Priority Mode`;
        btnToggle.className = "btn btn-sm btn-outline-warning fw-bold text-dark";
        btnSave.classList.add("d-none");
        searchContainer.classList.add("d-none"); // Sembunyikan pencarian khusus EWS
        searchInput.value = "";
    }
    renderEWSContainer();
}

async function executeSavePriorities() {
    savePriorityState(); // Pastikan halaman terakhir yang sedang tampil ikut terekam

    // Ambil semua nomor material yang status prioritasnya bernilai 'Y' dari memori global
    let listPriorityMaterialNos = datasetSparepart
        .filter(item => item.priority === 'Y')
        .map(item => item.materialNo);

    if (!confirm(`Simpan ${listPriorityMaterialNos.length} item sebagai prioritas?`)) return;

    const btnSave = document.getElementById("btnSavePriority");
    btnSave.disabled = true;
    btnSave.innerHTML = `<span class="spinner-border spinner-border-sm me-1"></span> Menyimpan...`;

    try {
        const response = await fetch('/Sparepart/UpdatePriorities', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(listPriorityMaterialNos)
        });

        if (response.ok) {
            alert("Daftar prioritas sparepart berhasil diperbarui!");
            togglePriorityMode();
            await fetchDataDariServer();
        } else { alert("Gagal menyimpan prioritas."); }
    } catch (error) { alert("Kesalahan jaringan."); }
    finally {
        btnSave.disabled = false;
        btnSave.innerHTML = `<i class="bi bi-save2 me-1"></i> Simpan List Prioritas`;
    }
}

function togglePRMode() {
    if (isPriorityModeActive) togglePriorityMode();

    isPRModeActive = !isPRModeActive;
    const btnToggle = document.getElementById("btnTogglePR");
    const btnDownload = document.getElementById("btnDownloadPR");
    const searchContainer = document.getElementById("ewsSearchContainer");
    const searchInput = document.getElementById("ewsSearchInput");

    if (isPRModeActive) {
        btnToggle.innerHTML = `<i class="bi bi-x-circle me-1"></i> Batal PR`;
        btnToggle.className = "btn btn-sm btn-danger fw-bold";
        btnDownload.classList.remove("d-none");
        searchContainer.classList.remove("d-none");
    } else {
        btnToggle.innerHTML = `<i class="bi bi-file-earmark-spreadsheet me-1"></i> Buat Manual PR`;
        btnToggle.className = "btn btn-sm btn-outline-primary fw-bold";
        btnDownload.classList.add("d-none");
        searchContainer.classList.add("d-none");
        searchInput.value = "";
        prSelections = {};
    }
    renderEWSContainer();
}

function savePRState() {
    if (!isPRModeActive) return;
    const rows = document.querySelectorAll("#ewsTableBody tr");
    rows.forEach(row => {
        const chk = row.querySelector(".chk-pr-item");
        if (chk) {
            const matNo = chk.getAttribute("data-matno");
            prSelections[matNo] = {
                checked: chk.checked,
                qty: row.querySelector(".txt-pr-qty").value,
                remark: row.querySelector(".txt-pr-remark").value
            };
        }
    });
}

function savePriorityState() {
    if (!isPriorityModeActive) return;
    const rows = document.querySelectorAll("#ewsTableBody tr");
    rows.forEach(row => {
        const chk = row.querySelector(".chk-priority-flag");
        if (chk) {
            const matNo = chk.getAttribute("data-matno");
            // Cari data asli di memori global dan perbarui properti priority-nya secara real-time
            const item = datasetSparepart.find(x => x.materialNo === matNo);
            if (item) {
                item.priority = chk.checked ? "Y" : "";
            }
        }
    });
}

function renderEWSContainer() {
    savePRState();
    savePriorityState(); // AMAN KAN CENTANGAN PRIORITAS SEBELUM FILTER BERJALAN

    let outOfStockList = datasetSparepart.filter(item => item.actualStock === 0);
    let criticalStockList = datasetSparepart.filter(item => item.actualStock > 0 && item.actualStock <= item.safetyStock);
    let ewsCombinedList = [...outOfStockList, ...criticalStockList];

    const searchInput = document.getElementById("ewsSearchInput");
    // PERBAIKAN: Aktifkan filter pencarian untuk Mode PR ATAU Mode Prioritas
    if ((isPRModeActive || isPriorityModeActive) && searchInput && searchInput.value.trim() !== "") {
        const searchQuery = searchInput.value.toLowerCase().trim();
        let tokens = searchQuery.split(" ").filter(t => t !== "");
        ewsCombinedList = ewsCombinedList.filter(item => {
            let textToSearch = `${item.materialNo} ${item.description}`.toLowerCase();
            return tokens.every(token => textToSearch.includes(token));
        });
    }

    ewsCombinedList.sort((a, b) => {
        if (a.priority === 'Y' && b.priority !== 'Y') return -1;
        if (a.priority !== 'Y' && b.priority === 'Y') return 1;
        return 0;
    });

    const theadRow = document.getElementById("ewsTheadRow");
    document.getElementById('ewsCount').innerText = `${ewsCombinedList.length} Item`;

    if (isPriorityModeActive) {
        theadRow.innerHTML = `<th style="width: 50px;">Prioritas</th><th>Material No</th><th>Description</th><th class="text-center">Actual Stock</th><th class="text-center">Status</th>`;
    } else if (isPRModeActive) {
        theadRow.innerHTML = `<th style="width: 40px;">Pilih</th><th>Material No</th><th>Description</th><th style="width: 90px;">Qty Order</th><th>Actual Stock</th><th>Remark PR</th>`;
    } else {
        theadRow.innerHTML = `<th>Material No</th><th>Description</th><th>Storage Loct</th><th class="text-center">Safety</th><th class="text-center">Actual</th><th class="text-center">Status</th>`;
    }

    ewsCombinedListGlobal = ewsCombinedList;
    ewsCurrentPage = 1;
    renderEwsTableRows();
}

function renderEwsTableRows() {
    const tbody = document.getElementById('ewsTableBody');
    const totalItems = ewsCombinedListGlobal.length;

    if (totalItems === 0) {
        tbody.innerHTML = `<tr><td colspan="6" class="text-center py-4 text-muted">Kosong.</td></tr>`;
        document.getElementById('lblEwsStart').innerText = 0;
        document.getElementById('lblEwsEnd').innerText = 0;
        document.getElementById('lblEwsTotal').innerText = 0; // Reset ke 0
        document.getElementById('ewsPagination').innerHTML = "";
        return;
    }

    // LOGIKA SLICING (MEMOTONG DATA SESUAI HALAMAN)
    const totalPages = Math.ceil(totalItems / ewsItemsPerPage);
    if (ewsCurrentPage > totalPages) ewsCurrentPage = totalPages;
    if (ewsCurrentPage < 1) ewsCurrentPage = 1;

    const startIndex = (ewsCurrentPage - 1) * ewsItemsPerPage;
    const endIndex = Math.min(startIndex + ewsItemsPerPage, totalItems);
    const paginatedData = ewsCombinedListGlobal.slice(startIndex, endIndex);

    let htmlGrid = "";
    paginatedData.forEach((item) => {
        let isZero = item.actualStock === 0;
        let isItemPriority = item.priority === 'Y';
        let rowClass = isZero ? "table-danger" : "table-warning";

        if (isItemPriority && !isPriorityModeActive && !isPRModeActive) {
            rowClass = "table-secondary border-start border-4 border-warning";
        }

        if (isPriorityModeActive) {
            let isChecked = isItemPriority ? "checked" : "";
            htmlGrid += `
                <tr class="${rowClass}">
                    <td class="text-center"><input type="checkbox" class="form-check-input border-warning chk-priority-flag" data-matno="${item.materialNo}" ${isChecked}></td>
                    <td class="font-monospace fw-bold">${item.materialNo} ${isItemPriority ? '⭐' : ''}</td>
                    <td class="fw-bold text-truncate" style="max-width: 320px;">${item.description}</td>
                    <td class="text-center fw-bold">${item.actualStock} ${item.uom}</td>
                    <td class="text-center"><span class="badge ${isZero ? 'bg-danger' : 'bg-warning text-dark'} fw-bold">${isZero ? 'HABIS' : 'KRITIS'}</span></td>
                </tr>`;
        } else if (isPRModeActive) {
            let memory = prSelections[item.materialNo] || {};
            let isChecked = memory.checked ? "checked" : "";
            let savedQty = memory.qty || "1";
            let savedRemark = memory.remark || "";
            htmlGrid += `
                <tr class="${rowClass}">
                    <td class="text-center"><input type="checkbox" class="form-check-input chk-pr-item" data-matno="${item.materialNo}" ${isChecked}></td>
                    <td class="font-monospace fw-bold">${item.materialNo} ${isItemPriority ? '⭐' : ''}</td>
                    <td class="fw-bold text-truncate" style="max-width: 220px;">${item.description}</td>
                    <td><input type="number" class="form-control form-control-sm txt-pr-qty" min="1" value="${savedQty}" style="padding: 2px 5px;"></td>
                    <td class="text-center fw-bold">${item.actualStock} ${item.uom}</td>
                    <td><input type="text" class="form-control form-control-sm txt-pr-remark" value="${savedRemark}" placeholder="Urgent" style="padding: 2px 5px;"></td>
                </tr>`;
        } else {
            let badgeClass = isZero ? "bg-danger" : "bg-warning text-dark border border-warning";
            let statusText = isZero ? "HABIS" : "KRITIS";
            if (isItemPriority) statusText = "PRIORITAS";

            htmlGrid += `
                <tr class="${rowClass} ews-row" onclick="focusSearchToItem('${item.materialNo}')" style="cursor:pointer;">
                    <td class="font-monospace fw-bold">${item.materialNo}</td>
                    <td class="fw-bold text-truncate" style="max-width: 250px;">${item.description}</td>
                    <td><span class="badge bg-secondary-subtle text-secondary-emphasis border">${item.storLoct}</span></td>
                    <td class="text-center text-muted fw-bold">${item.safetyStock}</td>
                    <td class="text-center fw-bold ${isZero ? 'text-danger' : ''}">${item.actualStock} <span class="small">${item.uom}</span></td>
                    <td class="text-center"><span class="badge ${badgeClass} fw-bold w-100">${statusText}</span></td>
                </tr>`;
        }
    });

    tbody.innerHTML = htmlGrid;

    // =========================================================
    // PERBAIKAN SINKRONISASI LABEL FOOTER EWS
    // =========================================================
    document.getElementById('lblEwsStart').innerText = startIndex + 1;
    document.getElementById('lblEwsEnd').innerText = endIndex;
    document.getElementById('lblEwsTotal').innerText = totalItems.toLocaleString('id-ID'); // Format ribuan ala Indonesia

    renderEwsPagination(totalPages);
}

function renderEwsPagination(totalPages) {
    const ul = document.getElementById('ewsPagination');
    let html = "";

    // Tombol Mundur
    html += `<li class="page-item ${ewsCurrentPage === 1 ? 'disabled' : ''}">
                    <a class="page-link py-1 px-2 fw-bold" href="javascript:void(0)" onclick="changeEwsPage(${ewsCurrentPage - 1})">&laquo;</a>
                 </li>`;

    // Tombol Maju
    html += `<li class="page-item ${ewsCurrentPage === totalPages ? 'disabled' : ''}">
                    <a class="page-link py-1 px-2 fw-bold" href="javascript:void(0)" onclick="changeEwsPage(${ewsCurrentPage + 1})">&raquo;</a>
                 </li>`;

    ul.innerHTML = html;
}

function changeEwsPage(pageNum) {
    savePRState();
    savePriorityState(); // AMAN KAN CENTANGAN SEBELUM PINDAH HALAMAN
    ewsCurrentPage = pageNum;
    renderEwsTableRows();
}

async function executeDownloadPR() {
    savePRState();
    const payloadItems = [];
    for (const matNo in prSelections) {
        if (prSelections[matNo].checked) {
            payloadItems.push({ MaterialNo: matNo, Qty: parseFloat(prSelections[matNo].qty) || 1, Remark: prSelections[matNo].remark });
        }
    }
    if (payloadItems.length === 0) { alert("Silakan pilih item!"); return; }

    const btnDownload = document.getElementById("btnDownloadPR");
    const originalHtml = btnDownload.innerHTML;
    btnDownload.innerHTML = `<span class="spinner-border spinner-border-sm me-1"></span> Merakit...`;
    btnDownload.disabled = true;

    try {
        const response = await fetch('/Sparepart/ExportPR', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ Items: payloadItems })
        });
        if (response.ok) {
            const blob = await response.blob();
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `PR_Technical_P1_${new Date().toISOString().slice(0, 10).replace(/-/g, "")}.xlsx`;
            document.body.appendChild(a);
            a.click();
            a.remove();
            window.URL.revokeObjectURL(url);
            togglePRMode();
        } else { alert("Gagal memproses Purchase Request."); }
    } catch (error) { alert("Kesalahan jaringan."); }
    finally {
        document.getElementById("btnDownloadPR").disabled = false;
        document.getElementById("btnDownloadPR").innerHTML = `<i class="bi bi-download me-1"></i> Unduh PR Excel`;
    }
}

function performFiltering() {
    const searchQuery = document.getElementById('smartSearch').value.toLowerCase().trim();
    const statusFilter = document.getElementById('statusFilter').value;

    let filteredResult = datasetSparepart;
    if (searchQuery) {
        let tokens = searchQuery.split(" ").filter(t => t !== "");
        filteredResult = datasetSparepart.filter(item => {
            let textToSearch = `${item.materialNo} ${item.description} ${item.storLoct}`.toLowerCase();
            return tokens.every(token => textToSearch.includes(token));
        });
    }

    if (statusFilter === "PRIORITY_ONLY") {
        filteredResult = filteredResult.filter(item => item.priority === 'Y');
    } else if (statusFilter === "EMPTY") {
        filteredResult = filteredResult.filter(item => item.actualStock === 0);
    } else if (statusFilter === "CRITICAL") {
        filteredResult = filteredResult.filter(item => item.actualStock > 0 && item.actualStock <= item.safetyStock);
    } else if (statusFilter === "SAFE") {
        filteredResult = filteredResult.filter(item => item.actualStock > item.safetyStock);
    }

    filteredResult.sort((a, b) => {
        if (a.priority === 'Y' && b.priority !== 'Y') return -1;
        if (a.priority !== 'Y' && b.priority === 'Y') return 1;
        return 0;
    });

    currentTableData = filteredResult;
    currentPage = 1;
    renderMainTableRows();
}

function handleSearch() { performFiltering(); }
function handleStatusFilter() { performFiltering(); }
function focusSearchToItem(matNo) {
    document.getElementById('statusFilter').value = "ALL";
    document.getElementById('smartSearch').value = matNo;
    performFiltering();
    document.getElementById('smartSearch').focus();
}
function resetDashboard() {
    document.getElementById('smartSearch').value = "";
    document.getElementById('statusFilter').value = "ALL";
    currentTableData = [...datasetSparepart];
    sortState = { col: -1, asc: true };
    currentTableData.sort((a, b) => {
        if (a.priority === 'Y' && b.priority !== 'Y') return -1;
        if (a.priority !== 'Y' && b.priority === 'Y') return 1;
        return 0;
    });
    currentPage = 1;
    renderMainTableRows();
}

function sortTable(colIdx) {
    if (currentTableData.length === 0) return;
    if (sortState.col === colIdx) { sortState.asc = !sortState.asc; }
    else { sortState.col = colIdx; sortState.asc = true; }

    currentTableData.sort((a, b) => {
        let valA, valB;
        if (colIdx === 0) { valA = a.materialNo; valB = b.materialNo; }
        else if (colIdx === 1) { valA = a.description.toLowerCase(); valB = b.description.toLowerCase(); }
        else if (colIdx === 2) { valA = a.storLoct.toLowerCase(); valB = b.storLoct.toLowerCase(); }
        else if (colIdx === 3) { valA = a.safetyStock; valB = b.safetyStock; }
        else if (colIdx === 4) { valA = a.actualStock; valB = b.actualStock; }

        if (valA < valB) return sortState.asc ? -1 : 1;
        if (valA > valB) return sortState.asc ? 1 : -1;
        return 0;
    });
    renderMainTableRows();
}

function renderMainTableRows() {
    const tbody = document.getElementById('sparepartTableBody');

    // 1. Logika Jika Kosong
    if (currentTableData.length === 0) {
        tbody.innerHTML = `<tr><td colspan="6" class="text-center py-4 text-muted fs-6"><i class="bi bi-search me-2 fs-5"></i> Tidak ada material suku cadang yang cocok.</td></tr>`;
        document.getElementById('lblShowingStart').innerText = 0;
        document.getElementById('lblShowingEnd').innerText = 0;
        document.getElementById('lblTotalCount').innerText = 0;
        document.getElementById('paginationControls').innerHTML = "";
        return;
    }

    // 2. Logika Paginasi (Slicing Array)
    const totalItems = currentTableData.length;
    const totalPages = Math.ceil(totalItems / itemsPerPage);

    if (currentPage > totalPages) currentPage = totalPages;
    if (currentPage < 1) currentPage = 1;

    const startIndex = (currentPage - 1) * itemsPerPage;
    const endIndex = Math.min(startIndex + itemsPerPage, totalItems);
    const paginatedData = currentTableData.slice(startIndex, endIndex);

    // 3. Render HTML Hanya Untuk 100 Baris (Sangat Ringan!)
    let htmlBuffer = "";
    paginatedData.forEach(item => {
        let isZero = item.actualStock === 0;
        let isCritical = item.actualStock > 0 && item.actualStock <= item.safetyStock;
        let isItemPriority = item.priority === 'Y';

        let badgeComponent = `<span class="badge bg-success-subtle text-success border border-success fw-bold px-2 w-100">AMAN</span>`;
        let rowStyleClass = "";
        let textStockStyle = "fw-bold text-success";

        if (isZero) {
            badgeComponent = `<span class="badge bg-danger text-white fw-bold px-2 w-100">KOSONG</span>`;
            rowStyleClass = "table-danger opacity-90";
            textStockStyle = "text-danger fw-bold fs-6";
        } else if (isCritical) {
            badgeComponent = `<span class="badge bg-warning text-dark fw-bold px-2 border border-warning w-100">KRITIS</span>`;
            textStockStyle = "text-warning fw-bold fs-6";
            rowStyleClass = "table-warning";
        }

        if (isItemPriority && !isZero && !isCritical) {
            rowStyleClass = "table-secondary";
            badgeComponent = `<span class="badge bg-info text-white fw-bold px-2 w-100">PRIORITAS</span>`;
        } else if (isItemPriority) {
            badgeComponent = `<span class="badge ${isZero ? 'bg-danger' : 'bg-warning text-dark'} fw-bold px-2 w-100">PRIORITAS</span>`;
        }

        htmlBuffer += `
            <tr class="${rowStyleClass}">
                <td class="font-monospace fw-bold text-secondary text-nowrap">${item.materialNo} ${isItemPriority ? '⭐' : ''}</td>
                <td><div class="fw-bold text-uppercase text-truncate" style="max-width: 380px;">${item.description}</div></td>
                <td><span class="badge bg-secondary-subtle text-secondary-emphasis border font-monospace px-2">${item.storLoct}</span></td>
                <td class="text-center text-secondary fw-bold">${item.safetyStock}</td>
                <td class="text-center ${textStockStyle}">${item.actualStock} <span class="small text-muted fw-normal" style="font-size:0.7rem">${item.uom}</span></td>
                <td class="text-center">${badgeComponent}</td>
            </tr>`;
    });

    tbody.innerHTML = htmlBuffer;

    // 4. Perbarui Label Footer
    document.getElementById('lblShowingStart').innerText = startIndex + 1;
    document.getElementById('lblShowingEnd').innerText = endIndex;
    document.getElementById('lblTotalCount').innerText = totalItems.toLocaleString('id-ID');

    // 5. Gambar Tombol Navigasi Halaman
    renderPaginationControls(totalPages);
}

function renderPaginationControls(totalPages) {
    const ul = document.getElementById('paginationControls');
    let html = "";

    // Tombol Prev
    html += `<li class="page-item ${currentPage === 1 ? 'disabled' : ''}">
                    <a class="page-link" href="javascript:void(0)" onclick="changePage(${currentPage - 1})">Prev</a>
                 </li>`;

    // Logika Limit Tombol (Tampilkan max 5 tombol angka)
    let startPage = Math.max(1, currentPage - 2);
    let endPage = Math.min(totalPages, currentPage + 2);

    if (startPage > 1) {
        html += `<li class="page-item"><a class="page-link" href="javascript:void(0)" onclick="changePage(1)">1</a></li>`;
        if (startPage > 2) html += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
    }

    for (let i = startPage; i <= endPage; i++) {
        html += `<li class="page-item ${i === currentPage ? 'active' : ''}">
                        <a class="page-link" href="javascript:void(0)" onclick="changePage(${i})">${i}</a>
                     </li>`;
    }

    if (endPage < totalPages) {
        if (endPage < totalPages - 1) html += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
        html += `<li class="page-item"><a class="page-link" href="javascript:void(0)" onclick="changePage(${totalPages})">${totalPages}</a></li>`;
    }

    // Tombol Next
    html += `<li class="page-item ${currentPage === totalPages ? 'disabled' : ''}">
                    <a class="page-link" href="javascript:void(0)" onclick="changePage(${currentPage + 1})">Next</a>
                 </li>`;

    ul.innerHTML = html;
}

function changePage(pageNum) {
    currentPage = pageNum;
    renderMainTableRows();
    // Gulir kembali ke atas tabel secara halus saat ganti halaman
    document.getElementById('statusFilter').scrollIntoView({ behavior: 'smooth', block: 'start' });
}
