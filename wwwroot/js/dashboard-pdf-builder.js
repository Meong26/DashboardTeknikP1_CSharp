let modalReportBuilderInst = null;
    
    function openReportBuilder() {
        if(!modalReportBuilderInst) {
            modalReportBuilderInst = new bootstrap.Modal(document.getElementById('modalReportBuilder'));
        }
        const ddlBulan = document.getElementById("fltBulan");
        const ddlMinggu = document.getElementById("fltMinggu");
        let optionsHtml = ddlBulan.innerHTML;
        let weekHtml = ddlMinggu.innerHTML;
        
        ['RepTrend', 'RepPie', 'RepLine', 'RepMachine', 'RepMTTRBF', 'RepTopBadActor', 'RepActivity'].forEach(k => {
            const m = document.getElementById(`flt${k}Month`);
            const w = document.getElementById(`flt${k}Week`);
            if(m) m.innerHTML = optionsHtml;
            if(w) w.innerHTML = weekHtml;
        });
        
        modalReportBuilderInst.show();
    }

    function toggleRepFilter(key) {
        const isChecked = document.getElementById(`chk${key}`).checked;
        const wrapper = document.getElementById(`flt${key}Wrapper`);
        if(isChecked) wrapper.classList.remove('d-none');
        else wrapper.classList.add('d-none');
    }

    function updateRepFilterDdl(key) {
        const mode = document.getElementById(`flt${key}Mode`).value;
        const ddlMonth = document.getElementById(`flt${key}Month`);
        const ddlWeek = document.getElementById(`flt${key}Week`);
        if(ddlMonth) ddlMonth.classList.add('d-none');
        if(ddlWeek) ddlWeek.classList.add('d-none');
        
        if(mode === 'MONTH' && ddlMonth) {
            ddlMonth.classList.remove('d-none');
        } else if (mode === 'WEEK' && ddlWeek) {
            ddlWeek.classList.remove('d-none');
        }
    }

    // Helper to get active filter text for subtitle
    function getFilterSubtitle(mode, timeVal) {
        let subtitle = `Filter Data: `;
        if (mode === 'SYNC') {
            const dbMode = document.getElementById("dimMode").value;
            const selMonth = document.getElementById("fltBulan").value;
            const selWeek = document.getElementById("fltMinggu").value;
            if (dbMode === 'Year') subtitle += `Semua Tahun`;
            if (dbMode === 'Month') subtitle += selMonth === 'ALL' ? `Semua Bulan` : `Bulan ${selMonth}`;
            if (dbMode === 'Week') subtitle += `Minggu Ke-${selWeek}`;
        } else if (mode === 'MONTH') {
            subtitle += timeVal === 'ALL' ? `Semua Bulan` : `Bulan ${timeVal}`;
        } else if (mode === 'WEEK') {
            subtitle += timeVal === 'ALL' ? `Semua Minggu` : `Minggu ${timeVal}`;
        } else if (mode === 'ALL') {
            subtitle += `Semua Waktu (All Time)`;
        }
        
        let notif = document.getElementById("fltNotif").options[document.getElementById("fltNotif").selectedIndex].text;
        let shift = document.getElementById("fltShift").options[document.getElementById("fltShift").selectedIndex].text;
        let steam = document.getElementById("chkSteam").checked ? 'Termasuk Steam' : 'Tanpa Steam';
        
        subtitle += ` | Notifikasi: ${notif} | Regu: ${shift} | Boiler: ${steam}`;
        return subtitle;
    }

    function appendImageToPDFContainer(container, title, dataUrl, subtitle = '') {
        const div = document.createElement('div');
        div.style.marginBottom = '20px';
        let html = `<h3 style="font-size: 16px; border-left: 4px solid #dc3545; padding-left: 8px; margin-bottom: 5px;">${title}</h3>`;
        if (subtitle) {
            html += `<p style="font-size: 10px; color: #666; margin-top: 0; margin-bottom: 10px; font-style: italic;">${subtitle}</p>`;
        }
        html += `<img src="${dataUrl}" style="width: 100%; height: auto; display: block; border: 1px solid #eee;">`;
        div.innerHTML = html;
        container.appendChild(div);
    }

    async function generatePDF() {
        modalReportBuilderInst.hide();
        document.getElementById('pdf-loading-overlay').classList.remove('d-none');
        document.getElementById('pdf-loading-overlay').classList.add('d-flex');
        
        // Setup timestamp
        const now = new Date();
        const strDate = now.toLocaleDateString('id-ID', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' });
        const strTime = now.toLocaleTimeString('id-ID');
        document.getElementById('pdf-timestamp').innerHTML = `<strong>Dicetak pada:</strong> ${strDate} pukul ${strTime}`;
        
        const container = document.getElementById('pdf-content-area');
        container.innerHTML = ''; // Clear previous

        try {
            // 1. CHART UTAMA (Wajib)
            if(barChartInst) {
                const mainChartDataUrl = await generateOfflineCurrentStateChart();
                if (mainChartDataUrl) {
                    appendImageToPDFContainer(container, 'Grafik Downtime vs Jam Terencana (State Saat Ini)', mainChartDataUrl, getFilterSubtitle('SYNC', 'ALL'));
                }
            }
            
            const getTimeVal = (k, m) => m === 'MONTH' ? document.getElementById(`flt${k}Month`).value : (m === 'WEEK' ? document.getElementById(`flt${k}Week`).value : 'ALL');

            // 2. TREND CHART (Sekunder)
            if(document.getElementById('chkRepTrend').checked) {
                const mode = document.getElementById('fltRepTrendMode').value;
                const timeVal = getTimeVal('RepTrend', mode);
                const trendDataUrl = await generateOfflineTrendChart(mode, timeVal);
                appendImageToPDFContainer(container, 'Grafik Analitik Sekunder (Run Chart)', trendDataUrl, getFilterSubtitle(mode, timeVal));
            }
            
            // 2.5. BAR GAUGE PENCAPAIAN DT NT (KHUSUS MINGGU INI)
            if(document.getElementById('chkRepDTAchievement') && document.getElementById('chkRepDTAchievement').checked) {
                const html = generateOfflineDTAchievement();
                const div = document.createElement('div');
                div.style.marginBottom = '20px';
                div.innerHTML = `<h3 style="font-size: 16px; border-left: 4px solid #dc3545; padding-left: 8px; margin-bottom: 5px;">Pencapaian DT NT</h3>
                                 <p style="font-size: 10px; color: #666; margin-top: 0; margin-bottom: 10px; font-style: italic;">Khusus Minggu Berjalan (Filter Diabaikan)</p>
                                 ${html}`;
                container.appendChild(div);
            }
            
            // 3. PIE CHART
            if(document.getElementById('chkRepPie').checked) {
                const mode = document.getElementById('fltRepPieMode').value;
                const timeVal = getTimeVal('RepPie', mode);
                const pieDataUrl = await generateOfflinePieChart(mode, timeVal);
                appendImageToPDFContainer(container, 'Proporsi Downtime per Regu (Menit)', pieDataUrl, getFilterSubtitle(mode, timeVal));
            }

            // 4. BAR CHART BY LINE
            if(document.getElementById('chkRepLine').checked) {
                const mode = document.getElementById('fltRepLineMode').value;
                const timeVal = getTimeVal('RepLine', mode);
                const lineDataUrl = await generateOfflineBarChart(mode, timeVal, 'LINE');
                appendImageToPDFContainer(container, 'Grafik Batang Downtime by Line', lineDataUrl, getFilterSubtitle(mode, timeVal));
            }

            // 5. BAR CHART BY MACHINE
            if(document.getElementById('chkRepMachine').checked) {
                const mode = document.getElementById('fltRepMachineMode').value;
                const timeVal = getTimeVal('RepMachine', mode);
                const machineDataUrl = await generateOfflineBarChart(mode, timeVal, 'MACHINE');
                appendImageToPDFContainer(container, 'Grafik Batang Downtime by Mesin', machineDataUrl, getFilterSubtitle(mode, timeVal));
            }

            // 6. MTTR, MTBF, AVAILABILITY
            if(document.getElementById('chkRepMTTRBF').checked) {
                const mode = document.getElementById('fltRepMTTRBFMode').value;
                const timeVal = getTimeVal('RepMTTRBF', mode);
                const html = generateOfflineMTTRBF(mode, timeVal);
                
                const div = document.createElement('div');
                div.style.marginBottom = '20px';
                div.innerHTML = `<h3 style="font-size: 16px; border-left: 4px solid #dc3545; padding-left: 8px; margin-bottom: 5px;">Kesimpulan Kehandalan Sistem</h3>
                                 <p style="font-size: 10px; color: #666; margin-top: 0; margin-bottom: 10px; font-style: italic;">${getFilterSubtitle(mode, timeVal)}</p>
                                 ${html}`;
                container.appendChild(div);
            }
            
            // 7. TOP 5 BAD ACTORS
            if(document.getElementById('chkRepTopBadActor').checked) {
                const mode = document.getElementById('fltRepTopBadActorMode').value;
                const timeVal = getTimeVal('RepTopBadActor', mode);
                const html = generateOfflineTopBadActors(mode, timeVal);
                const div = document.createElement('div');
                div.style.marginBottom = '20px';
                div.innerHTML = `<h3 style="font-size: 16px; border-left: 4px solid #dc3545; padding-left: 8px; margin-bottom: 5px;">Top 5 Bad Actor (Downtime Tertinggi)</h3>
                                 <p style="font-size: 10px; color: #666; margin-top: 0; margin-bottom: 10px; font-style: italic;">${getFilterSubtitle(mode, timeVal)}</p>
                                 ${html}`;
                container.appendChild(div);
            }


            // 7.5. TABEL RINCIAN AKTIFITAS
            if(document.getElementById('chkRepActivity').checked) {
                const mode = document.getElementById('fltRepActivityMode').value;
                const timeVal = getTimeVal('RepActivity', mode);
                const offlineData = filterDataForReport(mode, timeVal);
                
                // Urutkan data dari yang terlama ke terbaru (Ascending)
                let activities = [...offlineData.fYP].sort((a, b) => new Date(a.NotificationDate) - new Date(b.NotificationDate));

                if (activities.length === 0) {
                    const div = document.createElement('div');
                    div.style.marginBottom = '20px';
                    div.innerHTML = `<h3 style="font-size: 16px; border-left: 4px solid #dc3545; padding-left: 8px; margin-bottom: 5px;">Rincian Aktifitas Downtime</h3>
                                     <p style="font-size: 10px; color: #666; margin-top: 0; margin-bottom: 10px; font-style: italic;">${getFilterSubtitle(mode, timeVal)}</p>
                                     <table style="width: 100%; border-collapse: collapse; font-size: 10px;"><tr><td style="text-align:center; padding:10px; border: 1px solid #ddd;">Tidak ada aktifitas pada periode ini.</td></tr></table>`;
                    container.appendChild(div);
                } else {
                    const CHUNK_SIZE = 35; // Bagi tabel menjadi beberapa bagian agar tidak terpotong saat ganti halaman PDF
                    for (let i = 0; i < activities.length; i += CHUNK_SIZE) {
                        const chunk = activities.slice(i, i + CHUNK_SIZE);
                        let tableRows = '';
                        chunk.forEach((item, idx) => {
                            const fDate = new Date(item.NotificationDate).toLocaleDateString('id-ID', { day: '2-digit', month: 'short' });
                            let txtAktivitas = item.ActivityText ? item.ActivityText : (item.NotificationDesc || '-');
                            let namaLine = getLineName(item.FunctionLocation);
                            let namaMesin = getMachineName(item.FunctionLocation, item.ActivityText);
                            tableRows += `<tr>
                                <td style="border: 1px solid #ddd; padding: 4px; text-align: center;">${i + idx + 1}</td>
                                <td style="border: 1px solid #ddd; padding: 4px; text-align: center;">${fDate}</td>
                                <td style="border: 1px solid #ddd; padding: 4px; color: #198754; font-weight: bold;">${namaLine}</td>
                                <td style="border: 1px solid #ddd; padding: 4px; color: #0d6efd; font-weight: bold;">${namaMesin}</td>
                                <td style="border: 1px solid #ddd; padding: 4px;">${txtAktivitas}</td>
                                <td style="border: 1px solid #ddd; padding: 4px; text-align: center; color: #dc3545; font-weight: bold;">${item.TotalDownTimeInMinutes}</td>
                                <td style="border: 1px solid #ddd; padding: 4px; text-align: center;">${item.WageGroup_GroupShift || '-'}</td>
                            </tr>`;
                        });
                        
                        let html = `<table style="width: 100%; border-collapse: collapse; font-size: 10px; margin-bottom: 20px;">
                            <thead style="background-color: #343a40; color: white;">
                                <tr>
                                    <th style="padding: 6px; border: 1px solid #ddd; width: 5%;">No</th>
                                    <th style="padding: 6px; border: 1px solid #ddd; width: 10%;">Tanggal</th>
                                    <th style="padding: 6px; border: 1px solid #ddd; width: 15%;">Line</th>
                                    <th style="padding: 6px; border: 1px solid #ddd; width: 15%;">Mesin</th>
                                    <th style="padding: 6px; border: 1px solid #ddd; width: 35%;">Aktivitas Perbaikan</th>
                                    <th style="padding: 6px; border: 1px solid #ddd; width: 10%;">Menit</th>
                                    <th style="padding: 6px; border: 1px solid #ddd; width: 10%;">Regu</th>
                                </tr>
                            </thead>
                            <tbody>${tableRows}</tbody>
                        </table>`;

                        const div = document.createElement('div');
                        div.style.marginBottom = '20px';
                        if (i === 0) {
                            div.innerHTML = `<h3 style="font-size: 16px; border-left: 4px solid #dc3545; padding-left: 8px; margin-bottom: 5px;">Rincian Aktifitas Downtime</h3>
                                             <p style="font-size: 10px; color: #666; margin-top: 0; margin-bottom: 10px; font-style: italic;">${getFilterSubtitle(mode, timeVal)}</p>
                                             ${html}`;
                        } else {
                            div.innerHTML = `<p style="font-size: 10px; color: #666; margin-top: 0; margin-bottom: 5px; font-style: italic;">(Lanjutan) Rincian Aktifitas Downtime</p>${html}`;
                        }
                        container.appendChild(div);
                    }
                }
            }

            // 8. TEMUAN OPEN (FETCH API)
            if(document.getElementById('chkRepOpenFindings').checked) {
                try {
                    const response = await fetch('/Temuan/GetApiData');
                    const apiData = await response.json();
                    let openFindings = apiData.history.filter(t => t.Status === "OPEN");
                    
                    let tableRows = '';
                    if (openFindings.length === 0) {
                        tableRows = '<tr><td colspan="6" style="text-align:center; padding:10px;">Tidak ada temuan OPEN.</td></tr>';
                    } else {
                        openFindings.forEach((item, idx) => {
                            tableRows += `<tr>
                                <td style="border: 1px solid #ddd; padding: 6px; text-align: center;">${idx + 1}</td>
                                <td style="border: 1px solid #ddd; padding: 6px;">${item.TanggalFormated}</td>
                                <td style="border: 1px solid #ddd; padding: 6px;">${item.Line} - ${item.NamaMesin}</td>
                                <td style="border: 1px solid #ddd; padding: 6px;">${item.DeskripsiAbnormal}</td>
                                <td style="border: 1px solid #ddd; padding: 6px;">${item.Pelapor}</td>
                                <td style="border: 1px solid #ddd; padding: 6px; color: red; font-weight: bold; text-align: center;">OPEN</td>
                            </tr>`;
                        });
                    }
                    
                    let html = `<table style="width: 100%; border-collapse: collapse; font-size: 11px;">
                        <thead style="background-color: #343a40; color: white;">
                            <tr>
                                <th style="padding: 6px; border: 1px solid #ddd; width: 5%;">No</th>
                                <th style="padding: 6px; border: 1px solid #ddd; width: 15%;">Tanggal</th>
                                <th style="padding: 6px; border: 1px solid #ddd; width: 25%;">Mesin</th>
                                <th style="padding: 6px; border: 1px solid #ddd; width: 35%;">Deskripsi</th>
                                <th style="padding: 6px; border: 1px solid #ddd; width: 10%;">Pelapor</th>
                                <th style="padding: 6px; border: 1px solid #ddd; width: 10%;">Status</th>
                            </tr>
                        </thead>
                        <tbody>${tableRows}</tbody>
                    </table>`;

                    const div = document.createElement('div');
                    div.style.marginBottom = '20px';
                    div.innerHTML = `<h3 style="font-size: 16px; border-left: 4px solid #dc3545; padding-left: 8px; margin-bottom: 5px;">Daftar Temuan (Status OPEN)</h3>
                                     <p style="font-size: 10px; color: #666; margin-top: 0; margin-bottom: 10px; font-style: italic;">Diambil secara real-time dari database temuan</p>
                                     ${html}`;
                    container.appendChild(div);
                } catch (err) {
                    console.error("Gagal menarik temuan open:", err);
                }
            }

            // Give the browser a tick to render the DOM inside the absolute container
            await new Promise(r => setTimeout(r, 800)); // slightly longer wait to ensure all elements render
            
            // Execute html2canvas block by block with slicing
            const { jsPDF } = window.jspdf;
            const pdf = new jsPDF('p', 'mm', 'a4');
            const pdfWidth = pdf.internal.pageSize.getWidth();
            const pageHeight = pdf.internal.pageSize.getHeight();
            const marginY = 15; 
            const marginX = 10; 
            const imgWidth = pdfWidth - (2 * marginX);
            
            let currentY = marginY;

            // Render Header
            const headerEl = document.getElementById('pdf-header');
            if(headerEl) {
                const headerCanvas = await html2canvas(headerEl, { scale: 2, useCORS: true });
                let headerData = headerCanvas.toDataURL('image/png');
                let headerHeight = (headerCanvas.height * imgWidth) / headerCanvas.width;
                pdf.addImage(headerData, 'PNG', marginX, currentY, imgWidth, headerHeight);
                currentY += headerHeight + 5; 
            }

            // Helper function to handle slicing
            const addCanvasToPdfWithSlicing = (childCanvas) => {
                let remainingHeight = childCanvas.height;
                let currentCanvasY = 0;
                
                // Calculate PDF height for full canvas
                let fullPdfHeight = (childCanvas.height * imgWidth) / childCanvas.width;

                // Move to next page if it doesn't fit and it's small enough to fit on a full page, 
                // OR if current page is almost full
                if (currentY + fullPdfHeight > pageHeight - marginY && fullPdfHeight < (pageHeight - 2 * marginY)) {
                    pdf.addPage();
                    currentY = marginY;
                } else if (currentY > pageHeight - marginY - 20) { 
                    // Too close to bottom, just break
                    pdf.addPage();
                    currentY = marginY;
                }
                
                while (remainingHeight > 0) {
                    let pdfSpaceAvailable = pageHeight - marginY - currentY;
                    let canvasSpaceAvailable = (pdfSpaceAvailable * childCanvas.width) / imgWidth;
                    
                    let sliceHeight = Math.min(remainingHeight, canvasSpaceAvailable);
                    let pdfSliceHeight = (sliceHeight * imgWidth) / childCanvas.width;
                    
                    if (sliceHeight <= 0) {
                        pdf.addPage();
                        currentY = marginY;
                        continue;
                    }

                    // Create slice canvas
                    let sliceCanvas = document.createElement('canvas');
                    sliceCanvas.width = childCanvas.width;
                    sliceCanvas.height = sliceHeight;
                    let sliceCtx = sliceCanvas.getContext('2d');
                    sliceCtx.drawImage(childCanvas, 0, currentCanvasY, childCanvas.width, sliceHeight, 0, 0, childCanvas.width, sliceHeight);
                    
                    pdf.addImage(sliceCanvas.toDataURL('image/png'), 'PNG', marginX, currentY, imgWidth, pdfSliceHeight);
                    
                    remainingHeight -= sliceHeight;
                    currentCanvasY += sliceHeight;
                    
                    if (remainingHeight > 0.5) { // prevent infinite loop on float math
                        pdf.addPage();
                        currentY = marginY;
                    } else {
                        currentY += pdfSliceHeight + 5;
                    }
                }
            };

            // Render each component iteratively
            const contentArea = document.getElementById('pdf-content-area');
            const children = Array.from(contentArea.children);
            for(let i = 0; i < children.length; i++) {
                const child = children[i];
                const childCanvas = await html2canvas(child, { scale: 2, useCORS: true, backgroundColor: '#ffffff' });
                addCanvasToPdfWithSlicing(childCanvas);
            }

            // Render Footer
            const footerEl = document.getElementById('pdf-footer');
            if(footerEl) {
                const footerCanvas = await html2canvas(footerEl, { scale: 2, useCORS: true });
                let footerHeight = (footerCanvas.height * imgWidth) / footerCanvas.width;
                if(currentY + footerHeight > pageHeight - marginY) {
                    pdf.addPage();
                    currentY = marginY;
                }
                pdf.addImage(footerCanvas.toDataURL('image/png'), 'PNG', marginX, currentY, imgWidth, footerHeight);
            }

            pdf.save(`Summary_Analitik_${new Date().getTime()}.pdf`);
            
        } catch (error) {
            console.error(error);
            alert("Terjadi kesalahan saat menghasilkan PDF.");
        } finally {
            document.getElementById('pdf-loading-overlay').classList.add('d-none');
            document.getElementById('pdf-loading-overlay').classList.remove('d-flex');
        }
    }

    function generateOfflineDTAchievement() {
        // Karena ini murni "Minggu Ini", kita tentukan string minggu dari data terbaru YP
        let maxDate = new Date('2000-01-01');
        let currentWeekStr = "";
        (ypDataRaw || []).filter(item => item.NotificationType === 'NT').forEach(i => { 
            let d = new Date(i.NotificationDate); 
            if (d > maxDate) { maxDate = d; currentWeekStr = i.WeekKalendarIndofood; } 
        });

        if (!currentWeekStr) currentWeekStr = ""; 

        let fYP = (ypDataRaw || []).filter(item => item.WeekKalendarIndofood === currentWeekStr && item.NotificationType === 'NT');
        let fYR = (yrDataRaw || []).filter(item => item.WeekOfBasicFinishedDate === currentWeekStr);

        let dtMins = fYP.reduce((sum, item) => sum + item.TotalDownTimeInMinutes, 0);
        let plannedMins = fYR.reduce((sum, item) => sum + (item.PlannedHour * 60), 0);
        
        let percentage = 0;
        if (plannedMins > 0) percentage = (dtMins / plannedMins) * 100;
        
        const target = window.AppConfig.TargetDowntime || 1.5;
        let scaledWidth = (percentage / (target * 2)) * 100;
        if (scaledWidth > 100) scaledWidth = 100;

        let colorCode = '#dc3545'; // bg-danger
        let textCode = '#dc3545';
        if (percentage <= 1.2) { colorCode = '#198754'; textCode = '#198754'; }
        else if (percentage <= 1.5) { colorCode = '#ffc107'; textCode = '#ffc107'; }

        return `
            <div style="background: white; border: 1px solid #ddd; border-radius: 5px; padding: 20px; text-align: center; margin-bottom: 20px;">
                <h1 style="font-size: 32px; font-weight: bold; color: ${textCode}; margin-bottom: 15px;">${percentage.toFixed(2)} %</h1>
                
                <div style="background: #e9ecef; height: 30px; border-radius: 15px; overflow: hidden; margin-bottom: 20px; position: relative;">
                    <div style="height: 100%; width: ${scaledWidth}%; background-color: ${colorCode};"></div>
                </div>
                
                <div style="display: flex; justify-content: center; gap: 40px;">
                    <div style="background: #f8f9fa; border: 1px solid #ccc; padding: 15px 30px; border-radius: 5px;">
                        <span style="font-size: 12px; color: #666; font-weight: bold; display: block; text-transform: uppercase;">Total DT NT</span>
                        <span style="font-size: 24px; font-weight: bold; color: #dc3545;">${dtMins.toFixed(0)} Menit</span>
                    </div>
                    <div style="background: #f8f9fa; border: 1px solid #ccc; padding: 15px 30px; border-radius: 5px;">
                        <span style="font-size: 12px; color: #666; font-weight: bold; display: block; text-transform: uppercase;">Total Jam Terencana</span>
                        <span style="font-size: 24px; font-weight: bold; color: #0d6efd;">${plannedMins.toFixed(0)} Menit</span>
                    </div>
                </div>
            </div>
        `;
    }

    async function generateOfflineCurrentStateChart() {
        if (!barChartInst) return null;

        const canvas = document.getElementById('barChart');
        const container = canvas.parentElement;
        
        // Simpan dimensi asli
        const origWidth = container.style.width;
        const origHeight = container.style.height;
        const origMinWidth = container.style.minWidth;
        const origMinHeight = container.style.minHeight;
        
        // Paksa wadah grafik berukuran persis 700x300 piksel
        container.style.minWidth = '700px';
        container.style.width = '700px';
        container.style.minHeight = '300px';
        container.style.height = '300px';
        
        // Perintahkan Chart.js menyesuaikan ulang ke ukuran baru
        barChartInst.resize();
        
        // Beri waktu 300ms agar proses render selesai sempurna
        await new Promise(r => setTimeout(r, 300));
        
        // Ambil gambar tajam dari grafik
        const dataUrl = barChartInst.toBase64Image();
        
        // Kembalikan ke ukuran layar responsif HP/Desktop semula
        container.style.minWidth = origMinWidth;
        container.style.width = origWidth || '100%';
        container.style.minHeight = origMinHeight;
        container.style.height = origHeight || '35vh';
        barChartInst.resize();
        
        return dataUrl;
    }

    function filterDataForReport(mode, timeVal) {
        let isTimeMatch = () => true;
        
        if (mode === 'SYNC') {
            const dbMode = document.getElementById("dimMode").value;
            const selMonth = document.getElementById("fltBulan").value;
            const selWeek = document.getElementById("fltMinggu").value;
            isTimeMatch = (dateObj, weekStr) => {
                if (dbMode === "Month") {
                    if (selMonth === "ALL") return true; 
                    return `${dateObj.getFullYear()}-${String(dateObj.getMonth() + 1).padStart(2, '0')}` === selMonth;
                }
                if (dbMode === "Week") return weekStr === selWeek;
                return true;
            };
        } else if (mode === 'MONTH' && timeVal !== 'ALL') {
            isTimeMatch = (dateObj) => {
                 return `${dateObj.getFullYear()}-${String(dateObj.getMonth() + 1).padStart(2, '0')}` === timeVal;
            };
        } else if (mode === 'WEEK' && timeVal !== 'ALL') {
            isTimeMatch = (dateObj, weekStr) => {
                 return weekStr === timeVal;
            };
        }

        const fltNotif = document.getElementById("fltNotif").value;
        const fltShift = document.getElementById("fltShift").value;
        const isSteamIncluded = document.getElementById("chkSteam").checked;

        let filteredYP = ypDataRaw.filter(item => {
            let mNotif = !fltNotif || (item.NotificationType && item.NotificationType.includes(fltNotif));
            let mShift = !fltShift || (item.WageGroup_GroupShift && item.WageGroup_GroupShift.includes(fltShift));
            let mTime = isTimeMatch(new Date(item.NotificationDate), item.WeekKalendarIndofood);
            let mBoiler = !(getMachineName(item.FunctionLocation, item.ActivityText) === "Boiler" && !isSteamIncluded);
            return mNotif && mShift && mTime && mBoiler;
        });

        let filteredYR = yrDataRaw.filter(item => {
            if (fltShift && (!item.WageGroup || !item.WageGroup.includes(fltShift))) return false;
            if (!isTimeMatch(new Date(item.PostingDate), item.WeekOfBasicFinishedDate)) return false;
            return true;
        });

        return { fYP: filteredYP, fYR: filteredYR };
    }

    async function generateOfflineTrendChart(mode, timeVal) {
        const { fYP, fYR } = filterDataForReport(mode, timeVal);
        let groupMode = 'Year'; // Default
        if (mode === 'SYNC') {
            groupMode = document.getElementById("dimMode").value;
        } else if (mode === 'MONTH') {
            groupMode = 'Month';
        } else if (mode === 'WEEK') {
            groupMode = 'Week';
        }

        let ypAgg = {}, yrAgg = {};
        fYP.forEach(item => {
            let key = getGroupingKey(new Date(item.NotificationDate), groupMode, item.WeekKalendarIndofood);
            if (!ypAgg[key]) ypAgg[key] = { mins: 0, items: [] };
            ypAgg[key].mins += item.TotalDownTimeInMinutes;
            ypAgg[key].items.push(item);
        });
        
        fYR.forEach(item => {
            let key = getGroupingKey(new Date(item.PostingDate), groupMode, item.WeekOfBasicFinishedDate);
            if (!yrAgg[key]) yrAgg[key] = 0;
            yrAgg[key] += (item.PlannedHour * 60);
        });

        let rawKeys = Object.keys(ypAgg);
        rawKeys.sort((a, b) => {
            if (groupMode === "Year") return monthNames.indexOf(a) - monthNames.indexOf(b);
            if (groupMode === "Month") {
                let numA = parseInt(a.replace(/\D/g, '')) || 0;
                let numB = parseInt(b.replace(/\D/g, '')) || 0;
                return numA - numB;
            }
            return a.localeCompare(b);
        });

        let labels = [], percentData = [], minuteData = [], targetLine = [];
        rawKeys.forEach(k => {
            labels.push(formatLabel(k, groupMode));
            let dtMins = ypAgg[k].mins;            
            let plannedMins = yrAgg[k] || 0;       
            minuteData.push(dtMins);
            percentData.push(plannedMins > 0 ? ((dtMins / plannedMins) * 100).toFixed(2) : 0);
            targetLine.push(window.AppConfig.TargetDowntime);
        });

        const canvas = document.createElement('canvas');
        // Ukuran logis (tampilan)
        canvas.style.width = '700px';
        canvas.style.height = '300px';
        // Set ukuran asli (digunakan oleh chart.js jika tidak ada dPR, tapi kita akan force dPR)
        canvas.width = 700;
        canvas.height = 300;
        canvas.style.position = 'absolute';
        canvas.style.left = '-9999px';
        document.body.appendChild(canvas);

        let tChart = new Chart(canvas.getContext('2d'), {
            type: 'line',
            data: { 
                labels: labels, 
                datasets: [
                    { type: 'line', label: 'Standar (' + window.AppConfig.TargetDowntime + '%)', data: targetLine, borderColor: 'rgba(220, 53, 69, 1)', borderWidth: 2, borderDash: [5, 5], pointRadius: 0, fill: false, datalabels: { display: false } },
                    { 
                        type: 'line', label: 'Downtime (%)', data: percentData, backgroundColor: 'rgba(54, 162, 235, 0.2)', borderColor: 'rgba(54, 162, 235, 1)', borderWidth: 2, 
                        pointBackgroundColor: 'rgba(54, 162, 235, 1)', pointBorderColor: '#fff', pointBorderWidth: 1, pointRadius: 4, fill: true, tension: 0.1, customMinutes: minuteData 
                    }
                ]
            },
            options: {
                devicePixelRatio: 3, // Resolusi 3x lipat lebih tajam
                animation: false, responsive: false,
                plugins: {
                    legend: { position: 'bottom', labels: { boxWidth: 12 } },
                    datalabels: { 
                        anchor: 'end', align: 'top', offset: 4, color: '#000', textAlign: 'center',
                        font: function(context) {
                            let count = context.chart.data.labels.length;
                            return { weight: 'bold', size: count > 15 ? 8 : 10 };
                        },
                        display: function(context) {
                            return !context.dataset.label.includes('Standar');
                        },
                        formatter: function(v, ctx) { return v == 0 ? null : `${v}% \n(${ctx.dataset.customMinutes[ctx.dataIndex]} m)`; } 
                    }
                },
                scales: { 
                    y: { type: 'linear', display: true, position: 'left', title: { display: true, text: 'Persentase Downtime (%)' }, beginAtZero: true, suggestedMax: 3 }
                }
            }
        });

        await new Promise(r => setTimeout(r, 100));
        let dataUrl = tChart.toBase64Image();
        tChart.destroy();
        canvas.remove();
        return dataUrl;
    }

    async function generateOfflinePieChart(mode, selectedMonth) {
        const { fYP } = filterDataForReport(mode, selectedMonth);
        
        let shiftMap = { 'A': 0, 'B': 0, 'C': 0, 'Lainnya': 0 };
        fYP.forEach(item => {
            let s = item.WageGroup_GroupShift;
            if(!s) s = 'Lainnya';
            else if(s.includes('A')) s = 'A';
            else if(s.includes('B')) s = 'B';
            else if(s.includes('C')) s = 'C';
            else s = 'Lainnya';
            shiftMap[s] += parseFloat(item.TotalDownTimeInMinutes || 0);
        });

        const canvas = document.createElement('canvas');
        canvas.style.width = '600px';
        canvas.style.height = '300px';
        canvas.width = 600;
        canvas.height = 300;
        canvas.style.position = 'absolute';
        canvas.style.left = '-9999px';
        document.body.appendChild(canvas);

        const ctx = canvas.getContext('2d');
        const labels = ['Regu A', 'Regu B', 'Regu C', 'Lainnya'];
        const values = [shiftMap['A'], shiftMap['B'], shiftMap['C'], shiftMap['Lainnya']];
        const colors = ['#dc3545', '#ffc107', '#0d6efd', '#6c757d'];

        let tChart = new Chart(ctx, {
            type: 'pie',
            data: { labels, datasets: [{ data: values.map(v => +(v).toFixed(0)), backgroundColor: colors }] },
            options: {
                devicePixelRatio: 3, // Resolusi 3x lipat
                animation: false,
                responsive: false,
                plugins: {
                    legend: { position: 'right' },
                    datalabels: { color: '#fff', font: {weight: 'bold'}, formatter: (v, ctx) => {
                        if (v === 0) return null; // Hilangkan label jika 0
                        let sum = ctx.chart.data.datasets[0].data.reduce((a, b) => a + b, 0);
                        let percentage = sum > 0 ? (v * 100 / sum).toFixed(1) + "%" : "";
                        return `${v} m\n(${percentage})`;
                    }}
                }
            }
        });

        await new Promise(r => setTimeout(r, 100));
        let dataUrl = tChart.toBase64Image();
        tChart.destroy();
        canvas.remove();
        return dataUrl;
    }

    async function generateOfflineBarChart(mode, selectedMonth, groupBy) {
        const { fYP } = filterDataForReport(mode, selectedMonth);
        
        let aggMap = {};
        fYP.forEach(item => {
            let key = groupBy === 'LINE' ? getLineName(item.FunctionLocation) : getMachineName(item.FunctionLocation, item.ActivityText);
            if(!aggMap[key]) aggMap[key] = 0;
            aggMap[key] += parseFloat(item.TotalDownTimeInMinutes || 0);
        });

        let sortedKeys = Object.keys(aggMap).sort((a,b) => aggMap[b] - aggMap[a]);
        let values = sortedKeys.map(k => aggMap[k]);

        const canvas = document.createElement('canvas');
        canvas.style.width = '700px';
        canvas.style.height = groupBy === 'MACHINE' ? '350px' : '300px';
        canvas.width = 700;
        canvas.height = groupBy === 'MACHINE' ? 350 : 300;
        canvas.style.position = 'absolute';
        canvas.style.left = '-9999px';
        document.body.appendChild(canvas);

        const ctx = canvas.getContext('2d');
        const color = groupBy === 'LINE' ? 'rgba(54, 162, 235, 0.8)' : 'rgba(255, 99, 132, 0.8)';

        let tChart = new Chart(ctx, {
            type: 'bar',
            data: { labels: sortedKeys, datasets: [{ label: 'Total Downtime (Menit)', data: values.map(v => +(v).toFixed(0)), backgroundColor: color }] },
            options: {
                devicePixelRatio: 3, // Resolusi 3x lipat
                animation: false,
                responsive: false,
                plugins: {
                    legend: { display: false },
                    datalabels: { color: '#000', anchor: 'end', align: 'top', font: {weight: 'bold'}, formatter: v => v > 0 ? v + 'm' : null }
                },
                scales: { 
                    y: { beginAtZero: true, suggestedMax: values.length > 0 ? Math.max(...values) * 1.2 : 10, title: { display: true, text: 'Total Downtime (Menit)' } },
                    x: { ticks: { autoSkip: false, maxRotation: 45, minRotation: 45, font: {size: 10} } }
                }
            }
        });

        await new Promise(r => setTimeout(r, 100));
        let dataUrl = tChart.toBase64Image();
        tChart.destroy();
        canvas.remove();
        return dataUrl;
    }

    function generateOfflineMTTRBF(mode, selectedMonth) {
        const { fYP, fYR } = filterDataForReport(mode, selectedMonth);
        let totalDowntime = 0, breakCount = 0;
        fYP.forEach(item => {
            totalDowntime += parseFloat(item.TotalDownTimeInMinutes || 0);
            breakCount++;
        });
        
        let totalPlanned = 0;
        fYR.forEach(item => {
            totalPlanned += parseFloat(item.PlannedHour || 0) * 60;
        });

        let mttr = breakCount > 0 ? (totalDowntime / breakCount) : 0;
        let uptime = totalPlanned - totalDowntime;
        let mtbf = breakCount > 0 ? (uptime / breakCount) : (uptime > 0 ? uptime : 0);
        
        let avail = 0;
        if ((mtbf + mttr) > 0) {
            avail = (mtbf / (mtbf + mttr)) * 100;
        }
        
        return `
            <div style="display: flex; justify-content: space-around; background: #f8f9fa; padding: 15px; border-radius: 5px;">
                <div style="text-align: center; width: 25%;">
                    <div style="font-size: 11px; color: #666; font-weight: bold;">TOTAL DOWNTIME</div>
                    <div style="font-size: 22px; color: #dc3545; font-weight: bold;">${totalDowntime.toFixed(0)} Min</div>
                </div>
                <div style="text-align: center; width: 25%; border-left: 1px solid #ccc;">
                    <div style="font-size: 11px; color: #666; font-weight: bold;">MEAN TIME TO REPAIR (MTTR)</div>
                    <div style="font-size: 22px; color: #dc3545; font-weight: bold;">${mttr.toFixed(1)} Min</div>
                </div>
                <div style="text-align: center; width: 25%; border-left: 1px solid #ccc;">
                    <div style="font-size: 11px; color: #666; font-weight: bold;">MEAN TIME BETWEEN FAILURES (MTBF)</div>
                    <div style="font-size: 22px; color: #198754; font-weight: bold;">${mtbf.toFixed(1)} Min</div>
                </div>
                <div style="text-align: center; width: 25%; border-left: 1px solid #ccc;">
                    <div style="font-size: 11px; color: #666; font-weight: bold;">MACHINE AVAILABILITY</div>
                    <div style="font-size: 22px; color: #0d6efd; font-weight: bold;">${avail.toFixed(2)} %</div>
                </div>
            </div>
        `;
    }

    function generateOfflineTopBadActors(mode, selectedMonth) {
        const { fYP } = filterDataForReport(mode, selectedMonth);
        let machineMap = {};
        fYP.forEach(item => {
            let line = getLineName(item.FunctionLocation);
            let machine = getMachineName(item.FunctionLocation, item.ActivityText);
            let nameKey = `${line} - ${machine}`;
            if (!machineMap[nameKey]) machineMap[nameKey] = 0;
            machineMap[nameKey] += item.TotalDownTimeInMinutes;
        });

        let sortedActors = Object.keys(machineMap).map(k => ({ name: k, mins: machineMap[k] })).sort((a,b) => b.mins - a.mins).slice(0, 5);
        
        let tableRows = '';
        if (sortedActors.length === 0) {
            tableRows = '<tr><td colspan="3" style="text-align:center; padding:10px;">Tidak ada data kerusakan.</td></tr>';
        } else {
            sortedActors.forEach((item, idx) => {
                tableRows += `<tr>
                    <td style="border: 1px solid #ddd; padding: 6px; text-align: center; font-weight: bold;">${idx + 1}</td>
                    <td style="border: 1px solid #ddd; padding: 6px;">${item.name}</td>
                    <td style="border: 1px solid #ddd; padding: 6px; color: #dc3545; font-weight: bold; text-align: right;">${item.mins} Menit</td>
                </tr>`;
            });
        }
        
        return `<table style="width: 100%; border-collapse: collapse; font-size: 12px; margin-top: 10px;">
                    <thead style="background-color: #f1f3f5;">
                        <tr>
                            <th style="padding: 6px; border: 1px solid #ddd; width: 10%;">Rank</th>
                            <th style="padding: 6px; border: 1px solid #ddd; width: 70%;">Identitas Mesin & Line</th>
                            <th style="padding: 6px; border: 1px solid #ddd; width: 20%; text-align: right;">Total Downtime</th>
                        </tr>
                    </thead>
                    <tbody>${tableRows}</tbody>
                </table>`;
    }
