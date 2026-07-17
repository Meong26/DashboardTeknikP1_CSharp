using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using DashboardTeknikP1.Models;
using Dapper;
using System.Threading.Tasks;

namespace DashboardTeknikP1.Repositories
{
    public class TemuanRepository
    {
        private readonly string _connectionString;

        // Konstruktor untuk mengambil Connection String secara dinamis dari appsettings.json
        public TemuanRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // ====================================================================
        // 1. METHOD: SIMPAN LAPORAN TEMUAN BARU (INSERT)
        // ====================================================================
        public async Task InsertTemuanAsync(TemuanAbnormal temuan)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO tbl_TemuanAbnormal 
                                (TanggalInput, UserID, Line, KodeMesin, DeskripsiAbnormal, TindakanKorektif, StatusTemuan) 
                                VALUES (@TanggalInput, @UserID, @Line, @KodeMesin, @DeskripsiAbnormal, @TindakanKorektif, @StatusTemuan)";

                var parameters = new
                {
                    TanggalInput = temuan.TanggalInput,
                    UserID = temuan.UserID,
                    Line = string.IsNullOrEmpty(temuan.Line) ? "" : temuan.Line,
                    KodeMesin = temuan.KodeMesin,
                    DeskripsiAbnormal = temuan.DeskripsiAbnormal,
                    TindakanKorektif = string.IsNullOrEmpty(temuan.TindakanKorektif) ? "" : temuan.TindakanKorektif,
                    StatusTemuan = temuan.StatusTemuan
                };

                await conn.ExecuteAsync(query, parameters);
            }
        }

        // ====================================================================
        // 2. METHOD: TARIK SEMUA DAFTAR RIWAYAT TEMUAN (SELECT)
        // ====================================================================
        public async Task<List<TemuanAbnormal>> GetAllTemuanAsync()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT t.TemuanID, t.TanggalInput, COALESCE(u.NamaLengkap, t.UserID) AS UserID, t.Line, t.KodeMesin, 
                           m.NamaMesin, t.DeskripsiAbnormal, t.TindakanKorektif, t.StatusTemuan,
                           t.TanggalClosed, t.ClosedBy, uc.NamaLengkap AS ClosedByName
                    FROM tbl_TemuanAbnormal t
                    LEFT JOIN tbl_Mesin m ON t.KodeMesin = m.KodeMesin
                    LEFT JOIN tbl_Users u ON t.UserID = u.UserID
                    LEFT JOIN tbl_Users uc ON t.ClosedBy = uc.UserID
                    ORDER BY t.TanggalInput DESC";

                var result = await conn.QueryAsync<TemuanAbnormal>(query);
                
                // Set default values for null fields
                foreach (var item in result)
                {
                    item.UserID ??= "-";
                    item.Line ??= "-";
                    item.KodeMesin ??= "-";
                    item.NamaMesin ??= "Mesin Tidak Dikenal";
                    item.DeskripsiAbnormal ??= "-";
                    item.TindakanKorektif ??= "";
                    item.StatusTemuan ??= "OPEN";
                }
                
                return result.AsList();
            }
        }

        // ====================================================================
        // 3. METHOD: AMBIL DAFTAR MASTER MESIN UNTUK DROPDOWN FORM (SELECT)
        // ====================================================================
        public async Task<List<Mesin>> GetAllMesinAsync()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT KodeMesin, NamaMesin FROM tbl_Mesin ORDER BY NamaMesin ASC";
                var result = await conn.QueryAsync<Mesin>(query);
                return result.AsList();
            }
        }
        
        // ====================================================================
        // 4. METHOD: AMBIL 1 DATA TEMUAN BERDASARKAN ID (SELECT BY ID)
        // ====================================================================
        public async Task<TemuanAbnormal?> GetTemuanByIdAsync(int id)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT t.TemuanID, t.TanggalInput, COALESCE(u.NamaLengkap, t.UserID) AS UserID, t.Line, t.KodeMesin, 
                           m.NamaMesin, t.DeskripsiAbnormal, t.TindakanKorektif, t.StatusTemuan,
                           t.TanggalClosed, t.ClosedBy, uc.NamaLengkap AS ClosedByName
                    FROM tbl_TemuanAbnormal t
                    LEFT JOIN tbl_Mesin m ON t.KodeMesin = m.KodeMesin
                    LEFT JOIN tbl_Users u ON t.UserID = u.UserID
                    LEFT JOIN tbl_Users uc ON t.ClosedBy = uc.UserID
                    WHERE t.TemuanID = @TemuanID";

                var item = await conn.QueryFirstOrDefaultAsync<TemuanAbnormal>(query, new { TemuanID = id });
                
                if (item != null)
                {
                    item.UserID ??= "-";
                    item.Line ??= "-";
                    item.KodeMesin ??= "-";
                    item.NamaMesin ??= "Mesin Tidak Dikenal";
                    item.DeskripsiAbnormal ??= "-";
                    item.TindakanKorektif ??= "";
                    item.StatusTemuan ??= "OPEN";
                }

                return item;
            }
        }

        // ====================================================================
        // 5. METHOD: UPDATE TINDAKAN DAN UBAH STATUS MENJADI CLOSED
        // ====================================================================
        public async Task CloseTemuanAsync(int id, string tindakanKorektif, string closedBy)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE tbl_TemuanAbnormal 
                                SET TindakanKorektif = @Tindakan, StatusTemuan = 'CLOSED',
                                    TanggalClosed = GETDATE(), ClosedBy = @ClosedBy
                                WHERE TemuanID = @TemuanID";

                await conn.ExecuteAsync(query, new { 
                    TemuanID = id, 
                    Tindakan = string.IsNullOrEmpty(tindakanKorektif) ? null : tindakanKorektif,
                    ClosedBy = closedBy
                });
            }
        }
    }
}