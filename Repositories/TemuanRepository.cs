using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using DashboardTeknikP1.Models;

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
        public void InsertTemuan(TemuanAbnormal temuan)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                // Query SQL disesuaikan dengan struktur baru (menggunakan KodeMesin)
                string query = @"INSERT INTO tbl_TemuanAbnormal 
                                (TanggalInput, UserID, Line, KodeMesin, DeskripsiAbnormal, TindakanKorektif, StatusTemuan) 
                                VALUES (@TanggalInput, @UserID, @Line, @KodeMesin, @Deskripsi, @Tindakan, @StatusTemuan)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    // Pemetaan parameter untuk mencegah SQL Injection
                    cmd.Parameters.AddWithValue("@TanggalInput", temuan.TanggalInput);
                    cmd.Parameters.AddWithValue("@UserID", temuan.UserID);
                    cmd.Parameters.AddWithValue("@Line", temuan.Line ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@KodeMesin", temuan.KodeMesin);
                    cmd.Parameters.AddWithValue("@Deskripsi", temuan.DeskripsiAbnormal);

                    // Antisipasi jika kolom Tindakan Korektif kosong saat disubmit oleh teknisi
                    cmd.Parameters.AddWithValue("@Tindakan", string.IsNullOrEmpty(temuan.TindakanKorektif)
                        ? (object)DBNull.Value
                        : temuan.TindakanKorektif);

                    cmd.Parameters.AddWithValue("@StatusTemuan", temuan.StatusTemuan);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ====================================================================
        // 2. METHOD: TARIK SEMUA DAFTAR RIWAYAT TEMUAN (SELECT)
        // ====================================================================
        public List<TemuanAbnormal> GetAllTemuan()
        {
            List<TemuanAbnormal> listTemuan = new List<TemuanAbnormal>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                // Menampilkan laporan terbaru di urutan paling atas
                string query = @"
                    SELECT t.*, m.NamaMesin 
                    FROM tbl_TemuanAbnormal t
                    LEFT JOIN tbl_Mesin m ON t.KodeMesin = m.KodeMesin
                    ORDER BY t.TanggalInput DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            listTemuan.Add(new TemuanAbnormal
                            {
                                TemuanID = Convert.ToInt32(reader["TemuanID"]),
                                TanggalInput = Convert.ToDateTime(reader["TanggalInput"]),
                                UserID = reader["UserID"] != DBNull.Value ? reader["UserID"].ToString() : "-",
                                Line = reader["Line"] != DBNull.Value ? reader["Line"].ToString() : "-",
                                KodeMesin = reader["KodeMesin"] != DBNull.Value ? reader["KodeMesin"].ToString() : "-",
                                NamaMesin = reader["NamaMesin"] != DBNull.Value ? reader["NamaMesin"].ToString() : "Mesin Tidak Dikenal",
                                DeskripsiAbnormal = reader["DeskripsiAbnormal"] != DBNull.Value ? reader["DeskripsiAbnormal"].ToString() : "-",
                                TindakanKorektif = reader["TindakanKorektif"] != DBNull.Value ? reader["TindakanKorektif"].ToString() : "",
                                StatusTemuan = reader["StatusTemuan"] != DBNull.Value ? reader["StatusTemuan"].ToString() : "OPEN"
                            });
                        }
                    }
                }
            }
            return listTemuan;
        }

        // ====================================================================
        // 3. METHOD: AMBIL DAFTAR MASTER MESIN UNTUK DROPDOWN FORM (SELECT)
        // ====================================================================
        public List<Mesin> GetAllMesin()
        {
            var listMesin = new List<Mesin>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                // Mengambil KodeMesin dan NamaMesin untuk kebutuhan dropdown dinamis di halaman Create
                string query = "SELECT KodeMesin, NamaMesin FROM tbl_Mesin ORDER BY NamaMesin ASC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            listMesin.Add(new Mesin
                            {
                                KodeMesin = reader["KodeMesin"].ToString(),
                                NamaMesin = reader["NamaMesin"].ToString()
                            });
                        }
                    }
                }
            }
            return listMesin;
        }
        
        // ====================================================================
        // 4. METHOD: AMBIL 1 DATA TEMUAN BERDASARKAN ID (SELECT BY ID)
        // ====================================================================
        public TemuanAbnormal GetTemuanById(int id)
        {
            TemuanAbnormal temuan = null;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT t.*, m.NamaMesin 
                    FROM tbl_TemuanAbnormal t
                    LEFT JOIN tbl_Mesin m ON t.KodeMesin = m.KodeMesin
                    WHERE t.TemuanID = @TemuanID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@TemuanID", id);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            temuan = new TemuanAbnormal
                            {
                                TemuanID = Convert.ToInt32(reader["TemuanID"]),
                                TanggalInput = Convert.ToDateTime(reader["TanggalInput"]),
                                UserID = reader["UserID"].ToString(),
                                Line = reader["Line"] != DBNull.Value ? reader["Line"].ToString() : "-",
                                KodeMesin = reader["KodeMesin"].ToString(),
                                NamaMesin = reader["NamaMesin"] != DBNull.Value ? reader["NamaMesin"].ToString() : "Mesin Tidak Dikenal",
                                DeskripsiAbnormal = reader["DeskripsiAbnormal"].ToString(),
                                TindakanKorektif = reader["TindakanKorektif"] != DBNull.Value ? reader["TindakanKorektif"].ToString() : "",
                                StatusTemuan = reader["StatusTemuan"].ToString()
                            };
                        }
                    }
                }
            }
            return temuan;
        }

        // ====================================================================
        // 5. METHOD: UPDATE TINDAKAN DAN UBAH STATUS MENJADI CLOSED
        // ====================================================================
        public void CloseTemuan(int id, string tindakanKorektif)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE tbl_TemuanAbnormal 
                                SET TindakanKorektif = @Tindakan, StatusTemuan = 'CLOSED' 
                                WHERE TemuanID = @TemuanID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@TemuanID", id);
                    cmd.Parameters.AddWithValue("@Tindakan", string.IsNullOrEmpty(tindakanKorektif) ? (object)DBNull.Value : tindakanKorektif);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}