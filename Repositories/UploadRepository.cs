using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using DashboardTeknikP1.Models;
using DashboardTeknikP1.Helpers;
using Dapper;

namespace DashboardTeknikP1.Repositories
{
    public class UploadRepository
    {
        private readonly string _connectionString;

        public UploadRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // HAPUS DATA LAMA SEBELUM UPLOAD DATA BARU
        public void TruncateTable(string tableName)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                if (tableName == "tbl_SAP_YP11" || tableName == "tbl_SAP_YR21" || tableName == "tbl_SAP_Sparepart" || tableName == "tbl_SAP_YP14")
                {
                    conn.Execute($"TRUNCATE TABLE {tableName}");
                }
            }
        }

        // 1. BULK INSERT YP11 (Downtime)
        public void InsertBulkYP11(List<SAP_YP11> dataList)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
                {
                    bulkCopy.DestinationTableName = "tbl_SAP_YP11";
                    var table = dataList.ToDataTable();
                    
                    var columnsToMap = new[] { "WeekKalendarIndofood", "FunctionLocation", "NotificationType", "NotificationDesc", "NotificationDate", "TotalDownTimeInMinutes", "DownTimeStartTime", "DownTimeEndTime", "ActivityText", "WageGroup_GroupShift", "MasterReceipt", "ProcessOrder", "WorkCenterPPDesc", "DownTimeCode_ActivityCodeDesc" };
                    foreach(var col in columnsToMap) bulkCopy.ColumnMappings.Add(col, col);

                    bulkCopy.WriteToServer(table);
                }
            }
        }

        // 2. BULK INSERT YR21 (Produksi & Jam Terencana)
        public void InsertBulkYR21(List<SAP_YR21> dataList)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
                {
                    bulkCopy.DestinationTableName = "tbl_SAP_YR21";
                    var table = dataList.ToDataTable();
                    
                    var columnsToMap = new[] { "WeekOfBasicFinishedDate", "PostingDate", "ResourceName", "WageGroup", "GroupName", "PlannedHour", "ActualHour", "StdOutputPcs", "DelivQtyPcs", "EffectivityPO_Pct", "Efficiency_Pct", "Ach_Pct" };
                    foreach(var col in columnsToMap) bulkCopy.ColumnMappings.Add(col, col);

                    bulkCopy.WriteToServer(table);
                }
            }
        }

        // 3. BULK INSERT SPAREPART (Suku Cadang & EWS)
        public void InsertBulkSparepart(List<SAP_Sparepart> dataList)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
                {
                    bulkCopy.DestinationTableName = "tbl_SAP_Sparepart";
                    var table = dataList.ToDataTable();
                    
                    var columnsToMap = new[] { "Plant", "Material", "MaterialDescription", "UoM", "MovingUnitPrice", "CurrentStock", "SafetyStock", "MatType", "StorLoct", "Priority" };
                    foreach(var col in columnsToMap) bulkCopy.ColumnMappings.Add(col, col);

                    bulkCopy.WriteToServer(table);
                }
            }
        }

        // 4. BULK INSERT YP14 (Actual Sparepart Cost)
        public void InsertBulkYP14(List<SAP_YP14> dataList)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
                {
                    bulkCopy.DestinationTableName = "tbl_SAP_YP14";
                    var table = dataList.ToDataTable();
                    
                    var columnsToMap = new[] { "OrderType", "OrderNo", "Description", "DocumentDate", "MaterialNo", "MaterialDescription", "Qty", "PricePerUnit", "UoM", "MaterialCost", "WorkCenter", "EquipmentDescription", "CostCenter", "FuncLoc" };
                    foreach(var col in columnsToMap) bulkCopy.ColumnMappings.Add(col, col);

                    bulkCopy.WriteToServer(table);
                }
            }
        }

        public List<string> GetExistingPriorities()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                return conn.Query<string>("SELECT Material FROM tbl_Sparepart_Priority WHERE Material IS NOT NULL").ToList();
            }
        }

        private void EnsureLogTableExists(SqlConnection conn)
        {
            var createTableQuery = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='tbl_Upload_Logs' AND xtype='U')
                BEGIN
                    CREATE TABLE tbl_Upload_Logs (
                        TableName VARCHAR(100) PRIMARY KEY,
                        LastUploadDate DATETIME
                    )
                END";
            conn.Execute(createTableQuery);
        }

        public Dictionary<string, DateTime?> GetLastUploadDates()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                EnsureLogTableExists(conn);
                var query = "SELECT TableName, LastUploadDate FROM tbl_Upload_Logs";
                var result = conn.Query(query).ToDictionary(
                    row => (string)row.TableName,
                    row => (DateTime?)row.LastUploadDate
                );
                return result;
            }
        }

        public void LogUpload(string tableName)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                EnsureLogTableExists(conn);
                var query = @"
                    IF EXISTS (SELECT 1 FROM tbl_Upload_Logs WHERE TableName = @TableName)
                        UPDATE tbl_Upload_Logs SET LastUploadDate = GETDATE() WHERE TableName = @TableName;
                    ELSE
                        INSERT INTO tbl_Upload_Logs (TableName, LastUploadDate) VALUES (@TableName, GETDATE());
                ";
                conn.Execute(query, new { TableName = tableName });
            }
        }
    }
}