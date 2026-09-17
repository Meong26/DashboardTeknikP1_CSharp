using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using DashboardTeknikP1.Models;
using Dapper;

namespace DashboardTeknikP1.Repositories
{
    public class HomeRepository
    {
        private readonly string _connectionString;

        public HomeRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<List<SAP_YP11>> GetDowntimeDetailsAsync(int year)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                var startDate = new DateTime(year, 1, 1);
                var endDate = new DateTime(year + 1, 1, 1);

                string query = @"SELECT WorkCenterPPDesc, NotificationDesc, TotalDownTimeInMinutes, 
                                        NotificationDate, DownTimeCode_ActivityCodeDesc,
                                        FunctionLocation, NotificationType, WageGroup_GroupShift, 
                                        WeekKalendarIndofood, ActivityText
                                 FROM tbl_SAP_YP11 
                                 WHERE NotificationDate >= @StartDate AND NotificationDate < @EndDate
                                 ORDER BY NotificationDate DESC";
                var result = await conn.QueryAsync<SAP_YP11>(query, new { StartDate = startDate, EndDate = endDate });
                return result.ToList();
            }
        }

        public async Task<List<SAP_YR21>> GetProduksiDetailsAsync(int year)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                var startDate = new DateTime(year, 1, 1);
                var endDate = new DateTime(year + 1, 1, 1);

                string query = @"SELECT PostingDate, ResourceName, PlannedHour, WageGroup, WeekOfBasicFinishedDate 
                                 FROM tbl_SAP_YR21
                                 WHERE PostingDate >= @StartDate AND PostingDate < @EndDate";
                var result = await conn.QueryAsync<SAP_YR21>(query, new { StartDate = startDate, EndDate = endDate });
                return result.ToList();
            }
        }

        public async Task<List<SAP_Sparepart>> GetEwsSparepartsAsync()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT Material, MaterialDescription, CurrentStock, SafetyStock, StorLoct 
                                FROM tbl_SAP_Sparepart 
                                WHERE CurrentStock <= SafetyStock 
                                ORDER BY CurrentStock ASC";
                var result = await conn.QueryAsync<SAP_Sparepart>(query);
                return result.ToList();
            }
        }

        public async Task<List<int>> GetAvailableYearsAsync()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT DISTINCT YEAR(NotificationDate) AS Yr FROM tbl_SAP_YP11 WHERE NotificationDate IS NOT NULL
                    UNION
                    SELECT DISTINCT YEAR(PostingDate) AS Yr FROM tbl_SAP_YR21 WHERE PostingDate IS NOT NULL
                    UNION
                    SELECT DISTINCT YEAR(DocumentDate) AS Yr FROM tbl_SAP_YP14 WHERE DocumentDate IS NOT NULL
                    UNION
                    SELECT DISTINCT YEAR(TanggalPengambilan) AS Yr FROM tbl_PengambilanSparepart WHERE TanggalPengambilan IS NOT NULL
                    ORDER BY Yr DESC";
                var result = await conn.QueryAsync<int>(query);
                var list = result.ToList();
                if (!list.Any())
                {
                    list.Add(DateTime.Now.Year);
                }
                return list;
            }
        }
    }
}
