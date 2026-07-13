
    Chart.register(ChartDataLabels);

    // KOSONGKAN DATA AWAL
    let ypDataRaw = [];
    let yrDataRaw = [];

    let baseDataYP = [];
    let baseDataYR = [];
    let drillHistory = []; // Stack untuk navigasi 5 level

    let barChartInst = null;
    let pieChartInst = null;
    let isChartZoomed = false;
    
    let subDrillMode = "Line"; 
    let currentTableMode = "Raw";
    let currentDrillDownData = [];
    let sortState = { col: -1, asc: true };

    const monthNames = ["Jan", "Feb", "Mar", "Apr", "Mei", "Jun", "Jul", "Ags", "Sep", "Okt", "Nov", "Des"];
    const dayNames = ["Minggu", "Senin", "Selasa", "Rabu", "Kamis", "Jumat", "Sabtu"];

    // --- PEMANTAU TEMA DARK MODE ---
    const themeObserver = new MutationObserver(function(mutations) {
        mutations.forEach(function(mutation) {
            if (mutation.attributeName === "data-bs-theme") {
                if (barChartInst) barChartInst.update();
                if (pieChartInst) pieChartInst.update();
            }
        });
    });
    themeObserver.observe(document.documentElement, { attributes: true });

    const getChartLabelColor = () => document.documentElement.getAttribute('data-bs-theme') === 'dark' ? '#f8f9fa' : '#333';
    const getGridColor = () => document.documentElement.getAttribute('data-bs-theme') === 'dark' ? 'rgba(255,255,255,0.1)' : 'rgba(0,0,0,0.1)';

    document.addEventListener("DOMContentLoaded", () => {
        document.getElementById("rowBadActors").innerHTML = `
            <div class="col-12 text-center py-3">
                <div class="spinner-border text-primary me-2 spinner-border-sm"></div> 
                <span class="text-muted fw-bold">Memuat Analitik Performa dari Server...</span>
            </div>`;
            
        fetchDashboardData();
    });

    async function fetchDashboardData() {
        try {
            const response = await fetch('/Home/GetDashboardData');
            const data = await response.json();
            ypDataRaw = data.ypData || [];
            yrDataRaw = data.yrData || [];

            populateDynamicDropdowns();
            handleModeChange(); 
        } catch (error) {
            console.error("Gagal menarik data dasbor:", error);
            document.getElementById("rowBadActors").innerHTML = `
                <div class="col-12 text-center py-3 text-danger fw-bold">
                    <i class="bi bi-x-circle me-2"></i> Gagal terhubung ke Database.
                </div>`;
        }
    }

    function getLineName(funcLoc) {
        if (!funcLoc) return "-";
        let parts = funcLoc.split('-');
        if (parts.length >= 4) return "Line " + Number(parts[3]); 
        return "-";
    }

    function getMachineName(funcLoc, activityText) {
    if (!funcLoc) return "-";
    let kodeMesin = funcLoc.slice(-3);
    let txt = activityText ? activityText.toLowerCase() : "";
    
    // Ekstrak nomor line secara dinamis dari Functional Location (Urutan ke-4)
    let parts = funcLoc.split('-');
    let lineNum = (parts.length >= 4) ? Number(parts[3]) : 0;

    if (kodeMesin === "001") return "Alkali";
    if (kodeMesin === "002") return "Mixer";
    if (kodeMesin === "003") return "Feeder";
    if (kodeMesin === "004") return "Press";
    if (kodeMesin === "005") {
        if (txt.includes("tekanan") || txt.includes("drop") || txt.includes("bar") || txt.includes("dibawah") || txt.includes("di bawah") || txt.includes("turun") || txt.includes("kurang")) return "Boiler";
        return "Steambox";
    }
    if (kodeMesin === "006") return "Cutter & Distributor";
    if (kodeMesin === "007") return "Fryer";
    if (kodeMesin === "008") return "Cooler";
    
    // Area Packing & End of Line
    if (kodeMesin === "009") {
        // 1. Validasi Syarat X-Ray
        if (txt.includes("xray") || txt.includes("x-ray") || txt.includes("x ray")) {
            return "X-Ray";
        }
        
        // 2. Validasi Syarat Bandeed (Wajib di Line 8)
        if (lineNum === 8 && (txt.includes("bandid") || txt.includes("bandeed") || txt.includes("banded") || txt.includes("5in1") || txt.includes("4in1"))) {
            return "Bandeed";
        }
        
        // 3. Validasi Syarat Cuploader (Wajib di Line 5 atau Line 6)
        if ((lineNum === 5 || lineNum === 6) && 
            (txt.includes("cuploader") || txt.includes("cup loader") || txt.includes("suplai cup") || txt.includes("supplycup") || 
             txt.includes("heater cup seal") || txt.includes("heater seal") || txt.includes("preheater") || txt.includes("pre heater") || 
             txt.includes("pallet") || txt.includes("palet"))) {
            return "Cuploader";
        }
        
        // 4. Validasi Syarat Autoloader (Kondisi Eksisting)
        if (txt.includes("autoloader") || txt.includes("auto loader") || txt.includes("bumbu") || txt.includes("minyak")) {
            return "Autoloader";
        }
        
        // Jika tidak memenuhi semua kondisi spesifik di atas, default kembali ke Wrapper
        return "Wrapper";
    }
    
    return funcLoc;
}

    function populateDynamicDropdowns() {
        const ddlBulan = document.getElementById("fltBulan");
        const ddlMinggu = document.getElementById("fltMinggu");
        let bulanSet = new Set(), mingguSet = new Set();
        ypDataRaw.forEach(item => {
            let d = new Date(item.NotificationDate);
            if(d.getFullYear() > 1900) {
                bulanSet.add(JSON.stringify({ val: `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`, text: d.toLocaleDateString('id-ID', { month: 'long', year: 'numeric' }) }));
            }
            if (item.WeekKalendarIndofood) mingguSet.add(item.WeekKalendarIndofood);
        });
        ddlBulan.innerHTML = `<option value="ALL">Semua Bulan</option>`; 
        Array.from(bulanSet).map(x => JSON.parse(x)).sort((a, b) => b.val.localeCompare(a.val)).forEach(item => ddlBulan.innerHTML += `<option value="${item.val}">${item.text}</option>`);

        ddlMinggu.innerHTML = "";
        Array.from(mingguSet).sort((a, b) => b.localeCompare(a)).forEach(w => ddlMinggu.innerHTML += `<option value="${w}">Week ${w}</option>`);
    }

    function handleModeChange() {
        const mode = document.getElementById("dimMode").value;
        document.getElementById("divFltBulan").classList.toggle("d-none", mode !== "Month");
        document.getElementById("divFltMinggu").classList.toggle("d-none", mode !== "Week");
        updateDashboard(); 
    }

    function changeSubDrillGroup(mode) {
        subDrillMode = mode;
        if (drillHistory.length > 0) {
            let state = drillHistory[drillHistory.length - 1];
            if (state.type === 'Pareto') {
                renderCurrentState(); 
            }
        }
    }

    function changeTableMode(mode) {
        currentTableMode = mode;
        renderTableRows();
    }

    // --- DEEP DRILL DOWN LOGIC ---

    function getGroupingKey(d, mode, sapWeekStr) {
        if (mode === "Year") return monthNames[d.getMonth()]; 
        if (mode === "Month") {
            if (sapWeekStr && sapWeekStr.length >= 6) {
                let weekNum = parseInt(sapWeekStr.slice(-2), 10);
                return "Week " + weekNum;
            }
            return "Minggu Ke-" + Math.ceil(d.getDate() / 7); 
        }
        if (mode === "Week") {
            return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
        }
        return "Lainnya";
    }

    function formatLabel(key, mode) {
        if (mode === "Week") {
            let d = new Date(key);
            if (isNaN(d)) return key;
            return `${dayNames[d.getDay()]}, ${d.toLocaleDateString('id-ID', {day:'2-digit', month:'short'})}`;
        }
        return key;
    }

    function updateDashboard(isBackwards = false) {
        if (!isBackwards) {
            drillHistory = [];
        }

        const mode = document.getElementById("dimMode").value;
        const fltNotif = document.getElementById("fltNotif").value;
        const fltShift = document.getElementById("fltShift").value;
        const selMonth = document.getElementById("fltBulan").value;
        const selWeek = document.getElementById("fltMinggu").value;
        const isSteamIncluded = document.getElementById("chkSteam").checked;

        const isTimeMatch = (dateObj, weekStr) => {
            if (mode === "Month") {
                if (selMonth === "ALL") return true; 
                return `${dateObj.getFullYear()}-${String(dateObj.getMonth() + 1).padStart(2, '0')}` === selMonth;
            }
            if (mode === "Week") return weekStr === selWeek;
            return true;
        };

        baseDataYP = ypDataRaw.filter(item => {
            let mNotif = !fltNotif || (item.NotificationType && item.NotificationType.includes(fltNotif));
            let mShift = !fltShift || (item.WageGroup_GroupShift && item.WageGroup_GroupShift.includes(fltShift));
            let mTime = isTimeMatch(new Date(item.NotificationDate), item.WeekKalendarIndofood);
            let mBoiler = !(getMachineName(item.FunctionLocation, item.ActivityText) === "Boiler" && !isSteamIncluded);
            return mNotif && mShift && mTime && mBoiler;
        });

        baseDataYR = yrDataRaw.filter(item => {
            if (fltShift && (!item.WageGroup || !item.WageGroup.includes(fltShift))) return false;
            if (!isTimeMatch(new Date(item.PostingDate), item.WeekOfBasicFinishedDate)) return false;
            return true;
        });

        // ==========================================
        // KALKULASI MTTR & MTBF DARI DATA TERSARING
        // ==========================================
        updateMTTRAndMTBF(baseDataYP, baseDataYR);
        // ==========================================

        calculateTopBadActors(baseDataYP);
        updatePieChart(baseDataYP);

        if (drillHistory.length === 0) {
            document.getElementById('drillControls').classList.add('d-none');
            document.getElementById('paretoToggleContainer').classList.remove('d-none');
            document.getElementById('mainChartTitle').innerText = "📊 Analitik Downtime vs Jam Terencana";
            document.getElementById('lblDetail').innerText = "Pilih Batang Grafik";
            document.getElementById('table-body').innerHTML = `<tr><td colspan="6" class="text-center py-4 text-muted">Klik salah satu titik grafik di atas untuk melihat detail.</td></tr>`;
            renderLevel0();
        } else {
            renderCurrentState();
        }
    }
    

    function toggleChartZoom() {
        if (!barChartInst) return;
        
        isChartZoomed = !isChartZoomed;
        const btn = document.getElementById('btnZoomChart');
        const wrapper = document.getElementById('chartCanvasWrapper');
        
        let dataCount = barChartInst.data.labels.length;

        if (isChartZoomed) {
            // MODE ZOOM: Perlebar layar dan munculkan label
            btn.innerHTML = `<i class="bi bi-arrows-angle-contract me-1"></i> Fit ke Layar`;
            btn.classList.replace('btn-outline-info', 'btn-info');
            btn.classList.add('text-white');
            
            // Rumus Dinamis: Setiap titik data diberi ruang 60px (Minimal selebar layar)
            let calculatedWidth = Math.max(100, (dataCount * 60) / wrapper.parentElement.clientWidth * 100);
            wrapper.style.minWidth = calculatedWidth + '%';
        } else {
            // MODE FIT: Kembalikan ukuran pas layar dan sembunyikan label
            btn.innerHTML = `<i class="bi bi-arrows-angle-expand me-1"></i> Zoom & Scroll`;
            btn.classList.replace('btn-info', 'btn-outline-info');
            btn.classList.remove('text-white');
            
            wrapper.style.minWidth = '100%';
        }
        
        // Render ulang chart agar aturan Datalabels yang baru langsung aktif
        barChartInst.update();
    }

    function goBackToMainChart() {
        drillHistory.pop();
        updateDashboard(true);
    }

    function drillToTrend() {
        if (drillHistory.length === 0) return;
        let state = drillHistory[drillHistory.length - 1];
        if (state.type === 'Pareto') {
            drillHistory.push({
                type: 'Trend',
                mode: state.mode,
                label: state.label,
                dataYP: state.dataYP,
                dataYR: state.dataYR
            });
            renderCurrentState();
        }
    }

    function renderCurrentState() {
        document.getElementById('drillControls').classList.remove('d-none');
        document.getElementById('paretoToggleContainer').classList.add('d-none');

        let state = drillHistory[drillHistory.length - 1];
        
        if (state.type === 'Pareto') {
            let titleLink = state.mode !== 'Day' 
                ? `<span class="text-primary drill-link" onclick="drillToTrend()" title="Klik untuk melihat tren waktu khusus ${state.label}">${state.label} 📈</span>` 
                : `<span class="text-primary">${state.label}</span>`;
            document.getElementById('mainChartTitle').innerHTML = `📊 Rincian Periode: ${titleLink}`;
            
            renderParetoSubChart(state.dataYP, state.label);
            loadDrillDownTable(`Periode: ${state.label}`, state.dataYP);
        } 
        else if (state.type === 'Trend') {
            let titleText = state.mode === 'Month' ? 'Mingguan' : 'Harian';
            document.getElementById('mainChartTitle').innerHTML = `📊 Tren ${titleText}: <span class="text-primary">${state.label}</span>`;
            
            renderTrendChart(state.dataYP, state.dataYR, state.mode, state.label);
            loadDrillDownTable(`Tren: ${state.label}`, state.dataYP);
        }

        // Sinkronisasi Pie Chart dan Bad Actor dengan data drill-down saat ini
        updatePieChart(state.dataYP);
        calculateTopBadActors(state.dataYP);
        updateMTTRAndMTBF(state.dataYP, state.dataYR);
    }

    function renderLevel0() {
        const mode = document.getElementById("dimMode").value;
        const isParetoMode = document.getElementById("chkPareto").checked;

        let ypAgg = {};
        baseDataYP.forEach(item => {
            let key = getGroupingKey(new Date(item.NotificationDate), mode, item.WeekKalendarIndofood);
            if (!ypAgg[key]) ypAgg[key] = { mins: 0, items: [] };
            ypAgg[key].mins += item.TotalDownTimeInMinutes;
            ypAgg[key].items.push(item);
        });
        
        let yrAgg = {};
        baseDataYR.forEach(item => {
            let key = getGroupingKey(new Date(item.PostingDate), mode, item.WeekOfBasicFinishedDate);
            if (!yrAgg[key]) yrAgg[key] = 0;
            yrAgg[key] += (item.PlannedHour * 60); 
        });

        let rawKeys = Object.keys(ypAgg);
        if (isParetoMode) {
            rawKeys.sort((a, b) => ypAgg[b].mins - ypAgg[a].mins);
        } else {
            rawKeys.sort((a, b) => {
                if (mode === "Year") return monthNames.indexOf(a) - monthNames.indexOf(b);
                if (mode === "Month") {
                    let numA = parseInt(a.replace(/\D/g, '')) || 0;
                    let numB = parseInt(b.replace(/\D/g, '')) || 0;
                    return numA - numB;
                }
                return a.localeCompare(b);
            });
        }

        let labels = [], percentData = [], minuteData = [], targetLine = [], actualKeys = [], paretoLineData = [];
        let totalMinsAll = rawKeys.reduce((sum, k) => sum + ypAgg[k].mins, 0);
        let cumulativeMins = 0;

        rawKeys.forEach(k => {
            actualKeys.push(k);
            labels.push(formatLabel(k, mode));
            
            let dtMins = ypAgg[k].mins;            
            let plannedMins = yrAgg[k] || 0;       
            minuteData.push(dtMins);
            percentData.push(plannedMins > 0 ? ((dtMins / plannedMins) * 100).toFixed(2) : 0);
            targetLine.push(window.AppConfig.TargetDowntime);

            cumulativeMins += dtMins;
            paretoLineData.push(totalMinsAll > 0 ? ((cumulativeMins / totalMinsAll) * 100).toFixed(1) : 0);
        });

        const onClickHandler = (e, elements) => {
            if (elements.length > 0) {
                const idx = elements[0].index;
                const clickedKey = actualKeys[idx];
                const clickedLabel = labels[idx];
                
                let nextMode = mode === 'Year' ? 'Month' : (mode === 'Month' ? 'Week' : 'Day');
                
                let subYP = baseDataYP.filter(item => getGroupingKey(new Date(item.NotificationDate), mode, item.WeekKalendarIndofood) === clickedKey);
                let subYR = baseDataYR.filter(item => getGroupingKey(new Date(item.PostingDate), mode, item.WeekOfBasicFinishedDate) === clickedKey);
                
                drillHistory.push({ type: 'Pareto', mode: nextMode, label: clickedLabel, dataYP: subYP, dataYR: subYR });
                renderCurrentState();
            }
        };

        if (isParetoMode) {
            renderBaseParetoChart(labels, minuteData, paretoLineData, onClickHandler);
        } else {
            renderBaseTopChart(labels, percentData, minuteData, targetLine, onClickHandler);
        }
    }

    function renderBaseTopChart(labels, pctData, minData, targetLine, clickHandler) {
        const ctx = document.getElementById('barChart').getContext('2d');
        if (barChartInst) barChartInst.destroy();

        barChartInst = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [
                    { type: 'line', label: 'Standar (' + window.AppConfig.TargetDowntime + '%)', data: targetLine, borderColor: 'rgba(220, 53, 69, 1)', borderWidth: 2, borderDash: [5, 5], pointRadius: 0, fill: false, datalabels: { display: false } },
                    { 
                        type: 'line', label: 'Downtime (%)', data: pctData, backgroundColor: 'rgba(54, 162, 235, 0.2)', borderColor: 'rgba(54, 162, 235, 1)', borderWidth: 2, 
                        pointBackgroundColor: 'rgba(54, 162, 235, 1)', pointBorderColor: '#fff', pointBorderWidth: 1, pointRadius: 4, pointHoverRadius: 6,
                        fill: true, tension: 0.1, customMinutes: minData 
                    }
                ]
            },
            options: {
                devicePixelRatio: 3,
                responsive: true, maintainAspectRatio: false,
                plugins: {
                    datalabels: { 
                        anchor: 'end', align: 'top', offset: 4, color: getChartLabelColor, textAlign: 'center',
                        font: function(context) {
                            let count = context.chart.data.labels.length;
                            return { weight: 'bold', size: count > 15 ? 8 : 10 };
                        },
                        display: function(context) {
                            return !context.dataset.label.includes('Standar');
                        },
                        formatter: function(v, ctx) { return v == 0 ? null : `${v}%`; } 
                    },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                let label = context.dataset.label || '';
                                if (label) label += ': ';
                                if (context.parsed.y !== null) {
                                    label += context.parsed.y + '%';
                                    if (context.dataset.customMinutes && context.dataset.customMinutes[context.dataIndex] !== undefined) {
                                        label += ` (${context.dataset.customMinutes[context.dataIndex]} menit)`;
                                    }
                                }
                                return label;
                            }
                        }
                    }
                },
                scales: { y: { beginAtZero: true, suggestedMax: 3, title: { display: true, text: 'Persentase Downtime (%)' } } },
                onClick: clickHandler
            }
        });
    }

    function renderBaseParetoChart(labels, minData, cumPctData, clickHandler) {
        const ctx = document.getElementById('barChart').getContext('2d');
        if (barChartInst) barChartInst.destroy();

        barChartInst = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [
                    { type: 'line', label: '% Kumulatif', data: cumPctData, borderColor: 'rgba(220, 53, 69, 1)', backgroundColor: 'rgba(220, 53, 69, 0.1)', borderWidth: 2, pointRadius: 4, yAxisID: 'yPareto', fill: false, datalabels: { anchor: 'bottom', align: 'top', formatter: v => `${v}%`, color: getChartLabelColor, font: { size: 9 } } },
                    { type: 'bar', label: 'Downtime (Menit)', data: minData, backgroundColor: 'rgba(153, 102, 255, 0.7)', borderColor: 'rgba(153, 102, 255, 1)', borderWidth: 1, borderRadius: 4, yAxisID: 'yMins', customMinutes: minData }
                ]
            },
            options: {
                devicePixelRatio: 3,
                responsive: true, maintainAspectRatio: false,
                plugins: { datalabels: { formatter: (v, c) => c.dataset.type === 'line' ? `${v}%` : `${v}m`, font: { weight: 'bold', size: 10 } } },
                scales: { 
                    yMins: { type: 'linear', position: 'left', title: { display: true, text: 'Durasi (Menit)' }, beginAtZero: true, suggestedMax: minData.length > 0 ? Math.max(...minData) * 1.2 : 10 },
                    yPareto: { type: 'linear', position: 'right', title: { display: true, text: 'Persentase Kumulatif (%)' }, min: 0, max: 105, grid: { drawOnChartArea: false }, ticks: { callback: v => `${v}%` } }
                },
                onClick: clickHandler
            }
        });
    }

    function renderParetoSubChart(dataYP, labelTitle) {
        let subAgg = {};
        dataYP.forEach(item => {
            let key = subDrillMode === "Line" ? getLineName(item.FunctionLocation) : getMachineName(item.FunctionLocation, item.ActivityText);
            if(!subAgg[key]) subAgg[key] = { mins: 0, items: [] };
            subAgg[key].mins += item.TotalDownTimeInMinutes;
            subAgg[key].items.push(item);
        });

        let labels = Object.keys(subAgg).sort((a,b) => subAgg[b].mins - subAgg[a].mins); 
        let dataMins = labels.map(k => subAgg[k].mins);

        const ctx = document.getElementById('barChart').getContext('2d');
        if (barChartInst) barChartInst.destroy();

        barChartInst = new Chart(ctx, {
            type: 'bar',
            data: { labels: labels, datasets: [{ label: `Total Downtime (Menit) - ${labelTitle}`, data: dataMins, backgroundColor: 'rgba(255, 159, 64, 0.85)', borderColor: 'rgba(255, 159, 64, 1)', borderWidth: 1, borderRadius: 4 }] },
            options: { 
                devicePixelRatio: 3,
                responsive: true, maintainAspectRatio: false, 
                plugins: { datalabels: { anchor: 'end', align: 'top', color: getChartLabelColor, font: { weight: 'bold', size: 11 }, formatter: val => `${val} m` } }, 
                scales: { y: { beginAtZero: true, suggestedMax: dataMins.length > 0 ? Math.max(...dataMins) * 1.2 : 10 } }, 
                onClick: (e, elements) => { 
                    if (elements.length > 0) { 
                        const idx = elements[0].index; 
                        const key = labels[idx]; 
                        loadDrillDownTable(`${labelTitle} ➔ ${key}`, subAgg[key].items); 
                    } 
                } 
            }
        });
    }

    function renderTrendChart(dataYP, dataYR, mode, labelTitle) {
        let ypAgg = {};
        dataYP.forEach(item => {
            let key = getGroupingKey(new Date(item.NotificationDate), mode, item.WeekKalendarIndofood);
            if (!ypAgg[key]) ypAgg[key] = { mins: 0, items: [] };
            ypAgg[key].mins += item.TotalDownTimeInMinutes;
            ypAgg[key].items.push(item);
        });
        
        let yrAgg = {};
        dataYR.forEach(item => {
            let key = getGroupingKey(new Date(item.PostingDate), mode, item.WeekOfBasicFinishedDate);
            if (!yrAgg[key]) yrAgg[key] = 0;
            yrAgg[key] += (item.PlannedHour * 60); 
        });

        let rawKeys = Object.keys(ypAgg);
        rawKeys.sort((a, b) => {
            if (mode === "Month") {
                let numA = parseInt(a.replace(/\D/g, '')) || 0;
                let numB = parseInt(b.replace(/\D/g, '')) || 0;
                return numA - numB;
            }
            return a.localeCompare(b);
        });

        let labels = [], percentData = [], minuteData = [], targetLine = [], actualKeys = [];
        rawKeys.forEach(k => {
            actualKeys.push(k);
            labels.push(formatLabel(k, mode));
            
            let dtMins = ypAgg[k].mins;            
            let plannedMins = yrAgg[k] || 0;       
            minuteData.push(dtMins);
            percentData.push(plannedMins > 0 ? ((dtMins / plannedMins) * 100).toFixed(2) : 0);
            targetLine.push(window.AppConfig.TargetDowntime);
        });

        const ctx = document.getElementById('barChart').getContext('2d');
        if (barChartInst) barChartInst.destroy();

        barChartInst = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [
                    { type: 'line', label: 'Standar (' + window.AppConfig.TargetDowntime + '%)', data: targetLine, borderColor: 'rgba(220, 53, 69, 1)', borderWidth: 2, borderDash: [5, 5], pointRadius: 0, fill: false, datalabels: { display: false } },
                    { 
                        type: 'line', label: 'Downtime (%)', data: percentData, backgroundColor: 'rgba(54, 162, 235, 0.2)', borderColor: 'rgba(54, 162, 235, 1)', borderWidth: 2, 
                        pointBackgroundColor: 'rgba(54, 162, 235, 1)', pointBorderColor: '#fff', pointBorderWidth: 1, pointRadius: 4, pointHoverRadius: 6,
                        fill: true, tension: 0.1, customMinutes: minuteData 
                    }
                ]
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                plugins: {
                    datalabels: { 
                        anchor: 'end', align: 'top', offset: 4, color: getChartLabelColor, textAlign: 'center',
                        font: function(context) {
                            let count = context.chart.data.labels.length;
                            return { weight: 'bold', size: count > 15 ? 8 : 10 };
                        },
                        display: function(context) {
                            return !context.dataset.label.includes('Standar');
                        },
                        formatter: function(v, ctx) { return v == 0 ? null : `${v}%`; } 
                    },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                let label = context.dataset.label || '';
                                if (label) label += ': ';
                                if (context.parsed.y !== null) {
                                    label += context.parsed.y + '%';
                                    if (context.dataset.customMinutes && context.dataset.customMinutes[context.dataIndex] !== undefined) {
                                        label += ` (${context.dataset.customMinutes[context.dataIndex]} menit)`;
                                    }
                                }
                                return label;
                            }
                        }
                    }
                },
                scales: { y: { beginAtZero: true, suggestedMax: 3, title: { display: true, text: 'Persentase Downtime (%)' } } },
                onClick: (e, elements) => { 
                    if (elements.length > 0) { 
                        const idx = elements[0].index; 
                        const clickedKey = actualKeys[idx];
                        const clickedLabel = labels[idx];
                        
                        let nextMode = mode === 'Month' ? 'Week' : (mode === 'Week' ? 'Day' : 'None');
                        if (nextMode === 'None') return;

                        let subYP = dataYP.filter(item => getGroupingKey(new Date(item.NotificationDate), mode, item.WeekKalendarIndofood) === clickedKey);
                        let subYR = dataYR.filter(item => getGroupingKey(new Date(item.PostingDate), mode, item.WeekOfBasicFinishedDate) === clickedKey);

                        drillHistory.push({ type: 'Pareto', mode: nextMode, label: clickedLabel, dataYP: subYP, dataYR: subYR });
                        renderCurrentState();
                    } 
                }
            }
        });
    }

    function updateMTTRAndMTBF(dataYP, dataYR) {
        let freqBreakdown = dataYP.length;
        let totalDowntimeMins = dataYP.reduce((sum, item) => sum + item.TotalDownTimeInMinutes, 0);
        let totalPlannedMins = dataYR.reduce((sum, item) => sum + (item.PlannedHour * 60), 0);

        let mttr = freqBreakdown > 0 ? (totalDowntimeMins / freqBreakdown) : 0;
        let uptimeMins = totalPlannedMins - totalDowntimeMins;
        let mtbf = freqBreakdown > 0 ? (uptimeMins / freqBreakdown) : (uptimeMins > 0 ? uptimeMins : 0);

        let availability = 0;
        if ((mtbf + mttr) > 0) {
            availability = (mtbf / (mtbf + mttr)) * 100;
        }

        // Update UI HTML
        document.getElementById('lblMTTR').innerText = mttr.toFixed(1) + " Menit";
        document.getElementById('lblMTBF').innerText = mtbf.toFixed(1) + " Menit";
        const lblAvail = document.getElementById('lblAvailability');
        if (lblAvail) lblAvail.innerText = availability.toFixed(2) + " %";
    }

    // --- SISANYA ADALAH FUNGSI BAWAAN YANG TETAP SAMA ---

    function calculateTopBadActors(filteredData) {
        let machineMap = {};
        filteredData.forEach(item => {
            let line = getLineName(item.FunctionLocation);
            let machine = getMachineName(item.FunctionLocation, item.ActivityText);
            let nameKey = `${line} - ${machine}`;
            if (!machineMap[nameKey]) machineMap[nameKey] = 0;
            machineMap[nameKey] += item.TotalDownTimeInMinutes;
        });

        let sortedActors = Object.keys(machineMap).map(k => ({ name: k, mins: machineMap[k] })).sort((a,b) => b.mins - a.mins).slice(0, 5);
        const container = document.getElementById("rowBadActors");
        if(sortedActors.length === 0) {
            container.innerHTML = `<div class="col-12"><div class="alert alert-info py-2 small mb-0 text-center">💡 Tidak ada data downtime pada filter periode ini.</div></div>`;
            return;
        }

        let html = `<div class="col-12"><span class="badge bg-danger mb-1 fw-bold"><i class="bi bi-exclamation-triangle-fill me-1"></i> TOP 5 BAD ACTORS PERIODIK :</span></div>`;
        const colors = ["border-danger text-danger", "border-warning text-warning", "border-primary text-primary", "border-success text-success", "border-secondary text-secondary"];
        
        sortedActors.forEach((actor, i) => {
            html += `
                <div class="col shadow-sm">
                    <div class="card bad-actor-card h-100 border-start border-4 ${colors[i % 5]} bg-white" onclick="filterTableByBadActor('${actor.name}')" title="Klik untuk melihat rincian kerusakan">
                        <div class="card-body py-1 px-2 d-flex align-items-center justify-content-between">
                            <div class="text-truncate" style="max-width: 80%;">
                                <span class="fw-bold small d-block text-dark">${actor.name}</span>
                            </div>
                            <span class="badge bg-dark fw-bold">${actor.mins} m</span>
                        </div>
                    </div>
                </div>`;
        });
        container.innerHTML = html;
    }

    function filterTableByBadActor(actorName) {
        let specificItems = baseDataYP.filter(item => {
            let line = getLineName(item.FunctionLocation);
            let machine = getMachineName(item.FunctionLocation, item.ActivityText);
            let currentName = `${line} - ${machine}`;
            return currentName === actorName;
        });
        loadDrillDownTable(`🚨 Investigasi Bad Actor ➔ ${actorName}`, specificItems);
        document.getElementById('lblDetail').scrollIntoView({ behavior: 'smooth', block: 'center' });
    }

    function updatePieChart(filteredData) {
        let reguAgg = { "Regu A": 0, "Regu B": 0, "Regu C": 0, "Lainnya": 0 };
        filteredData.forEach(item => {
            let shift = item.WageGroup_GroupShift || "Lainnya";
            if (shift.includes("A")) reguAgg["Regu A"] += item.TotalDownTimeInMinutes;
            else if (shift.includes("B")) reguAgg["Regu B"] += item.TotalDownTimeInMinutes;
            else if (shift.includes("C")) reguAgg["Regu C"] += item.TotalDownTimeInMinutes;
            else reguAgg["Lainnya"] += item.TotalDownTimeInMinutes;
        });
        const ctx = document.getElementById('pieChart').getContext('2d');
        if (pieChartInst) pieChartInst.destroy();
        pieChartInst = new Chart(ctx, { 
            type: 'pie', 
            data: { labels: Object.keys(reguAgg), datasets: [{ data: Object.values(reguAgg), backgroundColor: ['rgba(255, 99, 132, 0.8)', 'rgba(54, 162, 235, 0.8)', 'rgba(255, 206, 86, 0.8)', 'rgba(201, 203, 207, 0.8)'], borderWidth: 1 }] }, 
            options: { 
                responsive: true, maintainAspectRatio: false, 
                plugins: { datalabels: { color: getChartLabelColor, font: { weight: 'bold', size: 11 }, formatter: (val, ctx) => {
                    if (val === 0) return null;
                    let sum = ctx.chart.data.datasets[0].data.reduce((a, b) => a + b, 0);
                    let percentage = sum > 0 ? (val * 100 / sum).toFixed(1) + "%" : "";
                    return `${val} m (${percentage})`;
                } } },
                onClick: (e, elements) => {
                    if (elements.length > 0) {
                        const idx = elements[0].index;
                        const reguLabel = Object.keys(reguAgg)[idx];
                        
                        let specificItems = filteredData.filter(item => {
                            let shift = item.WageGroup_GroupShift || "Lainnya";
                            if (reguLabel === "Regu A") return shift.includes("A");
                            if (reguLabel === "Regu B") return shift.includes("B");
                            if (reguLabel === "Regu C") return shift.includes("C");
                            return !shift.includes("A") && !shift.includes("B") && !shift.includes("C");
                        });
                        
                        document.getElementById('btnResetRegu').classList.remove('d-none');
                        loadDrillDownTable(`📌 Filter Regu: ${reguLabel}`, specificItems);
                        document.getElementById('lblDetail').scrollIntoView({ behavior: 'smooth', block: 'center' });
                    }
                }
            } 
        });
    }

    function resetReguFilter() {
        document.getElementById('btnResetRegu').classList.add('d-none');
        let data = drillHistory.length > 0 ? drillHistory[drillHistory.length - 1].dataYP : baseDataYP;
        let title = drillHistory.length > 0 ? `Rincian: ${drillHistory[drillHistory.length - 1].label}` : 'Pilih Batang Grafik';
        loadDrillDownTable(title, data);
    }

    function loadDrillDownTable(title, items) {
        document.getElementById('lblDetail').innerText = title;
        currentDrillDownData = items;
        renderTableRows();
    }

    function sortTable(colIdx) {
        if (currentTableMode === "Agg" || !currentDrillDownData || currentDrillDownData.length === 0) return; 
        if (sortState.col === colIdx) { sortState.asc = !sortState.asc; } else { sortState.col = colIdx; sortState.asc = true; }
        currentDrillDownData.sort((a, b) => {
            let valA, valB;
            if (colIdx === 0) { valA = new Date(a.NotificationDate); valB = new Date(b.NotificationDate); }
            else if (colIdx === 1) { valA = getLineName(a.FunctionLocation); valB = getLineName(b.FunctionLocation); }
            else if (colIdx === 2) { valA = getMachineName(a.FunctionLocation, a.ActivityText); valB = getMachineName(b.FunctionLocation, b.ActivityText); }
            else if (colIdx === 4) { valA = a.TotalDownTimeInMinutes; valB = b.TotalDownTimeInMinutes; }
            if (valA < valB) return sortState.asc ? -1 : 1;
            if (valA > valB) return sortState.asc ? 1 : -1;
            return 0;
        });
        renderTableRows();
    }

    function renderTableRows() {
        const thead = document.getElementById('table-header');
        const tbody = document.getElementById('table-body');

        if(currentDrillDownData.length === 0) { 
            tbody.innerHTML = `<tr><td colspan="6" class="text-center py-3">Tidak ada data.</td></tr>`; 
            return; 
        }

        if (currentTableMode === "Raw") {
            thead.innerHTML = `
                <tr>
                    <th style="cursor:pointer" onclick="sortTable(0)">Tanggal ⇅</th>
                    <th style="cursor:pointer" onclick="sortTable(1)">Line ⇅</th>
                    <th style="cursor:pointer" onclick="sortTable(2)">Mesin ⇅</th>
                    <th>Aktivitas Perbaikan (Activity Text)</th>
                    <th style="cursor:pointer" onclick="sortTable(4)">Menit ⇅</th>
                    <th>Regu</th>
                </tr>`;

            let html = "";
            currentDrillDownData.forEach(item => {
                const fDate = new Date(item.NotificationDate).toLocaleDateString('id-ID', { day: '2-digit', month: 'short' });
                let txtAktivitas = item.ActivityText ? item.ActivityText : (item.NotificationDesc || '-');
                let namaLine = getLineName(item.FunctionLocation);
                let namaMesin = getMachineName(item.FunctionLocation, item.ActivityText);
                html += `<tr>
                    <td>${fDate}</td>
                    <td class="fw-bold text-success">${namaLine}</td>
                    <td class="fw-bold text-primary">${namaMesin}</td>
                    <td class="text-truncate" style="max-width: 230px;" title="${txtAktivitas}">${txtAktivitas}</td>
                    <td class="text-danger fw-bold">${item.TotalDownTimeInMinutes}</td>
                    <td><span class="badge bg-secondary">${item.WageGroup_GroupShift || '-'}</span></td>
                </tr>`;
            });
            tbody.innerHTML = html;
        } else {
            thead.innerHTML = `
                <tr>
                    <th>Line</th>
                    <th>Mesin</th>
                    <th>Aktivitas Berulang (Grouped Activity Text)</th>
                    <th class="text-center">Frekuensi (Kali)</th>
                    <th class="text-end">Total Durasi (Menit)</th>
                </tr>`;

            let aggMap = {};
            currentDrillDownData.forEach(item => {
                let line = getLineName(item.FunctionLocation);
                let machine = getMachineName(item.FunctionLocation, item.ActivityText);
                let txt = item.ActivityText ? item.ActivityText.trim() : (item.NotificationDesc || '-');
                let groupKey = `${line}||${machine}||${txt}`;
                if (!aggMap[groupKey]) aggMap[groupKey] = { line: line, machine: machine, text: txt, count: 0, duration: 0 };
                aggMap[groupKey].count += 1;
                aggMap[groupKey].duration += item.TotalDownTimeInMinutes;
            });
            let sortedAgg = Object.values(aggMap).sort((a, b) => b.count - a.count);
            let html = "";
            sortedAgg.forEach(row => {
                html += `<tr>
                    <td class="fw-bold text-success">${row.line}</td>
                    <td class="fw-bold text-primary">${row.machine}</td>
                    <td class="text-truncate" style="max-width: 300px;" title="${row.text}">${row.text}</td>
                    <td class="text-center text-warning fw-bold fs-6">${row.count} x</td>
                    <td class="text-end text-danger fw-bold">${row.duration} min</td>
                </tr>`;
            });
            tbody.innerHTML = html;
        }
    }

    // ==========================================
    // LOGIKA DEEP-DIVE MODAL MTTR & MTBF
    // ==========================================
    let chartInstMTTR = null;
    let chartInstMTBF = null;

    function openMTTRDetails() {
        const modal = new bootstrap.Modal(document.getElementById('modalMTTR'));
        modal.show();

        // 1. Render Tabel: Top 10 Breakdown Terlama dari data filter saat ini
        let activeDataYP = drillHistory.length > 0 ? drillHistory[drillHistory.length - 1].dataYP : baseDataYP;
        let sortedYP = [...activeDataYP].sort((a, b) => b.TotalDownTimeInMinutes - a.TotalDownTimeInMinutes).slice(0, 10);
        let tbody = document.getElementById('tbodyMTTR');
        let htmlTable = '';
        
        if(sortedYP.length === 0) {
            htmlTable = '<tr><td colspan="5" class="text-center py-4 text-muted">Tidak ada data breakdown pada periode ini.</td></tr>';
        } else {
            sortedYP.forEach((item, index) => {
                let dateStr = new Date(item.NotificationDate).toLocaleDateString('id-ID', {day:'2-digit', month:'short', year:'numeric'});
                let machine = getMachineName(item.FunctionLocation, item.ActivityText);
                let line = getLineName(item.FunctionLocation);
                htmlTable += `<tr>
                    <td class="text-center fw-bold">${index + 1}</td>
                    <td class="text-center">${dateStr}</td>
                    <td class="fw-bold text-primary">${line} - ${machine}</td>
                    <td class="text-truncate" style="max-width: 350px;" title="${item.ActivityText || item.NotificationDesc}">${item.ActivityText || item.NotificationDesc || '-'}</td>
                    <td class="text-danger fw-bold text-end pe-3">${item.TotalDownTimeInMinutes} Menit</td>
                </tr>`;
            });
        }
        tbody.innerHTML = htmlTable;

        // 2. Render Grafik Historis 
        let activeMode = drillHistory.length > 0 ? drillHistory[drillHistory.length - 1].mode : document.getElementById("dimMode").value;
        renderTrendChartMTTR(activeDataYP, activeMode);
    }

    function renderTrendChartMTTR(activeDataYP, activeMode) {
        let aggYP = {};
        activeDataYP.forEach(item => {
            let key = getGroupingKey(new Date(item.NotificationDate), activeMode, item.WeekKalendarIndofood);
            if(!aggYP[key]) aggYP[key] = { mins: 0, freq: 0 };
            aggYP[key].mins += item.TotalDownTimeInMinutes;
            aggYP[key].freq += 1;
        });

        let rawKeys = Object.keys(aggYP);
        rawKeys.sort((a, b) => {
            if (activeMode === "Year") return monthNames.indexOf(a) - monthNames.indexOf(b);
            if (activeMode === "Month") {
                let numA = parseInt(a.replace(/\D/g, '')) || 0;
                let numB = parseInt(b.replace(/\D/g, '')) || 0;
                return numA - numB;
            }
            return a.localeCompare(b);
        });

        let labels = [], mttrData = [];
        rawKeys.forEach(k => {
            labels.push(formatLabel(k, activeMode));
            mttrData.push(+(aggYP[k].mins / aggYP[k].freq).toFixed(1));
        });

        let periodText = activeMode === "Year" ? "Bulanan" : (activeMode === "Month" ? "Mingguan" : (activeMode === "Week" ? "Harian" : "Historis"));
        document.getElementById('headerChartMTTR').innerText = `Tren MTTR Historis (${periodText})`;

        const ctx = document.getElementById('chartMTTR').getContext('2d');
        if (chartInstMTTR) chartInstMTTR.destroy();
        chartInstMTTR = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Rata-rata Menit Perbaikan',
                    data: mttrData,
                    borderColor: 'rgba(220, 53, 69, 1)', backgroundColor: 'rgba(220, 53, 69, 0.1)',
                    borderWidth: 2, pointRadius: 4, fill: true, tension: 0.3
                }]
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                plugins: { datalabels: { align: 'top', color: getChartLabelColor, font: {weight:'bold'}, formatter: v => v + 'm' } },
                scales: { y: { beginAtZero: true } }
            }
        });
    }

    function openMTBFDetails() {
        const modal = new bootstrap.Modal(document.getElementById('modalMTBF'));
        modal.show();

        // 1. Render Tabel: Mesin Paling Sering Rusak dari data filter saat ini
        let activeDataYP = drillHistory.length > 0 ? drillHistory[drillHistory.length - 1].dataYP : baseDataYP;
        let freqMap = {};
        activeDataYP.forEach(item => {
            let machine = getMachineName(item.FunctionLocation, item.ActivityText);
            let line = getLineName(item.FunctionLocation);
            let key = `${line} - ${machine}`;
            if(!freqMap[key]) freqMap[key] = { count: 0, totalMins: 0 };
            freqMap[key].count += 1;
            freqMap[key].totalMins += item.TotalDownTimeInMinutes;
        });

        let sortedFreq = Object.keys(freqMap).map(k => ({ name: k, count: freqMap[k].count, mins: freqMap[k].totalMins }))
                                             .sort((a,b) => b.count - a.count).slice(0, 10);

        let tbody = document.getElementById('tbodyMTBF');
        let htmlTable = '';
        if(sortedFreq.length === 0) {
            htmlTable = '<tr><td colspan="4" class="text-center py-4 text-muted">Tidak ada data breakdown pada periode ini.</td></tr>';
        } else {
            sortedFreq.forEach((item, index) => {
                htmlTable += `<tr>
                    <td class="text-center fw-bold">${index + 1}</td>
                    <td class="fw-bold text-success ps-3">${item.name}</td>
                    <td class="text-warning text-center fw-bold fs-6">${item.count} Kali</td>
                    <td class="text-danger text-end fw-bold pe-3">${item.mins} Menit</td>
                </tr>`;
            });
        }
        tbody.innerHTML = htmlTable;

        // 2. Render Grafik Historis 
        let activeDataYR = drillHistory.length > 0 ? drillHistory[drillHistory.length - 1].dataYR : baseDataYR;
        let activeMode = drillHistory.length > 0 ? drillHistory[drillHistory.length - 1].mode : document.getElementById("dimMode").value;
        renderTrendChartMTBF(activeDataYP, activeDataYR, activeMode);
    }

    function renderTrendChartMTBF(activeDataYP, activeDataYR, activeMode) {
        let aggYP = {}, aggYR = {};
        
        activeDataYP.forEach(item => {
            let key = getGroupingKey(new Date(item.NotificationDate), activeMode, item.WeekKalendarIndofood);
            if(!aggYP[key]) aggYP[key] = { mins: 0, freq: 0 };
            aggYP[key].mins += item.TotalDownTimeInMinutes;
            aggYP[key].freq += 1;
        });

        activeDataYR.forEach(item => {
            let key = getGroupingKey(new Date(item.PostingDate), activeMode, item.WeekOfBasicFinishedDate);
            if(!aggYR[key]) aggYR[key] = 0;
            aggYR[key] += (item.PlannedHour * 60);
        });

        let rawKeys = new Set([...Object.keys(aggYP), ...Object.keys(aggYR)]);
        let sortedKeys = Array.from(rawKeys).sort((a, b) => {
            if (activeMode === "Year") return monthNames.indexOf(a) - monthNames.indexOf(b);
            if (activeMode === "Month") {
                let numA = parseInt(a.replace(/\D/g, '')) || 0;
                let numB = parseInt(b.replace(/\D/g, '')) || 0;
                return numA - numB;
            }
            return a.localeCompare(b);
        });

        let labels = [], mtbfData = [];
        sortedKeys.forEach(k => {
            labels.push(formatLabel(k, activeMode));
            let mins = aggYP[k] ? aggYP[k].mins : 0;
            let freq = aggYP[k] ? aggYP[k].freq : 0;
            let planned = aggYR[k] ? aggYR[k] : 0;
            let uptime = planned - mins;
            let val = freq > 0 ? (uptime / freq) : (uptime > 0 ? uptime : 0);
            mtbfData.push(+(val).toFixed(0));
        });

        let periodText = activeMode === "Year" ? "Bulanan" : (activeMode === "Month" ? "Mingguan" : (activeMode === "Week" ? "Harian" : "Historis"));
        document.getElementById('headerChartMTBF').innerText = `📈 Tren MTBF Historis (${periodText})`;

        const ctx = document.getElementById('chartMTBF').getContext('2d');
        if (chartInstMTBF) chartInstMTBF.destroy();
        chartInstMTBF = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Rata-rata Uptime Antar Kerusakan (Menit)',
                    data: mtbfData,
                    borderColor: 'rgba(25, 135, 84, 1)', backgroundColor: 'rgba(25, 135, 84, 0.1)',
                    borderWidth: 2, pointRadius: 4, fill: true, tension: 0.3
                }]
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                plugins: { datalabels: { align: 'top', color: getChartLabelColor, font: {weight:'bold'}, formatter: v => v + 'm' } },
                scales: { y: { beginAtZero: true } }
            }
        });
    }

    // ==========================================
    // LOGIKA DEEP-DIVE MODAL AVAILABILITY
    // ==========================================
    let chartInstAvailability = null;

    function openAvailabilityDetails() {
        const modal = new bootstrap.Modal(document.getElementById('modalAvailability'));
        modal.show();

        let activeDataYP = drillHistory.length > 0 ? drillHistory[drillHistory.length - 1].dataYP : baseDataYP;
        let activeDataYR = drillHistory.length > 0 ? drillHistory[drillHistory.length - 1].dataYR : baseDataYR;
        
        let downtimeMap = {};
        activeDataYP.forEach(item => {
            let machine = getMachineName(item.FunctionLocation, item.ActivityText);
            let line = getLineName(item.FunctionLocation);
            let key = `${line} - ${machine}`;
            if(!downtimeMap[key]) downtimeMap[key] = 0;
            downtimeMap[key] += item.TotalDownTimeInMinutes;
        });

        // Estimasi availability per mesin
        let totalPlannedMins = activeDataYR.reduce((sum, item) => sum + (item.PlannedHour * 60), 0);

        let sortedDowntime = Object.keys(downtimeMap).map(k => {
            let mins = downtimeMap[k];
            let avail = totalPlannedMins > 0 ? Math.max(0, ((totalPlannedMins - mins) / totalPlannedMins) * 100) : 0;
            return { name: k, mins: mins, avail: avail };
        }).sort((a,b) => b.mins - a.mins).slice(0, 10);

        let tbody = document.getElementById('tbodyAvailability');
        let htmlTable = '';
        if(sortedDowntime.length === 0) {
            htmlTable = '<tr><td colspan="4" class="text-center py-4 text-muted">Tidak ada data pada periode ini.</td></tr>';
        } else {
            sortedDowntime.forEach((item, index) => {
                htmlTable += `<tr>
                    <td class="text-center fw-bold">${index + 1}</td>
                    <td class="fw-bold text-primary ps-3">${item.name}</td>
                    <td class="text-danger text-end fw-bold pe-3">${item.mins} Menit</td>
                    <td class="text-success text-center fw-bold">${item.avail.toFixed(2)} %</td>
                </tr>`;
            });
        }
        tbody.innerHTML = htmlTable;

        let activeMode = drillHistory.length > 0 ? drillHistory[drillHistory.length - 1].mode : document.getElementById("dimMode").value;
        renderTrendChartAvailability(activeDataYP, activeDataYR, activeMode);
    }

    function renderTrendChartAvailability(activeDataYP, activeDataYR, activeMode) {
        let aggYP = {}, aggYR = {};
        
        activeDataYP.forEach(item => {
            let key = getGroupingKey(new Date(item.NotificationDate), activeMode, item.WeekKalendarIndofood);
            if(!aggYP[key]) aggYP[key] = { mins: 0, freq: 0 };
            aggYP[key].mins += item.TotalDownTimeInMinutes;
            aggYP[key].freq += 1;
        });

        activeDataYR.forEach(item => {
            let key = getGroupingKey(new Date(item.PostingDate), activeMode, item.WeekOfBasicFinishedDate);
            if(!aggYR[key]) aggYR[key] = 0;
            aggYR[key] += (item.PlannedHour * 60);
        });

        let rawKeys = new Set([...Object.keys(aggYP), ...Object.keys(aggYR)]);
        let sortedKeys = Array.from(rawKeys).sort((a, b) => {
            if (activeMode === "Year") return monthNames.indexOf(a) - monthNames.indexOf(b);
            if (activeMode === "Month") {
                let numA = parseInt(a.replace(/\D/g, '')) || 0;
                let numB = parseInt(b.replace(/\D/g, '')) || 0;
                return numA - numB;
            }
            return a.localeCompare(b);
        });

        let labels = [], availData = [];
        sortedKeys.forEach(k => {
            labels.push(formatLabel(k, activeMode));
            let mins = aggYP[k] ? aggYP[k].mins : 0;
            let freq = aggYP[k] ? aggYP[k].freq : 0;
            let planned = aggYR[k] ? aggYR[k] : 0;
            
            let mttr = freq > 0 ? (mins / freq) : 0;
            let uptime = planned - mins;
            let mtbf = freq > 0 ? (uptime / freq) : (uptime > 0 ? uptime : 0);
            
            let avail = 0;
            if ((mtbf + mttr) > 0) {
                avail = (mtbf / (mtbf + mttr)) * 100;
            }
            availData.push(+(avail).toFixed(2));
        });

        let periodText = activeMode === "Year" ? "Bulanan" : (activeMode === "Month" ? "Mingguan" : (activeMode === "Week" ? "Harian" : "Historis"));
        document.getElementById('headerChartAvailability').innerText = `📈 Tren Availability Historis (${periodText})`;

        const ctx = document.getElementById('chartAvailability').getContext('2d');
        if (chartInstAvailability) chartInstAvailability.destroy();
        chartInstAvailability = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Machine Availability (%)',
                    data: availData,
                    borderColor: 'rgba(13, 110, 253, 1)', backgroundColor: 'rgba(13, 110, 253, 0.1)',
                    borderWidth: 2, pointRadius: 4, fill: true, tension: 0.3
                }]
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                plugins: { datalabels: { align: 'top', color: getChartLabelColor, font: {weight:'bold'}, formatter: v => v + '%' } },
                scales: { y: { beginAtZero: true, max: 100, min: 0 } }
            }
        });
    }
    
    // --- REPORT BUILDER LOGIC ---
    