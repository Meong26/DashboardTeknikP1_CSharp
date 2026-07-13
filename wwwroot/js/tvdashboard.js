
        Chart.register(ChartDataLabels);

        let dataAnalitik = null;
        let dataPemakaian = null;
        let dataEWS = null;

        // --- HELPER FUNCTIONS (Diadaptasi dari Index.cshtml) ---
        const monthNames = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
        function formatLabel(key, mode) {
            if (!key) return "Unknown";
            if (mode === "Year") return key; // e.g. "Jan", "Feb"
            if (mode === "Month") { let p = key.split('-'); return p.length === 2 ? `W${p[0]} (${monthNames[parseInt(p[1])-1]})` : key; }
            if (mode === "Week") { let p = key.split('-'); return p.length === 3 ? `${p[2]} ${monthNames[parseInt(p[1])-1]}` : key; }
            return key;
        }

        function getWeekNumber(d) {
            d = new Date(Date.UTC(d.getFullYear(), d.getMonth(), d.getDate()));
            d.setUTCDate(d.getUTCDate() + 4 - (d.getUTCDay() || 7));
            var yearStart = new Date(Date.UTC(d.getUTCFullYear(), 0, 1));
            var weekNo = Math.ceil((((d - yearStart) / 86400000) + 1) / 7);
            return weekNo;
        }

        function getGroupingKey(dateObj, mode, weekInfo) {
            if (mode === "Year") return monthNames[dateObj.getMonth()];
            if (mode === "Month") {
                let wk = parseInt(weekInfo);
                if (isNaN(wk)) wk = getWeekNumber(dateObj);
                return `${wk}-${dateObj.getMonth() + 1}`;
            }
            if (mode === "Week") {
                return `${dateObj.getFullYear()}-${String(dateObj.getMonth() + 1).padStart(2, '0')}-${String(dateObj.getDate()).padStart(2, '0')}`;
            }
            return "Unknown";
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
            if (kodeMesin === "009") {
                if (txt.includes("xray") || txt.includes("x-ray") || txt.includes("x ray")) return "X-Ray";
                if (lineNum === 8 && (txt.includes("bandid") || txt.includes("bandeed") || txt.includes("banded") || txt.includes("5in1") || txt.includes("4in1"))) return "Bandeed";
                if ((lineNum === 5 || lineNum === 6) && (txt.includes("cuploader") || txt.includes("cup loader") || txt.includes("suplai cup") || txt.includes("supplycup") || txt.includes("heater cup seal") || txt.includes("heater seal") || txt.includes("preheater") || txt.includes("pre heater") || txt.includes("pallet") || txt.includes("palet"))) return "Cuploader";
                if (txt.includes("autoloader") || txt.includes("auto loader") || txt.includes("bumbu") || txt.includes("minyak")) return "Autoloader";
                return "Wrapper";
            }
            return funcLoc;
        }

        async function initData() {
            try {
                const [resAnalitik, resPemakaian, resEWS] = await Promise.all([
                    fetch('/Home/GetDashboardData').then(r => r.json()),
                    fetch('/Pemakaian/GetHistoryData').then(r => r.json()),
                    fetch('/Sparepart/GetApiData').then(r => r.json())
                ]);
                dataAnalitik = resAnalitik;
                dataPemakaian = resPemakaian;
                dataEWS = resEWS;

                buildSlides();
                document.getElementById('loadingOverlay').style.display = 'none';
            } catch (err) {
                console.error(err);
                document.getElementById('loadingOverlay').innerHTML = `<h3 class="text-danger">Gagal memuat data: ${err.message}</h3>`;
            }
        }

        function buildSlides() {
            const inner = document.getElementById('carouselInner');
            let slideIndex = 0;

            function addSlide(title, contentHtml, link) {
                const isActive = slideIndex === 0 ? 'active' : '';
                const id = `slide_${slideIndex}`;
                inner.innerHTML += `
                    <div class="carousel-item ${isActive}" onclick="window.location.href='${link}'" id="${id}">
                        <div class="slide-content">
                            <div class="slide-title">${title}</div>
                            ${contentHtml}
                        </div>
                    </div>
                `;
                slideIndex++;
                return id;
            }

            // FILTER LOGIC
            const dYP = (dataAnalitik.ypData || []).filter(item => item.NotificationType === 'NT');
            const dYR = dataAnalitik.yrData || [];
            
            // Tentukan Bulan & Minggu berjalan dari data teratas/terbaru
            let maxDate = new Date('2000-01-01');
            let currentWeekStr = "";
            dYP.forEach(i => { 
                let d = new Date(i.NotificationDate); 
                if (d > maxDate) { maxDate = d; currentWeekStr = i.WeekKalendarIndofood; } 
            });
            
            const currentMonthStr = `${maxDate.getFullYear()}-${String(maxDate.getMonth() + 1).padStart(2, '0')}`;

            let filterMonthYP = dYP.filter(i => {
                let d = new Date(i.NotificationDate);
                return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}` === currentMonthStr;
            });
            let filterMonthYR = dYR.filter(i => {
                let d = new Date(i.PostingDate);
                return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}` === currentMonthStr;
            });

            let filterWeekYP = filterMonthYP.filter(i => i.WeekKalendarIndofood === currentWeekStr);
            let filterWeekYR = filterMonthYR.filter(i => i.WeekOfBasicFinishedDate === currentWeekStr);

            // Slide 1: Run Chart (This Year - All Months, grouped by Week)
            addSlide(`Run Chart Downtime vs Jam Terencana - Semua Bulan`, `<div class="chart-wrapper"><canvas id="chartS1"></canvas></div>`, '/Home/Index');
            
            // Slide 2: Run Chart (This Month)
            addSlide(`Run Chart - Analitik Bulan ${monthNames[maxDate.getMonth()]} ${maxDate.getFullYear()}`, `<div class="chart-wrapper"><canvas id="chartS2"></canvas></div>`, '/Home/Index');
            
            // Slide 3: Run Chart (This Week)
            addSlide(`Run Chart - Analitik Minggu Ke-${currentWeekStr}`, `<div class="chart-wrapper"><canvas id="chartS3"></canvas></div>`, '/Home/Index');
            
            // Slide 4: Bar Chart DT by Line (This Week)
            addSlide(`Downtime per Line - Minggu Ke-${currentWeekStr}`, `<div class="chart-wrapper"><canvas id="chartS4"></canvas></div>`, '/Home/Index');
            
            // Slide 5: Bar Chart DT by Machine (This Week)
            addSlide(`Downtime per Mesin - Minggu Ke-${currentWeekStr}`, `<div class="chart-wrapper"><canvas id="chartS5"></canvas></div>`, '/Home/Index');
            
            // Slide 6: Table Top 5 Bad Actor
            let badActorHtml = buildBadActorTable(filterWeekYP);
            addSlide(`Top 5 Bad Actor - Minggu Ke-${currentWeekStr}`, badActorHtml, '/Home/Index');

            // Slide 7: Kesimpulan MTTR, MTBF, Availability
            let kpiHtml = buildKpiSummary(filterWeekYP, filterWeekYR);
            addSlide(`Kesimpulan Kehandalan Sistem - Minggu Ke-${currentWeekStr}`, kpiHtml, '/Home/Index');

            // Slide 8: Estimasi Rupiah Sparepart & RM Cost Ratio
            let costHtml = buildSparepartCost(currentWeekStr);
            addSlide(`Analitik Pemakaian Sparepart - Minggu Ke-${currentWeekStr}`, costHtml, '/Pemakaian/Index');

            // Slide 9: EWS Table
            let ewsHtml = buildEwsTable();
            addSlide(`Early Warning System (EWS) - 10 Stok Paling Kritis`, ewsHtml, '/Sparepart/Index');

            // --- RENDER CHARTS ---
            setTimeout(() => {
                // Di Index.cshtml, saat filter 'Semua Bulan' digunakan, modenya adalah 'Month' (mengelompokkan by Week).
                renderTrendChart('chartS1', dYP, dYR, 'Month');
                renderTrendChart('chartS2', filterMonthYP, filterMonthYR, 'Month');
                renderTrendChart('chartS3', filterWeekYP, filterWeekYR, 'Week');
                renderBarChart('chartS4', filterWeekYP, 'LINE');
                renderBarChart('chartS5', filterWeekYP, 'MACHINE');
            }, 500); // beri jeda sebentar agar DOM selesai dirender
            
            // Agar chart menyesuaikan ulang jika ukuran rusak saat di-hide
            let myCarousel = document.getElementById('tvCarousel');
            var carousel = new bootstrap.Carousel(document.getElementById('tvCarousel'), {
                interval: window.AppConfig.TvModeDuration,
                pause: false
            });

            let cycleCount = 0;
            myCarousel.addEventListener('slide.bs.carousel', function (e) {
                // e.from adalah index slide saat ini, e.to adalah index slide tujuan
                let totalSlides = document.querySelectorAll('.carousel-item').length;
                if (e.from === totalSlides - 1 && e.to === 0) {
                    cycleCount++;
                    // Refresh halaman otomatis setiap 5 putaran (sekitar 7,5 menit) agar data terbaru masuk
                    // dan mencegah memory leak pada browser TV yang menyala berhari-hari.
                    if (cycleCount >= 5) {
                        window.location.reload();
                    }
                }
            });

            myCarousel.addEventListener('slid.bs.carousel', function (e) {
                // Resize trigger for active canvas
                let canvases = e.relatedTarget.querySelectorAll('canvas');
                canvases.forEach(c => {
                    let chartInst = Chart.getChart(c);
                    if (chartInst) chartInst.resize();
                });
            });
        }

        // --- CHART RENDERERS ---
        function renderTrendChart(canvasId, ypArr, yrArr, mode) {
            let ypAgg = {}, yrAgg = {};
            ypArr.forEach(item => {
                let key = getGroupingKey(new Date(item.NotificationDate), mode, item.WeekKalendarIndofood);
                if (!ypAgg[key]) ypAgg[key] = { mins: 0 };
                ypAgg[key].mins += item.TotalDownTimeInMinutes;
            });
            yrArr.forEach(item => {
                let key = getGroupingKey(new Date(item.PostingDate), mode, item.WeekOfBasicFinishedDate);
                if (!yrAgg[key]) yrAgg[key] = 0;
                yrAgg[key] += (item.PlannedHour * 60);
            });

            let rawKeys = Object.keys(ypAgg);
            rawKeys.sort((a, b) => {
                if (mode === "Year") return monthNames.indexOf(a) - monthNames.indexOf(b);
                if (mode === "Month") return parseInt(a.replace(/\D/g, '')) - parseInt(b.replace(/\D/g, ''));
                return a.localeCompare(b);
            });

            let labels = [], percentData = [], minuteData = [], targetLine = [];
            rawKeys.forEach(k => {
                labels.push(formatLabel(k, mode));
                let dtMins = ypAgg[k].mins;            
                let plannedMins = yrAgg[k] || 0;       
                minuteData.push(dtMins);
                percentData.push(plannedMins > 0 ? ((dtMins / plannedMins) * 100).toFixed(2) : 0);
                targetLine.push(window.AppConfig.TargetDowntime);
            });

            new Chart(document.getElementById(canvasId).getContext('2d'), {
                type: 'line',
                
                data: { 
                    labels: labels, 
                    datasets: [
                        { type: 'line', label: 'Standar (' + window.AppConfig.TargetDowntime + '%)', data: targetLine, borderColor: 'rgba(220, 53, 69, 1)', borderWidth: 3, borderDash: [5, 5], pointRadius: 0, fill: false, datalabels: { display: false } },
                        { type: 'line', label: 'Downtime', data: percentData, backgroundColor: 'rgba(13, 110, 253, 0.2)', borderColor: '#0d6efd', borderWidth: 4, pointRadius: 6, fill: true, customMinutes: minuteData }
                    ]
                },
                options: {
                    responsive: true, maintainAspectRatio: false,
                    plugins: { 
                        legend: { display: true, labels: { font: { size: 16 } } },
                        datalabels: { align: 'top', color: '#000', font: { weight: 'bold', size: 16 }, formatter: (v, ctx) => v == 0 ? null : `${v}%\n(${ctx.dataset.customMinutes[ctx.dataIndex]}m)` }
                    },
                    scales: { 
                        y: { beginAtZero: true, suggestedMax: 3, ticks: { font: { size: 16 } } },
                        x: { ticks: { font: { size: 16 } } }
                    }
                }
            });
        }

        function renderBarChart(canvasId, ypArr, groupBy) {
            let aggMap = {};
            ypArr.forEach(item => {
                let key = groupBy === 'LINE' ? getLineName(item.FunctionLocation) : getMachineName(item.FunctionLocation, item.ActivityText);
                if(!aggMap[key]) aggMap[key] = 0;
                aggMap[key] += item.TotalDownTimeInMinutes;
            });

            let sortedKeys = Object.keys(aggMap).sort((a,b) => aggMap[b] - aggMap[a]);
            let values = sortedKeys.map(k => aggMap[k]);

            new Chart(document.getElementById(canvasId).getContext('2d'), {
                type: 'bar',
                data: { labels: sortedKeys, datasets: [{ data: values, backgroundColor: groupBy === 'LINE' ? '#0dcaf0' : '#ffc107' }] },
                options: {
                    responsive: true, maintainAspectRatio: false,
                    plugins: { 
                        legend: { display: false }, 
                        datalabels: { 
                            anchor: 'end', align: 'top', offset: 8, color: '#000', font: { weight: 'bold', size: 16 }, 
                            formatter: function(v, ctx) { return v > 0 ? `${v}m` : null; } 
                        } 
                    },
                    scales: { 
                        y: { beginAtZero: true, grace: '20%', ticks: { font: { size: 16 } } },
                        x: { ticks: { autoSkip: false, font: {size: 14} } }
                    }
                }
            });
        }

        // --- HTML BUILDERS ---
        function buildBadActorTable(ypArr) {
            let map = {};
            ypArr.forEach(item => {
                let lineStr = getLineName(item.FunctionLocation);
                let macStr = getMachineName(item.FunctionLocation, item.ActivityText);
                let k = macStr + " (" + lineStr + ")"; // Misal: Wrapper (Line 4)
                if(!map[k]) map[k] = 0;
                map[k] += item.TotalDownTimeInMinutes;
            });
            let actors = Object.keys(map).map(k => ({ name: k, mins: map[k] })).sort((a,b) => b.mins - a.mins).slice(0, 5);
            
            if(actors.length === 0) return `<div class="alert alert-info text-center fs-3 mt-5">Tidak ada data kerusakan.</div>`;
            
            let trs = actors.map((a, i) => `
                <tr>
                    <td class="text-center fw-bold fs-3">${i+1}</td>
                    <td class="fs-3">${a.name}</td>
                    <td class="text-end text-danger fw-bold fs-3">${a.mins.toFixed(0)} Menit</td>
                </tr>
            `).join('');
            
            return `<div class="table-responsive h-100 d-flex flex-column justify-content-center">
                        <table class="table table-striped table-bordered mb-0">
                            <thead><tr><th class="text-center w-10">Peringkat</th><th>Mesin / Line</th><th class="text-end w-25">Total Downtime</th></tr></thead>
                            <tbody>${trs}</tbody>
                        </table>
                    </div>`;
        }

        function buildKpiSummary(ypArr, yrArr) {
            let dt = ypArr.reduce((s, i) => s + i.TotalDownTimeInMinutes, 0);
            let pt = yrArr.reduce((s, i) => s + (i.PlannedHour * 60), 0);
            let breaks = ypArr.length;
            let mttr = breaks > 0 ? dt / breaks : 0;
            let up = pt - dt;
            let mtbf = breaks > 0 ? up / breaks : (up > 0 ? up : 0);
            let avail = (mtbf + mttr) > 0 ? (mtbf / (mtbf + mttr)) * 100 : 0;

            return `
                <div class="row h-100 g-4">
                    <div class="col-6"><div class="kpi-card"><div class="kpi-label">TOTAL DOWNTIME</div><div class="kpi-value text-dark">${dt.toFixed(0)} <span class="fs-4">Menit</span></div></div></div>
                    <div class="col-6"><div class="kpi-card"><div class="kpi-label">MACHINE AVAILABILITY</div><div class="kpi-value text-primary">${avail.toFixed(2)} <span class="fs-4">%</span></div></div></div>
                    <div class="col-6"><div class="kpi-card"><div class="kpi-label">MTTR</div><div class="kpi-value">${mttr.toFixed(1)} <span class="fs-4">Menit</span></div></div></div>
                    <div class="col-6"><div class="kpi-card"><div class="kpi-label">MTBF</div><div class="kpi-value text-success">${mtbf.toFixed(1)} <span class="fs-4">Menit</span></div></div></div>
                </div>`;
        }

        function buildSparepartCost(currentWeek) {
            // Asumsi format data Pemakaian
            const items = dataPemakaian.HistoryData || [];
            
            // Gunakan matching nilai currentWeekStr (contoh "27") jika data Pemakaian juga punya format minggu yang sama, 
            // jika tidak, fallback ke getWeekNumber tanggal. 
            // Untuk amannya, kita parse angkanya.
            let weekNumeric = parseInt(currentWeek.replace(/\D/g, '')) || 0;

            let weekItems = items.filter(i => {
                return getWeekNumber(new Date(i.TanggalInput)) === weekNumeric;
            });
            
            let cost = weekItems.reduce((s, i) => s + i.TotalHarga, 0);
            
            // Estimasi Rasio (Cost / Rata-rata target, atau tampilkan nilai statis jika tak ada target harian di sini)
            // Di sini kita tampilkan nilai aslinya dengan format rupiah
            let costStr = new Intl.NumberFormat('id-ID', { style: 'currency', currency: 'IDR' }).format(cost);
            let itemsCount = weekItems.length;

            return `
                <div class="row h-100 g-4 align-items-center">
                    <div class="col-12">
                        <div class="kpi-card bg-light border-info">
                            <div class="kpi-label text-info">TOTAL BIAYA SPAREPART DIAMBIL MINGGU INI</div>
                            <div class="kpi-value text-info" style="font-size: 5rem;">${costStr}</div>
                            <div class="mt-3 fs-3 text-secondary">Dari ${itemsCount} Transaksi / Pengambilan</div>
                        </div>
                    </div>
                </div>`;
        }

        function buildEwsTable() {
            // Ambil sparepart prioritas, lalu hitung rasio dan ambil 10 teratas
            let priorityData = (dataEWS || []).filter(i => i.Priority === 'Y');
            let list = priorityData.map(i => {
                let r = i.SafetyStock > 0 ? (i.CurrentStock / i.SafetyStock) * 100 : 100;
                return { ...i, ratio: r };
            }).sort((a,b) => a.ratio - b.ratio).slice(0, 10);

            if(list.length === 0) return `<div class="alert alert-info fs-3 text-center mt-5">Tidak ada data EWS untuk Sparepart Prioritas.</div>`;

            let trs = list.map(a => `
                <tr>
                    <td class="fw-bold fs-4">${a.Material}</td>
                    <td class="fs-4">${a.MaterialDescription}</td>
                    <td class="text-center fw-bold fs-4 text-danger">${a.CurrentStock}</td>
                    <td class="text-center fs-4">${a.SafetyStock}</td>
                    <td class="text-center fw-bold fs-4 ${a.ratio <= 100 ? 'text-danger' : 'text-success'}">${a.ratio.toFixed(1)}%</td>
                </tr>
            `).join('');

            return `<div class="table-responsive h-100 d-flex flex-column justify-content-center">
                        <table class="table table-striped table-bordered mb-0">
                            <thead>
                                <tr>
                                    <th class="w-15">Material ID</th>
                                    <th>Deskripsi Sparepart</th>
                                    <th class="text-center w-10">Stok<br>Aktual</th>
                                    <th class="text-center w-10">Min<br>Stok</th>
                                    <th class="text-center w-15">Rasio<br>Kesehatan</th>
                                </tr>
                            </thead>
                            <tbody>${trs}</tbody>
                        </table>
                    </div>`;
        }

        // Initialize TV Mode
        initData();

    