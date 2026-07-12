using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using DashboardTeknikP1.Models;
using DashboardTeknikP1.Helpers;
using Dapper;

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
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
                {
                    bulkCopy.DestinationTableName = "tbl_PengambilanSparepart";
                    var table = dataList.ToDataTable();
                    
                    var columnsToMap = new[] { "TanggalPengambilan", "MaterialNo", "JumlahPengambilan", "TujuanPengambilan", "NamaPengambil", "HargaSatuanSaatIni", "TotalHarga", "Status", "Plant" };
                    foreach(var col in columnsToMap) bulkCopy.ColumnMappings.Add(col, col);

                    bulkCopy.WriteToServer(table);
                }
            }
        }

        public async Task<List<PengambilanSparepart>> GetAllHistoryAsync(int year)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT p.*, s.MaterialDescription AS MaterialDesc, p.Plant 
                                 FROM tbl_PengambilanSparepart p
                                 LEFT JOIN tbl_SAP_Sparepart s ON p.MaterialNo = s.Material
                                 WHERE YEAR(p.TanggalPengambilan) = @Year
                                 ORDER BY p.TanggalPengambilan DESC, p.TanggalInput DESC";
                var result = await conn.QueryAsync<PengambilanSparepart>(query, new { Year = year });
                return result.ToList();
            }
        }

        public async Task<Tuple<List<SAP_YP14>, List<SAP_YR21>>> GetRawSapDataAsync(int year)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string qYp14 = "SELECT * FROM tbl_SAP_YP14 WHERE YEAR(DocumentDate) = @Year ORDER BY DocumentDate DESC";
                var listYp14 = await conn.QueryAsync<SAP_YP14>(qYp14, new { Year = year });

                string qYr21 = "SELECT PostingDate, DelivQtyPcs, WeekOfBasicFinishedDate, ResourceName FROM tbl_SAP_YR21 WHERE YEAR(PostingDate) = @Year";
                var listYr21 = await conn.QueryAsync<SAP_YR21>(qYr21, new { Year = year });

                return new Tuple<List<SAP_YP14>, List<SAP_YR21>>(listYp14.ToList(), listYr21.ToList());
            }
        }

        public async Task ProcessBulkQuarantineAsync(List<int> ids)
        {
            if (ids == null || ids.Count == 0) return;
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "UPDATE tbl_PengambilanSparepart SET Status = 'KARANTINA' WHERE PengambilanID IN @Ids";
                await conn.ExecuteAsync(query, new { Ids = ids });
            }
        }

        public async Task DeletePengambilanAsync(int id)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM tbl_PengambilanSparepart WHERE PengambilanID = @ID";
                await conn.ExecuteAsync(query, new { ID = id });
            }
        }

        public async Task ShiftToNextWeekAsync(int id)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE tbl_PengambilanSparepart 
                                 SET TanggalPengambilan = DATEADD(day, 7, TanggalPengambilan), 
                                     Status = 'ESTIMASI' 
                                 WHERE PengambilanID = @ID";
                await conn.ExecuteAsync(query, new { ID = id });
            }
        }

        public async Task RestoreToEstimasiAsync(int id)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "UPDATE tbl_PengambilanSparepart SET Status = 'ESTIMASI' WHERE PengambilanID = @ID";
                await conn.ExecuteAsync(query, new { ID = id });
            }
        }
    }
}