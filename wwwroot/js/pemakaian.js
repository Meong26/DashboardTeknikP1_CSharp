
    // DEKLARASI VARIABEL GLOBAL
    let masterSparepart = [];
    let masterTeknisi = [];
    let historyDatasetEstimasi = [];
    let historyDatasetFix = [];
    let historyDatasetKarantina = [];
    let serverCurrentWeek = "";
    let yr21OutputPerWeek = {}; 
    let yr21OutputPerWeekPlant = {};
    
    let currentHistoryMode = 'ESTIMASI'; 
    let isQuarantineEditModeActive = false; 
    let isPRModeActive = false; 

    document.addEventListener("DOMContentLoaded", async () => {
        await fetchMasterData();
        await fetchTeknisiData();
        await loadHistoryFromServer();
    });

    async function fetchTeknisiData() {
        try {
            const response = await fetch('/Pemakaian/GetTeknisi');
            masterTeknisi = await response.json();
        } catch (error) { console.error(error); }
    }

    async function fetchMasterData() {
        document.getElementById('loadingMaster').classList.remove('d-none');
        try {
            const response = await fetch('/Sparepart/GetApiData');
            masterSparepart = await response.json();
            addNewRow(); 
        } catch (error) { console.error(error); }
        finally { document.getElementById('loadingMaster').classList.add('d-none'); }
    }

    async function loadHistoryFromServer() {
        try {
            const response = await fetch('/Pemakaian/GetHistoryData');
            const resJson = await response.json();
            
            historyDatasetEstimasi = resJson.dataEstimasi;
            historyDatasetFix = resJson.dataFix;
            historyDatasetKarantina = resJson.dataKarantina;
            serverCurrentWeek = resJson.currentWeek;
            yr21OutputPerWeek = resJson.outputPerWeek || {}; 
            yr21OutputPerWeekPlant = resJson.outputPerWeekPlant || {};

            var badge = document.getElementById("badgeKarantinaCount");
            if (badge) badge.innerText = historyDatasetKarantina.length;

            populateWeekFilterDropdown();
            renderHistoryTable();
            renderKarantinaTableModal();
        } catch (error) { console.error(error); }
    }

    // =========================================================
    // KENDALI UI & TOGGLE MODE
    // =========================================================
    function toggleHistoryMode(mode) {
        if(isQuarantineEditModeActive) toggleModeKarantinaUI(); 
        if(isPRModeActive) toggleModePRUI();

        currentHistoryMode = mode;
        const ddlFilter = document.getElementById("historyWeekFilter");
        const btnEditKarantina = document.getElementById("btnModeKarantina");
        const btnEditPR = document.getElementById("btnModePR");
        const btnKarantinaData = document.querySelector("[data-bs-target='#modalKarantina']");
        const btnPrintReport = document.getElementById("btnPrintReport");
        const formContainer = document.getElementById("formPengambilanContainer");
        
        ddlFilter.disabled = false; 
        
        if (mode === 'FIX') {
            if(formContainer) formContainer.classList.add("d-none");
            if(btnEditKarantina) btnEditKarantina.classList.add("d-none"); 
            if(btnEditPR) btnEditPR.classList.add("d-none"); 
            if(btnKarantinaData) btnKarantinaData.classList.add("d-none");
            if(ddlFilter) ddlFilter.classList.remove("d-none");
            if(btnPrintReport) btnPrintReport.innerHTML = `<i class="bi bi-printer me-1"></i> Cetak Tabel`;
        } else {
            if(formContainer) formContainer.classList.remove("d-none");
            if(btnEditKarantina) btnEditKarantina.classList.remove("d-none");
            if(btnEditPR) btnEditPR.classList.remove("d-none");
            if(btnKarantinaData) btnKarantinaData.classList.remove("d-none");
            if(ddlFilter) ddlFilter.classList.add("d-none"); // Sembunyikan week filter untuk mode estimasi
            if(btnPrintReport) btnPrintReport.innerHTML = `<i class="bi bi-printer me-1"></i> Cetak Lap. Lolos`;
        }
        populateWeekFilterDropdown();
        renderHistoryTable();
    }

    function toggleModeKarantinaUI() {
        if(isPRModeActive) toggleModePRUI(); 

        isQuarantineEditModeActive = !isQuarantineEditModeActive;
        const btnToggle = document.getElementById("btnModeKarantina");
        const btnExecute = document.getElementById("btnExecuteKarantina");
        
        if(isQuarantineEditModeActive) {
            btnToggle.innerHTML = `<i class="bi bi-x-circle me-1"></i> Batal Edit`;
            btnToggle.className = "btn btn-sm btn-outline-secondary fw-bold shadow-sm";
            btnExecute.classList.remove("d-none");
        } else {
            btnToggle.innerHTML = `<i class="bi bi-pencil-square me-1"></i> Karantina / Edit`;
            btnToggle.className = "btn btn-sm btn-outline-danger fw-bold shadow-sm";
            btnExecute.classList.add("d-none");
        }
        renderHistoryTable();
    }

    function toggleModePRUI() {
        if(isQuarantineEditModeActive) toggleModeKarantinaUI(); 

        isPRModeActive = !isPRModeActive;
        const btnToggle = document.getElementById("btnModePR");
        const btnExecute = document.getElementById("btnExecutePR");
        
        if(isPRModeActive) {
            btnToggle.innerHTML = `<i class="bi bi-x-circle me-1"></i> Batal PR Mode`;
            btnToggle.className = "btn btn-sm btn-outline-secondary fw-bold shadow-sm";
            btnExecute.classList.remove("d-none");
        } else {
            btnToggle.innerHTML = `<i class="bi bi-cart-plus me-1"></i> Mode Order PR`;
            btnToggle.className = "btn btn-sm btn-outline-info fw-bold shadow-sm";
            btnExecute.classList.add("d-none");
        }
        renderHistoryTable();
    }

    function populateWeekFilterDropdown() {
        const ddl = document.getElementById("historyWeekFilter");
        const currentSelection = ddl.value; 
        ddl.innerHTML = "";

        let uniqueWeeks = {};
        
        // Pilih sumber data berdasarkan mode yang aktif
        if (currentHistoryMode === 'ESTIMASI') {
            uniqueWeeks[serverCurrentWeek] = `Week ${serverCurrentWeek.split('-W')[1]} (Estimasi Berjalan)`;
            historyDatasetEstimasi.forEach(item => { uniqueWeeks[item.WeekYearKey] = item.WeekDisplay; });
        } else {
            historyDatasetFix.forEach(item => { uniqueWeeks[item.WeekYearKey] = item.WeekDisplay; });
        }

        // Urutkan minggu dari yang terbaru (Descending)
        const sortedKeys = Object.keys(uniqueWeeks).sort((a, b) => b.localeCompare(a));
        sortedKeys.forEach(key => { ddl.innerHTML += `<option value="${key}">${uniqueWeeks[key]}</option>`; });

        // Pertahankan seleksi sebelumnya jika masih relevan, jika tidak pilih yang paling atas
        if (currentSelection && sortedKeys.includes(currentSelection)) {
            ddl.value = currentSelection;
        } else {
            ddl.value = currentHistoryMode === 'ESTIMASI' ? serverCurrentWeek : (sortedKeys.length > 0 ? sortedKeys[0] : "");
        }
    }

    // =========================================================
    // RENDER TABEL UTAMA (HISTORY)
    // =========================================================
    function renderHistoryTable() {
        const selectedWeek = document.getElementById("historyWeekFilter").value;
        const selectedPlant = document.getElementById("historyPlantFilter") ? document.getElementById("historyPlantFilter").value : "";
        const theadRow = document.getElementById("historyTheadRow");
        const tbody = document.getElementById("historyTableBody");
        
        if (currentHistoryMode === 'FIX') {
            theadRow.innerHTML = `<th>Tanggal</th><th>No Material</th><th>Deskripsi Barang</th><th>Tujuan / Mesin</th><th>No Order</th><th style="width: 6%;">Qty</th><th>Harga Satuan</th><th>Total Biaya</th>`;
        } else {
            let prefixTh = "";
            if (isQuarantineEditModeActive) prefixTh = `<th style="width:40px;">Pilih</th>`;
            if (isPRModeActive) prefixTh = `<th style="width:40px;">Order PR</th>`;

            theadRow.innerHTML = `${prefixTh}<th>Tanggal</th><th>No Material</th><th>Deskripsi Barang</th><th>Tujuan / Mesin</th><th>Nama Pengambil</th><th>Plant</th><th style="width: 6%;">Qty</th><th style="width: 8%;">Sisa Stok</th><th>Harga Satuan</th><th>Total Biaya</th>`;
        }

        let sourceData = currentHistoryMode === 'FIX' ? historyDatasetFix : historyDatasetEstimasi;
        let filteredData = sourceData.filter(x => x.WeekYearKey === selectedWeek);
        if (selectedPlant !== "") {
            filteredData = filteredData.filter(x => x.Plant === selectedPlant);
        }
        
        // HITUNG NILAI WIDGET SECARA DINAMIS
        let totalCostSelectedWeek = filteredData.reduce((sum, item) => sum + item.TotalHargaNumeric, 0);

        if (currentHistoryMode === 'ESTIMASI') {
            document.getElementById("lblCostThisWeek").innerText = totalCostSelectedWeek.toLocaleString("id-ID");
            
            let outputPcs = 0;
            if (selectedPlant !== "") {
                outputPcs = yr21OutputPerWeekPlant[`${serverCurrentWeek}|${selectedPlant}`] || 0;
            } else {
                outputPcs = yr21OutputPerWeek[serverCurrentWeek] || 0;
            }
            document.getElementById("lblOutputThisWeek").innerText = outputPcs.toLocaleString("id-ID");
            
            let ratio = outputPcs > 0 ? totalCostSelectedWeek / outputPcs : 0;
            document.getElementById("lblRatioThisWeek").innerText = ratio.toLocaleString("id-ID", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
            
            let cardRatio = document.getElementById("cardRatioThisWeek");
            if (ratio > 1.75) {
                cardRatio.className = "card bg-danger text-white shadow-sm border-0";
            } else {
                cardRatio.className = "card bg-success text-white shadow-sm border-0";
            }
        } else if (currentHistoryMode === 'FIX') {
            document.getElementById("lblCostLastWeek").innerText = totalCostSelectedWeek.toLocaleString("id-ID");
            
            let outputPcs = 0;
            if (selectedPlant !== "") {
                outputPcs = yr21OutputPerWeekPlant[`${selectedWeek}|${selectedPlant}`] || 0;
            } else {
                outputPcs = yr21OutputPerWeek[selectedWeek] || 0;
            }
            document.getElementById("lblOutputLastWeek").innerText = outputPcs.toLocaleString("id-ID");
            
            let ratio = outputPcs > 0 ? totalCostSelectedWeek / outputPcs : 0;
            document.getElementById("lblRatioLastWeek").innerText = ratio.toLocaleString("id-ID", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

            let cardRatio = document.getElementById("cardRatioLastWeek");
            if (ratio > 1.75) {
                cardRatio.className = "card bg-danger text-white shadow-sm border-0";
            } else {
                cardRatio.className = "card bg-success text-white shadow-sm border-0";
            }
        }

        if (filteredData.length === 0) {
            let colSpan = currentHistoryMode === 'FIX' ? 8 : (isQuarantineEditModeActive || isPRModeActive ? 11 : 10);
            tbody.innerHTML = `<tr><td colspan="${colSpan}" class="text-center py-4 text-muted">Tidak ada riwayat pada periode ini.</td></tr>`;
            return;
        }

        let htmlGrid = "";
        filteredData.forEach(item => {
            if (currentHistoryMode === 'FIX') {
                htmlGrid += `
                <tr class="table-success opacity-85">
                    <td class="text-center text-secondary">${item.TanggalFormated}</td>
                    <td class="font-monospace fw-bold text-dark">${item.MaterialNo}</td>
                    <td class="text-uppercase text-truncate" style="max-width: 260px;" title="${item.MaterialDesc}">${item.MaterialDesc}</td>
                    <td class="text-muted text-truncate" style="max-width: 180px;" title="${item.TujuanPengambilan}">${item.TujuanPengambilan || '-'}</td>
                    <td>${item.OrderNo}</td>
                    <td class="text-center fw-bold text-primary">${item.JumlahPengambilan}</td>
                    <td class="text-end font-monospace">Rp ${item.HargaSatuanFormated}</td>
                    <td class="text-end font-monospace fw-bold text-danger">Rp ${item.TotalHargaFormated}</td>
                </tr>`;
            } else {
                // PENYESUAIAN C#: item.MaterialNo di Pemakaian dicocokkan dengan m.Material di tabel Sparepart yang baru
                let partData = masterSparepart.find(m => m.Material && m.Material.trim() === item.MaterialNo.trim());
                let sisaStok = partData ? partData.CurrentStock : 0;
                let safetyStok = partData ? partData.SafetyStock : 0;
                
                let badgeClass = "bg-success";
                let isKritis = false;
                if (sisaStok === 0) { badgeClass = "bg-danger"; isKritis = true; }
                else if (sisaStok <= safetyStok) { badgeClass = "bg-warning text-dark"; isKritis = true; }

                let stokHtml = `<span class="badge ${badgeClass} w-100 py-1">${sisaStok} PC</span>`;

                let prefixTd = "";
                if (isQuarantineEditModeActive) {
                    prefixTd = `<td class="text-center"><input type="checkbox" class="form-check-input border-danger chk-quarantine-item" data-id="${item.PengambilanID}"></td>`;
                } else if (isPRModeActive) {
                    if (isKritis) {
                        prefixTd = `<td class="text-center"><input type="checkbox" class="form-check-input border-info chk-pr-item" data-matno="${item.MaterialNo}" data-desc="${item.MaterialDesc}" data-stok="${sisaStok}"></td>`;
                    } else {
                        prefixTd = `<td class="text-center"><i class="bi bi-dash text-muted"></i></td>`;
                    }
                }

                htmlGrid += `
                <tr>
                    ${prefixTd}
                    <td class="text-center text-secondary">${item.TanggalFormated}</td>
                    <td class="font-monospace fw-bold text-dark">${item.MaterialNo}</td>
                    <td class="text-uppercase text-truncate" style="max-width: 260px;" title="${item.MaterialDesc}">${item.MaterialDesc}</td>
                    <td class="text-muted text-truncate" style="max-width: 180px;" title="${item.TujuanPengambilan}">${item.TujuanPengambilan || '-'}</td>
                    <td>${item.NamaPengambil || '-'}</td>
                    <td><span class="badge bg-secondary">${item.Plant || '-'}</span></td>
                    <td class="text-center fw-bold text-primary">${item.JumlahPengambilan}</td>
                    <td class="text-center align-middle">${stokHtml}</td>
                    <td class="text-end font-monospace">Rp ${item.HargaSatuanFormated}</td>
                    <td class="text-end font-monospace fw-bold text-danger">Rp ${item.TotalHargaFormated}</td>
                </tr>`;
            }
        });
        tbody.innerHTML = htmlGrid;
    }

    // =========================================================
    // FUNGSI AKSI: KARANTINA & PR
    // =========================================================
    async function submitKarantinaBulk() {
        const checkBoxes = document.querySelectorAll(".chk-quarantine-item:checked");
        let targetIds = Array.from(checkBoxes).map(cb => parseInt(cb.getAttribute("data-id")));

        if(targetIds.length === 0) { alert("Pilih item sparepart terlebih dahulu."); return; }
        if(!confirm("Karantina item terpilih?")) return;

        try {
            const response = await fetch('/Pemakaian/QuarantineItems', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(targetIds)
            });

            if (response.ok) {
                toggleModeKarantinaUI();
                loadHistoryFromServer(); 
            } else { alert("Gagal melakukan karantina."); }
        } catch (error) { alert("Kesalahan jaringan."); }
    }

    // INTEGRASI PENUH DENGAN CONTROLLER SPAREPART
    async function submitOrderPR() {
        const checkBoxes = document.querySelectorAll(".chk-pr-item:checked");
        if(checkBoxes.length === 0) { alert("Pilih minimal satu sparepart kritis untuk diorder."); return; }
        
        let payloadItems = [];
        checkBoxes.forEach(cb => {
            payloadItems.push({
                MaterialNo: cb.getAttribute("data-matno"),
                Qty: 1, // Default diset 1 karena form di riwayat tidak memiliki field Qty khusus PR
                Remark: "Order dari histori pemakaian"
            });
        });

        const btnExecute = document.getElementById("btnExecutePR");
        const originalText = btnExecute.innerHTML;
        btnExecute.innerHTML = `<span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span> Merakit PR...`;
        btnExecute.disabled = true;

        try {
            // Tembak langsung ke fungsi ExportPR yang ada di modul EWS Anda
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
                
                const dateStr = new Date().toISOString().slice(0,10).replace(/-/g,"");
                a.download = `PR_Technical_P1_${dateStr}.xlsx`;

                document.body.appendChild(a);
                a.click();
                a.remove();
                window.URL.revokeObjectURL(url);

                toggleModePRUI(); 
            } else {
                const errText = await response.text();
                alert("Gagal memproses PR: " + errText);
            }
        } catch (error) {
            alert("Kesalahan jaringan saat menghubungi EWS.");
        } finally {
            btnExecute.innerHTML = originalText;
            btnExecute.disabled = false;
        }
    }

    // =========================================================
    // RENDER MODAL KARANTINA & AKSI
    // =========================================================
    function renderKarantinaTableModal() {
        const tbody = document.getElementById("karantinaTableBody");
        if(historyDatasetKarantina.length === 0) {
            tbody.innerHTML = `<tr><td colspan="8" class="text-center py-4 text-muted"><i class="bi bi-shield-check text-success me-1 fs-5"></i> Gudang karantina bersih. Tidak ada item yang ditahan.</td></tr>`;
            return;
        }

        let htmlGrid = "";
        historyDatasetKarantina.forEach(item => {
            htmlGrid += `
            <tr>
                <td class="text-center text-secondary">${item.TanggalFormated}</td>
                <td class="font-monospace fw-bold">${item.MaterialNo}</td>
                <td class="text-truncate text-uppercase" style="max-width: 220px;">${item.MaterialDesc}</td>
                <td class="text-muted text-truncate" style="max-width: 150px;">${item.TujuanPengambilan || '-'}</td>
                <td>${item.NamaPengambil || '-'}</td>
                <td class="text-center fw-bold text-dark">${item.JumlahPengambilan}</td>
                <td class="text-end font-monospace text-danger fw-bold">Rp ${item.TotalHargaFormated}</td>
                <td class="text-center">
                    <div class="btn-group btn-group-sm w-100">
                        <button class="btn btn-outline-primary fw-bold py-1" onclick="executeRestore(${item.PengambilanID})" title="Batal Tahan">
                            <i class="bi bi-arrow-counterclockwise"></i> Batal
                        </button>
                        <button class="btn btn-outline-danger fw-bold py-1" onclick="executeRetur(${item.PengambilanID})">
                            <i class="bi bi-trash-fill"></i> Retur
                        </button>
                        <button class="btn btn-success fw-bold py-1" onclick="executeRollForward(${item.PengambilanID})">
                            <i class="bi bi-arrow-right-short"></i> Ke Week Depan
                        </button>
                    </div>
                </td>
            </tr>`;
        });
        tbody.innerHTML = htmlGrid;
    }

    async function executeRestore(id) {
        if(!confirm("Kembalikan item sparepart ini ke rekap estimasi?")) return;
        try {
            const res = await fetch(`/Pemakaian/RestoreItem?id=${id}`, { method: 'POST' });
            if (res.ok) loadHistoryFromServer();
        } catch (error) { alert("Error jaringan."); }
    }

    async function executeRetur(id) {
        if(!confirm("Retur ke gudang sparepart? Data dihapus permanen.")) return;
        try {
            const res = await fetch(`/Pemakaian/ReturItem?id=${id}`, { method: 'POST' });
            if (res.ok) loadHistoryFromServer();
        } catch (error) { alert("Error jaringan."); }
    }

    async function executeRollForward(id) {
        if(!confirm("Geser beban biaya item ini ke minggu depan?")) return;
        try {
            const res = await fetch(`/Pemakaian/ShiftToNextWeek?id=${id}`, { method: 'POST' });
            if (res.ok) loadHistoryFromServer();
        } catch (error) { alert("Error jaringan."); }
    }

    function printLaporanLolos() {
        window.print();
    }

    // =========================================================
    // LOGIKA FORM INLINE (INPUT EXCEL-STYLE)
    // =========================================================
    function addNewRow() {
        const tbody = document.getElementById('inputBody');
        const tr = document.createElement('tr');
        const today = new Date().toISOString().split('T')[0];
        
        tr.innerHTML = `
            <td><input type="date" class="form-control form-control-sm border-0 bg-transparent inp-tgl" value="${today}"></td>
            <td><input type="text" class="form-control form-control-sm border-0 bg-transparent inp-matno font-monospace fw-bold" placeholder="Ketik/Scan..." onblur="lookupMaterial(this)"></td>
            
            <td><input type="text" class="form-control form-control-sm border-0 bg-transparent text-primary fw-bold inp-desc" placeholder="Ketik deskripsi part..." autocomplete="off" oninput="handleDescSearch(this)" onfocus="handleDescSearch(this)"></td>
            
            <td><input type="text" class="form-control form-control-sm border-0 bg-transparent inp-tujuan" placeholder="Lokasi mesin/line..."></td>
            <td><input type="text" class="form-control form-control-sm border-0 bg-transparent inp-nama text-success fw-bold" placeholder="Cari teknisi..." autocomplete="off" oninput="handleTeknisiSearch(this)" onfocus="handleTeknisiSearch(this)" onblur="lookupTeknisi(this)"></td>
            <td><input type="text" class="form-control form-control-sm border-0 bg-light inp-plant text-center text-muted" placeholder="Plant" readonly tabindex="-1"></td>
            <td><input type="number" class="form-control form-control-sm border-0 bg-transparent inp-qty text-center fw-bold" placeholder="0" onkeydown="handleExcelTab(event, this)"></td>
            <td class="p-0"><input type="text" class="form-control form-control-sm border-0 text-center fw-bold inp-stok h-100 bg-light" readonly tabindex="-1" style="border-radius: 0;"></td>
            <td class="text-center"><button class="btn btn-sm text-danger border-0 p-0" onclick="removeRow(this)" tabindex="-1"><i class="bi bi-trash"></i></button><input type="hidden" class="inp-harga" value="0"></td>`;
        tbody.appendChild(tr);
    }
    
    function removeRow(btn) { 
        const tbody = document.getElementById('inputBody'); 
        if (tbody.children.length > 1) { btn.closest('tr').remove(); } 
        else { const row = btn.closest('tr'); row.querySelector('.inp-matno').value = ""; lookupMaterial(row.querySelector('.inp-matno')); } 
    }
    
    function lookupMaterial(inputElement) { 
        const row = inputElement.closest('tr'); 
        const matNo = inputElement.value.trim(); 
        const inpDesc = row.querySelector('.inp-desc'); 
        const inpStok = row.querySelector('.inp-stok'); 
        const inpHarga = row.querySelector('.inp-harga'); 
        
        if (matNo === "") { 
            inpDesc.value = ""; inpStok.value = ""; inpHarga.value = "0"; 
            inpStok.className = "form-control form-control-sm border-0 text-center bg-light fw-bold inp-stok h-100"; 
            return; 
        } 
        
        // MENGGUNAKAN KOLOM BARU "Material" & "MaterialDescription" dari masterSparepart
        const part = masterSparepart.find(x => x.Material && x.Material.trim() === matNo); 
        
        if (part) { 
            inpDesc.value = part.MaterialDescription; 
            inpHarga.value = part.MovingUnitPrice || 0; 
            const actualQty = part.CurrentStock || 0; 
            inpStok.value = `${actualQty} PC`; 
            
            if (actualQty === 0) inpStok.className = "form-control form-control-sm border-0 text-center table-danger fw-bold text-danger inp-stok h-100"; 
            else if (actualQty <= (part.SafetyStock || 0)) inpStok.className = "form-control form-control-sm border-0 text-center table-warning fw-bold text-warning-emphasis inp-stok h-100"; 
            else inpStok.className = "form-control form-control-sm border-0 text-center table-success fw-bold text-success inp-stok h-100"; 
        } else { 
            inpDesc.value = "TIDAK DIKENAL (SAP)"; inpStok.value = "N/A"; inpHarga.value = "0"; 
            inpStok.className = "form-control form-control-sm border-0 text-center bg-secondary text-white fw-bold inp-stok h-100"; 
        } 
    }

    // =========================================================
    // LOGIKA AUTOCOMPLETE DESKRIPSI SPAREPART
    // =========================================================
    let activeAutoCompleteRow = null;

    function handleDescSearch(inputElement) {
        const row = inputElement.closest('tr');
        const query = inputElement.value.toLowerCase().trim();
        const dropdown = document.getElementById('autoCompleteDropdown');

        // Jangan lakukan pencarian jika ketikan kurang dari 2 huruf
        if (query.length < 2) {
            dropdown.classList.add('d-none');
            return;
        }

        // Pecah kata agar pencarian tidak peduli urutan (misal: "bearing 6204" atau "6204 bearing")
        let tokens = query.split(" ").filter(t => t !== "");

        // Saring data dari database masterSparepart
        let filtered = masterSparepart.filter(item => {
            let textToSearch = `${item.Material} ${item.MaterialDescription}`.toLowerCase();
            return tokens.every(token => textToSearch.includes(token));
        }).slice(0, 15); // Batasi maksimal 15 hasil agar tidak membebani browser

        // Rakit UI Dropdown
        if (filtered.length === 0) {
            dropdown.innerHTML = `<div class="list-group-item text-muted small py-2"><i class="bi bi-info-circle me-1"></i> Part tidak ditemukan di SAP...</div>`;
        } else {
            let html = "";
            filtered.forEach(part => {
                let sisaStok = part.CurrentStock || 0;
                let badgeClass = sisaStok > 0 ? 'bg-success' : 'bg-danger';
                let stokTeks = sisaStok > 0 ? `${sisaStok} ${part.UoM || 'PC'}` : 'KOSONG';
                
                html += `
                <button type="button" class="list-group-item list-group-item-action p-2 border-bottom" onmousedown="selectMaterialFromDropdown(this, '${part.Material}')">
                    <div class="d-flex justify-content-between align-items-center mb-1">
                        <span class="fw-bold font-monospace text-primary small">${part.Material}</span>
                        <span class="badge ${badgeClass} shadow-sm" style="font-size: 0.7rem;">${stokTeks}</span>
                    </div>
                    <div class="small text-truncate text-uppercase text-dark" style="max-width: 380px;">${part.MaterialDescription}</div>
                </button>`;
            });
            dropdown.innerHTML = html;
        }

        // Pasang Koordinat Jendela Melayang (Berada pas di bawah input box yang sedang diketik)
        const rect = inputElement.getBoundingClientRect();
        dropdown.style.top = (rect.bottom + window.scrollY) + 'px';
        dropdown.style.left = (rect.left + window.scrollX) + 'px';
        dropdown.style.width = rect.width >= 400 ? rect.width + 'px' : '400px'; // Minimal lebar 400px agar tulisan tidak terpotong
        
        dropdown.classList.remove('d-none');
        activeAutoCompleteRow = row;
    }

    function selectMaterialFromDropdown(btn, matNo) {
        if (!activeAutoCompleteRow) return;
        
        // 1. Tembak Nomor Material ke Kolom 'No Material'
        const inpMatNo = activeAutoCompleteRow.querySelector('.inp-matno');
        inpMatNo.value = matNo;
        
        // 2. Tutup Jendela Melayang
        document.getElementById('autoCompleteDropdown').classList.add('d-none');
        
        // 3. Picu Trigger `lookupMaterial` seolah-olah user men-scan barcode
        lookupMaterial(inpMatNo);
        
        // 4. Fokuskan kursor langsung ke kolom QTY (Siap ketik jumlah)
        activeAutoCompleteRow.querySelector('.inp-qty').focus();
    }

    // =========================================================
    // LOGIKA AUTOCOMPLETE TEKNISI
    // =========================================================
    let activeTeknisiRow = null;

    function handleTeknisiSearch(inputElement) {
        const row = inputElement.closest('tr');
        const query = inputElement.value.toLowerCase().trim();
        const dropdown = document.getElementById('autoCompleteTeknisiDropdown');

        if (query.length < 2) {
            dropdown.classList.add('d-none');
            return;
        }

        let filtered = masterTeknisi.filter(item => {
            let nama = item.Nama || item.nama || "";
            return nama.toLowerCase().includes(query);
        }).slice(0, 10);

        if (filtered.length === 0) {
            dropdown.innerHTML = `<div class="list-group-item text-muted small py-2"><i class="bi bi-info-circle me-1"></i> Teknisi tidak ditemukan...</div>`;
        } else {
            let html = "";
            filtered.forEach(teknisi => {
                let nama = teknisi.Nama || teknisi.nama || "";
                let plant = teknisi.Plant || teknisi.plant || "";
                html += `
                <button type="button" class="list-group-item list-group-item-action p-2 border-bottom" onmousedown="selectTeknisiFromDropdown('${nama}', '${plant}')">
                    <div class="d-flex justify-content-between align-items-center">
                        <span class="fw-bold text-dark small">${nama}</span>
                        <span class="badge bg-secondary shadow-sm" style="font-size: 0.7rem;">${plant}</span>
                    </div>
                </button>`;
            });
            dropdown.innerHTML = html;
        }

        const rect = inputElement.getBoundingClientRect();
        dropdown.style.top = (rect.bottom + window.scrollY) + 'px';
        dropdown.style.left = (rect.left + window.scrollX) + 'px';
        dropdown.style.width = rect.width >= 250 ? rect.width + 'px' : '250px';
        
        dropdown.classList.remove('d-none');
        activeTeknisiRow = row;
    }

    function selectTeknisiFromDropdown(nama, plant) {
        if (!activeTeknisiRow) return;
        
        const inpNama = activeTeknisiRow.querySelector('.inp-nama');
        const inpPlant = activeTeknisiRow.querySelector('.inp-plant');
        
        inpNama.value = nama;
        inpPlant.value = plant;
        
        document.getElementById('autoCompleteTeknisiDropdown').classList.add('d-none');
        activeTeknisiRow.querySelector('.inp-qty').focus();
    }

    function lookupTeknisi(inputElement) {
        // Beri sedikit delay agar klik pada dropdown bisa terproses lebih dulu sebelum blur
        setTimeout(() => {
            const row = inputElement.closest('tr');
            const nama = inputElement.value.trim().toLowerCase();
            const inpPlant = row.querySelector('.inp-plant');
            
            // Cek apakah input kosong
            if (nama === "") {
                inpPlant.value = "";
                return;
            }

            // Coba cari teknisi yang persis sama namanya (case-insensitive)
            const matched = masterTeknisi.find(t => {
                let tNama = t.Nama || t.nama || "";
                return tNama.toLowerCase() === nama;
            });

            if (matched) {
                // Rapikan penulisan nama dan set plant
                let matchedNama = matched.Nama || matched.nama || "";
                let matchedPlant = matched.Plant || matched.plant || "";
                inputElement.value = matchedNama;
                inpPlant.value = matchedPlant;
            } else {
                // Jika tidak ditemukan kecocokan persis, kosongkan plant 
                // Opsional: tampilkan alert atau biarkan kosong jika plant wajib dari DB
                inpPlant.value = "";
            }
        }, 200);
    }

    // Fungsi Pengaman: Tutup dropdown jika user klik di sembarang tempat di luar layar
    document.addEventListener('mousedown', function(e) {
        const dropdown = document.getElementById('autoCompleteDropdown');
        if (dropdown && !dropdown.contains(e.target) && !e.target.classList.contains('inp-desc')) {
            dropdown.classList.add('d-none');
        }
        
        const dropdownTeknisi = document.getElementById('autoCompleteTeknisiDropdown');
        if (dropdownTeknisi && !dropdownTeknisi.contains(e.target) && !e.target.classList.contains('inp-nama')) {
            dropdownTeknisi.classList.add('d-none');
        }
    });
    
    function handleExcelTab(e, inputElement) { 
        if (e.key === 'Tab' || e.key === 'Enter') { 
            e.preventDefault(); 
            const tbody = document.getElementById('inputBody'); 
            const currentRow = inputElement.closest('tr'); 
            if (currentRow === tbody.lastElementChild) addNewRow(); 
            const nextRow = currentRow.nextElementSibling; 
            if (nextRow) nextRow.querySelector('.inp-matno').focus(); 
        } 
    }
    
    async function saveData() { 
        const rows = document.querySelectorAll('#inputBody tr'); 
        let payload = []; let hasError = false; 
        
        rows.forEach((row, index) => { 
            const matNo = row.querySelector('.inp-matno').value.trim(); 
            const qtyStr = row.querySelector('.inp-qty').value; 
            const desc = row.querySelector('.inp-desc').value; 
            
            if (matNo === "" && qtyStr === "") return; 
            if (desc.startsWith("TIDAK DIKENAL")) { alert(`Baris ke-${index + 1}: Material No tidak valid.`); hasError = true; return; } 
            
            const qty = parseFloat(qtyStr); 
            if (isNaN(qty) || qty <= 0) { alert(`Baris ke-${index + 1}: Kuantitas tidak valid.`); hasError = true; return; } 
            
            payload.push({ 
                TanggalPengambilan: row.querySelector('.inp-tgl').value, 
                MaterialNo: matNo, 
                JumlahPengambilan: qty, 
                TujuanPengambilan: row.querySelector('.inp-tujuan').value.trim(), 
                NamaPengambil: row.querySelector('.inp-nama').value.trim(), 
                Plant: row.querySelector('.inp-plant').value.trim(),
                HargaSatuanSaatIni: parseFloat(row.querySelector('.inp-harga').value) 
            }); 
        }); 
        
        if (hasError) return; 
        if (payload.length === 0) { alert("Isi data terlebih dahulu."); return; } 
        
        try { 
            const response = await fetch('/Pemakaian/SaveData', { 
                method: 'POST', 
                headers: { 'Content-Type': 'application/json' }, 
                body: JSON.stringify(payload) 
            }); 
            
            if (response.ok) { 
                alert("Data pemakaian berhasil disimpan!"); 
                document.getElementById('inputBody').innerHTML = ""; 
                addNewRow(); 
                loadHistoryFromServer(); 
            } else { alert("Gagal menyimpan."); } 
        } catch (error) { alert("Kesalahan jaringan."); } 
    }
