using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
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
                string query = @"SELECT WorkCenterPPDesc, NotificationDesc, TotalDownTimeInMinutes, 
                                        NotificationDate, DownTimeCode_ActivityCodeDesc,
                                        FunctionLocation, NotificationType, WageGroup_GroupShift, 
                                        WeekKalendarIndofood, ActivityText
                                 FROM tbl_SAP_YP11 
                                 WHERE YEAR(NotificationDate) = @Year
                                 ORDER BY NotificationDate DESC";
                var result = await conn.QueryAsync<SAP_YP11>(query, new { Year = year });
                return result.ToList();
            }
        }

        public async Task<List<SAP_YR21>> GetProduksiDetailsAsync(int year)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT PostingDate, ResourceName, PlannedHour, WageGroup, WeekOfBasicFinishedDate 
                                 FROM tbl_SAP_YR21
                                 WHERE YEAR(PostingDate) = @Year";
                var result = await conn.QueryAsync<SAP_YR21>(query, new { Year = year });
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
    }
}