# STANDARD OPERATING PROCEDURE PEMBUATAN APLIKASI DI UNIT INDOFOOD 1

* **Type**: Standard Operating Procedure 1  
* **No.**: SOP ISM.010/IT/11/2020 1  
* **Version**: 1.0 1  
* **Effective date**: Januari 2021 1  
* **Location**: Corporate Information Technology 1

## Document Control & Enquiries

* **Prepared By**: Fr Xaverina W (IT Policy & Compliance) & Anton Tisna (Non ABAP Developers Team Lead) 2\.  
* **Approved By**: Denny Prijadi (IT Policy & Compliance Team Lead) & Peter Layar (IT Solution Head) 2\.  
* **Document Ownership**: IT Security 3\.  
* **Contact for Enquiries**: IT Security (Phone: \+62 21 57958822 Ext. 1738 | E-mail: it.security@indofood.co.id) 3\.  
* **Related Documents**: SOP Pelayanan IT, SOP IT Infrastructure 4\.

## 1\. INTRODUCTION

### 1.1 Pendahuluan

Suatu sistem aplikasi harus dikembangkan, dimodifikasi agar sesuai dengan kebutuhan user dan kebutuhan bisnis Perusahaan 5\. Corporate Information Technology (selanjutnya disebut "CIT") merupakan divisi yang bertanggung jawab untuk pengembangan sistem aplikasi yang berada di naungan PT. Indofood Sukses Makmur, Tbk (selanjutnya disebut "Perusahaan") 5\.  
Namun dikarenakan semakin tinggi permintaan untuk pembuatan sistem aplikasi yang disebabkan oleh makin majunya dunia teknologi, maka cabang atau anak Perusahaan diperbolehkan untuk membuat sistem aplikasi secara mandiri sesuai dengan kebutuhan masing-masing dan mengikuti prosedur/policy yang berlaku 5.SOP ini bersifat *'living document'* yaitu akan selalu diperbaharui sesuai dengan kebutuhan dan kondisi yang timbul di masa depan 5\.

### 1.2 Scope

SOP ini diaplikasikan untuk seluruh staff IT dan personil yang berada di cabang atau anak Perusahaan yang membutuhkan 5\. Dalam SOP ini membahas hal-hal mengenai prosedur yang perlu dilakukan untuk pengembangan system di Unit Indofood 5\.

## 2\. POLICY

