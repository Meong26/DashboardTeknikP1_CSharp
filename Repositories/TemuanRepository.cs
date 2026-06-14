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

        public TemuanRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // Method untuk menyimpan laporan baru dari Teknisi (Field)
        public void InsertTemuan(TemuanAbnormal temuan)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO tbl_TemuanAbnormal 
                                (UserID, MesinID, DeskripsiAbnormal, TindakanKorektif) 
                                VALUES (@UserID, @MesinID, @Deskripsi, @Tindakan)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", temuan.UserID);
                    cmd.Parameters.AddWithValue("@MesinID", temuan.MesinID);
                    cmd.Parameters.AddWithValue("@Deskripsi", temuan.DeskripsiAbnormal);
                    cmd.Parameters.AddWithValue("@Tindakan", temuan.TindakanKorektif);
                    
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Method untuk menarik daftar temuan agar bisa dilihat oleh Supervisor
        public List<TemuanAbnormal> GetAllTemuan()
        {
            List<TemuanAbnormal> listTemuan = new List<TemuanAbnormal>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM tbl_TemuanAbnormal ORDER BY TanggalInput DESC";

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
                                UserID = reader["UserID"].ToString(),
                                MesinID = reader["MesinID"].ToString(),
                                DeskripsiAbnormal = reader["DeskripsiAbnormal"].ToString(),
                                TindakanKorektif = reader["TindakanKorektif"].ToString(),
                                StatusTemuan = reader["StatusTemuan"].ToString()
                            });
                        }
                    }
                }
            }
            return listTemuan;
        }
    }
}