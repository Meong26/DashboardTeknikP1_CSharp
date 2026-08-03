# Laporan Analisis Keamanan Aplikasi

**Project:** DashboardTeknikP1_CSharp
**Repo:** Meong26/DashboardTeknikP1_CSharp
**Tanggal Analisis:** 3 Agustus 2026
**Metode:** Review statis manual pada source code (Controllers, Repositories, Helpers, Services, Models, Views, JS)

---

## 1. Ringkasan Project

DashboardTeknikP1_CSharp adalah aplikasi web internal (ASP.NET Core MVC, C#) untuk departemen Teknik pabrik Indofood. Aplikasi menampilkan dashboard downtime mesin dan output produksi (data SAP YP11/YR21), manajemen sparepart (stok, EWS, pemakaian YP14), pencatatan temuan abnormal, monitoring HMI, serta fitur login berbasis role dan upload data Excel (SAP) yang diproses di memory.

**Tech stack:** .NET 10, SQL Server, Dapper, ClosedXML, MemoryCache, pola Repository + Service.

---

## 2. Skor Keamanan Keseluruhan

**6.5 / 10** (Aman dari SQL injection, namun ada kerentanan SSRF, XSS potensial, dan beberapa praktik keamanan yang perlu diperbaiki).

---

## 3. Hal yang Sudah Benar (Strengths)

| No | Aspek | Keterangan |
|----|-------|------------|
| 1 | SQL Injection | Semua query memakai parameter Dapper (@UserID, @Id) + bulk copy. Tidak ditemukan interpolasi string pada query. |
| 2 | Password | Hash PBKDF2 (100.000 iterasi, salted), perbandingan timing-safe, auto-migrasi dari SHA-256 legacy. |
| 3 | Brute-force | Rate limiting login (5 percobaan gagal → lockout 5 menit) + Anti-Forgery Token di semua form POST. |
| 4 | Autoriasasi | Semua controller pakai [Authorize] dengan role; session cookie HttpOnly, sliding expiration 5 menit. |
| 5 | Upload Excel | Tidak menyimpan file ke disk (diproses di memory stream), aman dari path traversal. |
| 6 | Cache | Cache dashboard memakai token yang di-reset saat upload, tidak ada stale data. |

---

## 4. Kerentanan yang Ditemukan

### 4.1 SSRF via Ping di HmiController (Sedang-Tinggi)

**Lokasi:** `HmiController.CheckStatus(string ip)`

Endpoint menerima IP/hostname dari user (role apa pun: Administrator s/d Dashboard) dan langsung di-ping + di-GET. Karena aplikasi berjalan di jaringan internal pabrik, ini bisa dipakai untuk memindai host internal, melewati firewall, atau memukul layanan internal (mis. localhost:1433, dll).

**Rekomendasi:** Whitelist range IP jaringan HMI + validasi format IP, jangan terima hostname bebas.

---

### 4.2 SSRF + Header Injection di HmiController.LogAccess (Sedang)

**Lokasi:** `HmiController.LogAccess`

Nilai `ip` dan `hmiName` dari form disimpan mentah ke DB tanpa validasi, dan ditampilkan ulang. Tidak ada validasi bahwa IP milik jaringan HMI.

**Rekomendasi:** Validasi format IP + whitelist, sanitasi input sebelum disimpan/ditampilkan.

---

### 4.3 XSS Potensial di Frontend JS (Sedang)

**Lokasi:** sparepart.js, pemakaian.js, dashboard-analytics.js, dan file JS lain

Banyak file JS memakai `innerHTML` untuk render data dari server (data SAP Excel yang di-upload, deskripsi temuan, nama mesin, dll). Data Excel bisa mengandung tag HTML berbahaya. Risiko lebih bersifat internal (data di-upload role Section/Administrator), namun tetap XSS.

**Rekomendasi:** Gunakan `textContent` atau library escape saat render; jangan pernah masukkan data user ke innerHTML tanpa escape. (Tidak ditemukan penggunaan Html.Raw di server - bagus.)

---

### 4.4 Rate Limiting Hanya di Memory (Rendah-Sedang)

**Lokasi:** Login attempt counter & lockout disimpan di MemoryCache

Kalau aplikasi di-restart atau dijalankan multi-instance, rate limit hilang. Untuk aplikasi internal single-server masih OK, tapi bukan best practice.

**Rekomendasi:** Pakai distributed cache (Redis) bila multi-instance.

---

### 4.5 Cookie SecurePolicy = SameAsRequest + HTTPS Redirect Nonaktif (Rendah-Sedang)

Karena aplikasi bisa diakses via HTTP lokal (HP), cookie bisa bocor lewat jaringan tidak terenkripsi.

**Rekomendasi:** Aktifkan HTTPS + set SecurePolicy = Always untuk production.

---

### 4.6 Potensi CSRF di API JSON Endpoints (Rendah-Sedang)

**Lokasi:** SaveDataFast, QuarantineItems, RestoreItem, ReturItem, ShiftToNextWeek, UpdatePriorities

Endpoint memakai [FromBody] JSON + [ValidateAntiForgeryToken]. Token anti-forgery untuk fetch/JSON biasanya dibaca dari header (X-XSRF-TOKEN), perlu dipastikan header-nya dikirim dari JS.

**Rekomendasi:** Pastikan semua fetch JS mengirim header X-XSRF-TOKEN; tes CSRF secara manual.

---

### 4.7 Error Detail Bocor ke User (Rendah)

**Lokasi:** UploadController, PemakaianController, SparepartController, TemuanController

Di beberapa catch, pesan exception asli (ex.Message) dikembalikan langsung ke client, bisa membocorkan struktur internal DB.

**Rekomendasi:** Log exception ke server, kembalikan pesan generik ke user.

---

### 4.8 Connection String di Repo Publik (Rendah)

`appsettings.json` berisi connection string SQL Server (localhost\SQLEXPRESS) yang dipublikasikan. Untuk aplikasi internal mungkin tidak masalah, tapi sebaiknya dipindah ke environment variable / user-secrets.

**Rekomendasi:** Pindahkan ke env var / user-secrets / Azure Key Vault.

---

### 4.9 Admin Bisa Mengubah Role User Lain Tanpa Audit Trail (Rendah)

**Lokasi:** UsersController Edit

Administrator bisa mengubah RoleID user lain. Tidak ada log audit perubahan role.

**Rekomendasi:** Tambahkan audit log untuk perubahan role (siapa, kapan, dari role apa ke role apa).

---

### 4.10 Tidak Ada Password Policy (Rendah)

**Lokasi:** UserViewModels

Tidak ada validasi kekuatan password (min length, complexity).

**Rekomendasi:** Tambahkan minimal length (mis. 8 karakter) + complexity requirement.

---

## 5. Rekomendasi Prioritas

| Prioritas | Kerentanan | Aksi |
|-----------|-----------|------|
| 🔴 HIGH | SSRF di HmiController | Whitelist IP internal + validasi format IP |
| 🟠 MEDIUM | XSS via innerHTML | Sanitasi/escape output, gunakan textContent |
| 🟠 MEDIUM | Connection string di repo | Pindahkan ke env var / user-secrets |
| 🟠 MEDIUM | HTTPS nonaktif | Aktifkan HTTPS + SecurePolicy Always |
| 🟠 MEDIUM | Error detail bocor | Log exception, pesan generik ke user |
| 🟢 LOW | Password policy | Min length 8 + complexity |
| 🟢 LOW | Audit trail role change | Tambah log audit |
| 🟢 LOW | Rate limit memory | Distributed cache (Redis) |
| 🟢 LOW | CSRF JSON header | Pastikan X-XSRF-TOKEN dikirim |

---

## 6. Kesimpulan

Aplikasi sudah menerapkan praktik keamanan dasar yang baik (parameterized query, PBKDF2, rate limiting, authorization berbasis role, anti-CSRF di form). Prioritas utama perbaikan adalah **SSRF di HmiController** dan **sanitasi output di frontend**. Dengan menutup kedua celah tersebut plus hardening HTTPS & secret management, skor keamanan bisa naik ke ~8.5/10.

---

*Laporan dibuat otomatis oleh TeleClaw pada 3 Agustus 2026 berdasarkan review statis source code.*