* IT Corporate tidak bertanggung jawab atas akibat yang timbul untuk aplikasi yang didevelop oleh unit 6\.  
* Setiap pembuatan aplikasi di Unit harus melalui prosedur yang berlaku, yaitu via *Service Desk Management* (reff : SOP Layanan IT), diketahui oleh IT-SAS Unit dan PIC *Solution Integrator* unit masing-masing serta mendapatkan persetujuan dari Kadiv masing-masing 6\.  
* Aplikasi yang didevelop pada unit harus bersifat *localize* dan siap di-*replace* apabila ada aplikasi sejenis yang diimplementasi oleh *corporate* secara nasional 6\.  
* Aplikasi yang didevelop harus mempergunakan tools standard yang ditentukan oleh IT Corporate, yaitu 6:  
* **Web**: Menggunakan Bahasa Microsoft ASP.NET C\#  
* **Database**: Microsoft SQL Server 2008 keatas  
* **Desktop**: Menggunakan Bahasa Microsoft C\#  
* **Mobile**: React Native/Android Studio  
* **Komponen**: Menggunakan komponen yang terlisensi dan masih valid  
* *Source code* terbaru dari setiap aplikasi yang didevelop mandiri pada unit agar disimpan menggunakan online repository (Github) (reff : https://github.com/ISMCIT) oleh IT SAS Unit 6\. Untuk mekanisme teknis otorisasi ke online repository bisa menghubungi IT-Dev Corporate 6\.  
* *Publish web application* ke internet hanya bisa dilakukan oleh unit yang sudah mempunyai infrastructure yang memadai sesuai dengan ketentuan dari team IT Corp \- infrastructure (reff: SOP IT Infrastructure) dengan persetujuan Kadiv masing-masing 6\.  
* Password super user atau administrator server database production hanya dipegang oleh personnel yang ditunjuk atau mempunyai role sebagai DBA (Database administrator) 6\.  
* *Source code* harus dibackup secara *daily* kedalam online repository (Github), dan IT-SAS Unit harus memastikan prosedur backup dijalankan dengan baik 6\.  
* Environment testing/QA dan production harus environment yang terpisah 6\.  
* Dokumentasi project (changes, requirement, testing) harus dikelola dengan baik oleh seluruh anggota team ataupun PIC yang ditunjuk 6\.  
* Dokumentasi yang perlu disiapkan 6:  
* User requirement  
* System & database design  
* Source code  
* UAT (optional)  
* Business process

## 3\. PROCEDURE

### 3.1 Workflow Solution Development Program

Prosedur ini melibatkan dua pihak, yaitu **IT SAS Unit** dan **Database Administrator** dengan alur sebagai berikut 7:

* **Dari sisi IT SAS Unit:**Mulai (Start) ➔ Membuat *service desk management* untuk Akses Repositori (Repository Access) ➔ Mendapatkan Persetujuan dari Manager & Kadiv ➔ Menunggu *Database Administrator* menyelesaikan tiket ➔ Selesai (End) 7\.  
* **Dari sisi Database Administrator:**Mendapatkan Persetujuan dari PIC Solution Integrator, Non ABAP Developers Team Lead & IT Solution Head ➔ Membuat Akses Repositori di GitHub ➔ Memenuhi tiket *service desk management* dengan memberikan informasi URL github ke pihak IT SAS Unit 7\.

## APPENDIX A : GUIDELINE PENGGUNAAN ONLINE REPOSITORY (GITHUB) IT INDOFOOD

Berikut tata cara penggunaan github untuk project di Unit dalam Indofood Group menggunakan *command prompt* di OS Windows 8:a) Install dan registrasi github terbaru (dapat diunduh di https://git-scm.com/downloads). Harap mengikuti petunjuk instalasi dengan opsi default (referensi instalasi detail ada di http://webapp.indofood.com/ism\_files/Cara%20Install%20Git.pdf) 8.b) Mendaftarkan dan mengaktifkan account ke website https://github.com 8.c) Membuat permintaan pembuatan aplikasi via *Service Desk Management* 8.d) Menunggu proses persetujuan sampai kadiv 8.e) Setelah permintaan disetujui, CIT akan mendaftarkan project dan mendaftarkan account tersebut sebagai *contributor* 8.f) CIT akan memberikan info URL github repository (URL diawali dengan http://github.com/ISMCIT) 8.g) Login terlebih dahulu dengan menggunakan account yang dibuat pada poin b) ke website Github 8.h) Melakukan clone repo ke local repo (Contoh perintah: git clone https://github.com/ISMCIT/TestRepo) 8.i) Jika ada perubahan, file bisa langsung di-edit di folder local 8.j) Untuk memasukkan perubahan, lakukan *add* (Contoh perintah: git add tes1.txt untuk satu file, atau git add . untuk keseluruhan folder) 8, 9\. (Sebagai alternatif, copy/clone source code juga bisa dilakukan dengan *drag and drop* via website Github 10).k) Setelah dilakukan *add*, berikan komentar *commit* (Contoh: git commit \-m "perubahan tambah 1 line") lalu lakukan *push* untuk mengupload file ke remote repo (git push origin) 9.l) Setelah perintah push, file akan terupdate otomatis di remote repo dan bisa di cek melalui website github 9\.

## APPENDIX B : SYSTEM DEVELOPMENT LIFE CYCLE STANDARD (NON SAP)

**B1. System Iniation** 11

1. Kebutuhan permintaan dituangkan secara jelas.  
2. Proposal konsep.  
3. Adanya *feasibility study* (optional).  
4. Tersedianya *project charter* (optional).

**B2. System Requirement Analysis** 11

1. Analisa kebutuhan user dan develop *user requirements*.  
2. Membuat *Functional Requirement Documents*.  
3. Kebutuhan mengenai otorisasi dan issue security harus didefinisikan secara jelas.

**B3. System Design** 11

1. Tahap ini adalah mengubah dari requirement menjadi *Design Document*.  
2. Function dan system operation sudah harus dijabarkan secara detail.  
3. Analisa risiko harus sudah diselesaikan pada tahap System Requirement dan System Design.  
4. Hasil review final harus sudah diselesaikan untuk memastikan hasil design sesuai dengan efisiensi, biaya, fleksibel dan keamanan yang memadai.

**B4. System Construction / Procurement** 11

1. Phase ini merupakan transformasi dari dokumen design ke dalam bentuk produk atau solusi software.  
2. Manual atau testing otomatis modul sudah dilakukan oleh system atau software developer. Pertimbangan masalah keamanan dibicarakan pada saat testing.  
3. Third-party product dapat berfungsi sebagai system/software solution jika memenuhi user requirement dan bersifat praktis dari sisi budget dan resource.

**B5. System Testing and Acceptance** 11, 12

1. Fase ini merupakan fase validasi atau konfirmasi bahwa pengembangan system/program telah memenuhi System Requirement Analysis 11\.  
2. Melakukan proses testing *Quality Assurance* yang sebaiknya dilaksanakan oleh team khusus 11\.  
3. Dokumentasi testing harus dibuat secara detail dan cocok dengan antara criteria testing dan detail requirement 11\.  
4. *Final acceptance testing* harus dilakukan oleh user 11\.  
5. Problem/issue yang ditemukan saat testing harus diperbaiki sebelum proses implementasi 12\.

**B6. System Implementation** 12

1. Setelah proses testing dan *user-acceptance* dilewati, software dipindahkan dari environment testing ke production.  
2. Seluruh tools, code atau mekanisme akses ke development atau software testing system harus sudah dikeluarkan dari program/aplikasi yang akan diupload ke environment production.  
3. Memberikan user training sesuai kebutuhan.

**B7. System Maintenance** 12

1. Tahap ini merupakan kelanjutan dari *ongoing* system/software. Tahap ini selesai jika system/aplikasi sudah tidak digunakan lagi.  
2. Perubahan terhadap system harus dijadwalkan, didokumentasikan dan dikomunikasikan ke user.  
3. Melakukan testing jika terjadi perubahan arsitektur/konfigurasi pada system/aplikasi.

## APPENDIX C : SPECIAL CONDITION

Beberapa kejadian yang tidak tercakup dalam SOP ini seringkali memaksa Corporate IT untuk bekerja diluar prosedur yang berlaku 13\. Kasus-kasus khusus yang membutuhkan pengecualian, maka diperlukan bukti tertulis/internal memorandum yang ditandatangani minimal oleh Kepala Cabang/Departemen dari unit yang bersangkutan sebagai tanda persetujuan dan bersedia menerima segala risiko yang ditimbulkan 13\.  
Berikut kondisi yang membutuhkan pengecualian (namun tidak terbatas) dibawah ini 13:

* Saat project berlangsung dan belum dilakukannya serah terima project.  
* Salah satu/sebagian PIC yang bertugas tidak ada ditempat/posisi kosong.  
* Keadaan *force majeure* sehingga seluruh prosedur tidak dapat dilaksanakan.  
* Prosedur tidak dapat diimplementasi akibat perubahan besar pada lingkungan organisasi Perusahaan/Corporate IT.

Apabila terjadi pengecualian seperti yang dideskripsikan diatas, maka Corporate IT dapat menggunakan metode lain untuk memenuhi tujuan dari prosedur ini dengan persetujuan dan pengetahuan dari IT Excom 13\. Pengganti prosedur yang diimplementasikan harus tetap memiliki kontrol dan pengawasan dari IT Security 13\.  
