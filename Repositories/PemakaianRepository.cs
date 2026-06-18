using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using DashboardTeknikP1.Models;

namespace DashboardTeknikP1.Repositories
{
    public class PemakaianRepository
    {
        private readonly string _connectionString;

        public PemakaianRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public void InsertBulkPengambilan(List<PengambilanSparepart> dataList)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    string query = @"INSERT INTO tbl_PengambilanSparepart 
                                     (TanggalPengambilan, MaterialNo, JumlahPengambilan, TujuanPengambilan, NamaPengambil, HargaSatuanSaatIni, TotalHarga, Status) 
                                     VALUES (@Tgl, @MatNo, @Qty, @Tujuan, @Nama, @Harga, @Total, @Status)";

                    using (SqlCommand cmd = new SqlCommand(query, conn, trans))
                    {
                        foreach (var data in dataList)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@Tgl", data.TanggalPengambilan);
                            cmd.Parameters.AddWithValue("@MatNo", data.MaterialNo);
                            cmd.Parameters.AddWithValue("@Qty", data.JumlahPengambilan);
                            cmd.Parameters.AddWithValue("@Tujuan", string.IsNullOrEmpty(data.TujuanPengambilan) ? (object)DBNull.Value : data.TujuanPengambilan);
                            cmd.Parameters.AddWithValue("@Nama", string.IsNullOrEmpty(data.NamaPengambil) ? (object)DBNull.Value : data.NamaPengambil);
                            cmd.Parameters.AddWithValue("@Harga", data.HargaSatuanSaatIni);
                            cmd.Parameters.AddWithValue("@Total", data.TotalHarga);
                            cmd.Parameters.AddWithValue("@Status", data.Status ?? "ESTIMASI");

                            cmd.ExecuteNonQuery();
                        }
                    }
                    trans.Commit();
                }
            }
        }

        public async Task<List<PengambilanSparepart>> GetAllHistoryAsync()
        {
            var list = new List<PengambilanSparepart>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT p.*, s.MaterialNoDescription AS MaterialDesc 
                                 FROM tbl_PengambilanSparepart p
                                 LEFT JOIN tbl_SAP_Sparepart s ON p.MaterialNo = s.MaterialNo
                                 ORDER BY p.TanggalPengambilan DESC, p.TanggalInput DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    await conn.OpenAsync();
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(new PengambilanSparepart
                            {
                                PengambilanID = Convert.ToInt32(reader["PengambilanID"]),
                                TanggalPengambilan = Convert.ToDateTime(reader["TanggalPengambilan"]),
                                MaterialNo = reader["MaterialNo"]?.ToString() ?? "",
                                JumlahPengambilan = Convert.ToDouble(reader["JumlahPengambilan"]),
                                TujuanPengambilan = reader["TujuanPengambilan"]?.ToString() ?? "",
                                NamaPengambil = reader["NamaPengambil"]?.ToString() ?? "",
                                HargaSatuanSaatIni = Convert.ToDecimal(reader["HargaSatuanSaatIni"]),
                                TotalHarga = Convert.ToDecimal(reader["TotalHarga"]),
                                TanggalInput = Convert.ToDateTime(reader["TanggalInput"]),
                                Status = reader["Status"]?.ToString() ?? "ESTIMASI",
                                MaterialDesc = reader["MaterialDesc"]?.ToString() ?? "Unknown Material"
                            });
                        }
                    }
                }
            }
            return list;
        }

        public async Task<Tuple<List<SAP_YP14>, List<SAP_YR21>>> GetRawSapDataAsync()
        {
            var listYp14 = new List<SAP_YP14>();
            var listYr21 = new List<SAP_YR21>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string qYp14 = "SELECT * FROM tbl_SAP_YP14 ORDER BY DocumentDate DESC";
                using (SqlCommand cmd = new SqlCommand(qYp14, conn))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            listYp14.Add(new SAP_YP14
                            {
                                OrderNo = reader["OrderNo"]?.ToString() ?? "",
                                DocumentDate = Convert.ToDateTime(reader["DocumentDate"]),
                                MaterialNo = reader["MaterialNo"]?.ToString() ?? "",
                                MaterialDescription = reader["MaterialDescription"]?.ToString() ?? "",
                                EquipmentDescription = reader["EquipmentDescription"]?.ToString() ?? "",
                                CostCenter = reader["CostCenter"]?.ToString() ?? "",
                                Qty = Convert.ToDouble(reader["Qty"]),
                                PricePerUnit = Convert.ToDecimal(reader["PricePerUnit"]),
                                MaterialCost = Convert.ToDecimal(reader["MaterialCost"])
                            });
                        }
                    }
                }

                string qYr21 = "SELECT PostingDate, DelivQtyPcs FROM tbl_SAP_YR21";
                using (SqlCommand cmd = new SqlCommand(qYr21, conn))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            listYr21.Add(new SAP_YR21
                            {
                                PostingDate = Convert.ToDateTime(reader["PostingDate"]),
                                DelivQtyPcs = Convert.ToDouble(reader["DelivQtyPcs"])
                            });
                        }
                    }
                }
            }
            return new Tuple<List<SAP_YP14>, List<SAP_YR21>>(listYp14, listYr21);
        }

        // ====================================================================
        // FUNGSI BARU: PROSES UPDATE BULK STATUS JADI KARANTINA
        // ====================================================================
        public async Task ProcessBulkQuarantineAsync(List<int> ids)
        {
            if (ids == null || ids.Count == 0) return;
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string idList = string.Join(",", ids);
                string query = $"UPDATE tbl_PengambilanSparepart SET Status = 'KARANTINA' WHERE PengambilanID IN ({idList})";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        // ====================================================================
        // FUNGSI BARU: RETUR SPAREPART (HAPUS PERMANEN DARI DATABASE)
        // ====================================================================
        public async Task DeletePengambilanAsync(int id)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM tbl_PengambilanSparepart WHERE PengambilanID = @ID";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ID", id);
                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        // ====================================================================
        // FUNGSI BARU: GEBER KEMBALI KE HISTORI MINGGU DEPAN (+7 HARI & ESTIMASI)
        // ====================================================================
        public async Task ShiftToNextWeekAsync(int id)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE tbl_PengambilanSparepart 
                                 SET TanggalPengambilan = DATEADD(day, 7, TanggalPengambilan), 
                                     Status = 'ESTIMASI' 
                                 WHERE PengambilanID = @ID";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ID", id);
                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        // ====================================================================
        // FUNGSI BARU: BATAL KARANTINA (KEMBALIKAN VALUE STATUS KE ESTIMASI)
        // ====================================================================
        public async Task RestoreToEstimasiAsync(int id)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "UPDATE tbl_PengambilanSparepart SET Status = 'ESTIMASI' WHERE PengambilanID = @ID";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ID", id);
                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
    }
}