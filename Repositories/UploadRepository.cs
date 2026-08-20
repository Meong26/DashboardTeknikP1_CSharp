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

        public async Task EnsureIndexesExistAsync(SqlConnection conn)
        {
            string indexQuery = @"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SAP_YR21_PostingDate_Resource' AND object_id = OBJECT_ID('tbl_SAP_YR21'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_SAP_YR21_PostingDate_Resource ON tbl_SAP_YR21 (PostingDate, ResourceName) INCLUDE (WageGroup, GroupName, PlannedHour, ActualHour, DelivQtyPcs);
                END
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SAP_YP11_NotificationDate_FuncLoc' AND object_id = OBJECT_ID('tbl_SAP_YP11'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_SAP_YP11_NotificationDate_FuncLoc ON tbl_SAP_YP11 (NotificationDate, FunctionLocation) INCLUDE (NotificationDesc, TotalDownTimeInMinutes, DownTimeStartTime, DownTimeEndTime);
                END
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SAP_YP14_DocumentDate_OrderNo' AND object_id = OBJECT_ID('tbl_SAP_YP14'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_SAP_YP14_DocumentDate_OrderNo ON tbl_SAP_YP14 (DocumentDate, OrderNo) INCLUDE (MaterialNo, Qty, PricePerUnit, MaterialCost, WorkCenter);
                END
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SAP_Sparepart_Material' AND object_id = OBJECT_ID('tbl_SAP_Sparepart'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_SAP_Sparepart_Material ON tbl_SAP_Sparepart (Material) INCLUDE (CurrentStock, MovingUnitPrice, Priority);
                END
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PengambilanSparepart_Tanggal' AND object_id = OBJECT_ID('tbl_PengambilanSparepart'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_PengambilanSparepart_Tanggal ON tbl_PengambilanSparepart (TanggalPengambilan) INCLUDE (MaterialNo, JumlahPengambilan, TotalHarga, Status, Plant);
                END";
            await conn.ExecuteAsync(indexQuery);
        }

        // 1. INKREMENTAL BULK INSERT YP11 (Downtime - Skip Identik & Sargable)
        public async Task InsertBulkYP11Async(List<SAP_YP11> dataList)
        {
            if (dataList == null || !dataList.Any()) return;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                await EnsureIndexesExistAsync(conn);

                string createStagingQuery = @"
                    CREATE TABLE #Staging_YP11 (
                        WeekKalendarIndofood NVARCHAR(50),
                        FunctionLocation NVARCHAR(100),
                        NotificationType NVARCHAR(50),
                        NotificationDesc NVARCHAR(500),
                        NotificationDate DATETIME,
                        TotalDownTimeInMinutes FLOAT,
                        DownTimeStartTime DATETIME,
                        DownTimeEndTime DATETIME,
                        ActivityText NVARCHAR(500),
                        WageGroup_GroupShift NVARCHAR(50),
                        MasterReceipt NVARCHAR(100),
                        ProcessOrder NVARCHAR(100),
                        WorkCenterPPDesc NVARCHAR(200),
                        DownTimeCode_ActivityCodeDesc NVARCHAR(500)
                    );";
                await conn.ExecuteAsync(createStagingQuery);

                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
                {
                    bulkCopy.DestinationTableName = "#Staging_YP11";
                    var table = dataList.ToDataTable();

                    var columnsToMap = new[] { "WeekKalendarIndofood", "FunctionLocation", "NotificationType", "NotificationDesc", "NotificationDate", "TotalDownTimeInMinutes", "DownTimeStartTime", "DownTimeEndTime", "ActivityText", "WageGroup_GroupShift", "MasterReceipt", "ProcessOrder", "WorkCenterPPDesc", "DownTimeCode_ActivityCodeDesc" };
                    foreach (var col in columnsToMap) bulkCopy.ColumnMappings.Add(col, col);

                    await bulkCopy.WriteToServerAsync(table);
                }

                string mergeInsertQuery = @"
                    -- Hapus data pada rentang tanggal yang ada di file Excel
                    DECLARE @MinDate DATE = (SELECT MIN(NotificationDate) FROM #Staging_YP11);
                    DECLARE @MaxDate DATE = (SELECT MAX(NotificationDate) FROM #Staging_YP11);
                    
                    IF @MinDate IS NOT NULL AND @MaxDate IS NOT NULL
                    BEGIN
                        DELETE FROM tbl_SAP_YP11 
                        WHERE NotificationDate >= @MinDate AND NotificationDate <= @MaxDate;
                    END

                    -- Insert semua data baru dari Excel
                    INSERT INTO tbl_SAP_YP11 (
                        WeekKalendarIndofood, FunctionLocation, NotificationType, NotificationDesc, NotificationDate, 
                        TotalDownTimeInMinutes, DownTimeStartTime, DownTimeEndTime, ActivityText, WageGroup_GroupShift, 
                        MasterReceipt, ProcessOrder, WorkCenterPPDesc, DownTimeCode_ActivityCodeDesc
                    )
                    SELECT 
                        s.WeekKalendarIndofood, s.FunctionLocation, s.NotificationType, s.NotificationDesc, s.NotificationDate, 
                        s.TotalDownTimeInMinutes, s.DownTimeStartTime, s.DownTimeEndTime, s.ActivityText, s.WageGroup_GroupShift, 
                        s.MasterReceipt, s.ProcessOrder, s.WorkCenterPPDesc, s.DownTimeCode_ActivityCodeDesc
                    FROM #Staging_YP11 s;

                    DROP TABLE #Staging_YP11;";

                await conn.ExecuteAsync(mergeInsertQuery);
            }
        }

        // 2. INKREMENTAL BULK INSERT YR21 (Produksi & Jam Terencana - Skip Identik & Sargable)
        public async Task InsertBulkYR21Async(List<SAP_YR21> dataList)
        {
            if (dataList == null || !dataList.Any()) return;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                await EnsureIndexesExistAsync(conn);

                string createStagingQuery = @"
                    CREATE TABLE #Staging_YR21 (
                        WeekOfBasicFinishedDate NVARCHAR(50),
                        PostingDate DATETIME,
                        ResourceName NVARCHAR(100),
                        WageGroup NVARCHAR(50),
                        GroupName NVARCHAR(100),
                        PlannedHour FLOAT,
                        ActualHour FLOAT,
                        StdOutputPcs FLOAT,
                        DelivQtyPcs FLOAT,
                        EffectivityPO_Pct FLOAT,
                        Efficiency_Pct FLOAT,
                        Ach_Pct FLOAT
                    );";
                await conn.ExecuteAsync(createStagingQuery);

                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
                {
                    bulkCopy.DestinationTableName = "#Staging_YR21";
                    var table = dataList.ToDataTable();

                    var columnsToMap = new[] { "WeekOfBasicFinishedDate", "PostingDate", "ResourceName", "WageGroup", "GroupName", "PlannedHour", "ActualHour", "StdOutputPcs", "DelivQtyPcs", "EffectivityPO_Pct", "Efficiency_Pct", "Ach_Pct" };
                    foreach (var col in columnsToMap) bulkCopy.ColumnMappings.Add(col, col);

                    await bulkCopy.WriteToServerAsync(table);
                }

                string mergeInsertQuery = @"
                    -- Hapus data pada rentang tanggal yang ada di file Excel
                    DECLARE @MinDate DATE = (SELECT MIN(PostingDate) FROM #Staging_YR21);
                    DECLARE @MaxDate DATE = (SELECT MAX(PostingDate) FROM #Staging_YR21);
                    
                    IF @MinDate IS NOT NULL AND @MaxDate IS NOT NULL
                    BEGIN
                        DELETE FROM tbl_SAP_YR21 
                        WHERE PostingDate >= @MinDate AND PostingDate <= @MaxDate;
                    END

                    -- Insert semua data baru dari Excel
                    INSERT INTO tbl_SAP_YR21 (
                        WeekOfBasicFinishedDate, PostingDate, ResourceName, WageGroup, GroupName, 
                        PlannedHour, ActualHour, StdOutputPcs, DelivQtyPcs, EffectivityPO_Pct, Efficiency_Pct, Ach_Pct
                    )
                    SELECT 
                        s.WeekOfBasicFinishedDate, s.PostingDate, s.ResourceName, s.WageGroup, s.GroupName, 
                        s.PlannedHour, s.ActualHour, s.StdOutputPcs, s.DelivQtyPcs, s.EffectivityPO_Pct, s.Efficiency_Pct, s.Ach_Pct
                    FROM #Staging_YR21 s;

                    DROP TABLE #Staging_YR21;";

                await conn.ExecuteAsync(mergeInsertQuery);
            }
        }

        // 3. UPSERT SPAREPART (Update Stok Baru jika Material ada, Insert jika Material baru)
        public async Task InsertBulkSparepartAsync(List<SAP_Sparepart> dataList)
        {
            if (dataList == null || !dataList.Any()) return;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                await EnsureIndexesExistAsync(conn);

                string createStagingQuery = @"
                    CREATE TABLE #Staging_Sparepart (
                        Plant NVARCHAR(50),
                        Material NVARCHAR(100),
                        MaterialDescription NVARCHAR(500),
                        UoM NVARCHAR(50),
                        MovingUnitPrice DECIMAL(18, 2),
                        CurrentStock FLOAT,
                        SafetyStock INT,
                        MatType NVARCHAR(50),
                        StorLoct NVARCHAR(50),
                        Priority NVARCHAR(10)
                    );";
                await conn.ExecuteAsync(createStagingQuery);

                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
                {
                    bulkCopy.DestinationTableName = "#Staging_Sparepart";
                    var table = dataList.ToDataTable();

                    var columnsToMap = new[] { "Plant", "Material", "MaterialDescription", "UoM", "MovingUnitPrice", "CurrentStock", "SafetyStock", "MatType", "StorLoct", "Priority" };
                    foreach (var col in columnsToMap) bulkCopy.ColumnMappings.Add(col, col);

                    await bulkCopy.WriteToServerAsync(table);
                }

                string mergeUpsertQuery = @"
                    MERGE tbl_SAP_Sparepart AS target
                    USING #Staging_Sparepart AS source
                    ON (target.Material = source.Material)
                    WHEN MATCHED THEN
                        UPDATE SET 
                            target.Plant = source.Plant,
                            target.MaterialDescription = source.MaterialDescription,
                            target.UoM = source.UoM,
                            target.MovingUnitPrice = source.MovingUnitPrice,
                            target.CurrentStock = source.CurrentStock,
                            target.SafetyStock = source.SafetyStock,
                            target.MatType = source.MatType,
                            target.StorLoct = source.StorLoct,
                            target.Priority = ISNULL(source.Priority, target.Priority)
                    WHEN NOT MATCHED THEN
                        INSERT (Plant, Material, MaterialDescription, UoM, MovingUnitPrice, CurrentStock, SafetyStock, MatType, StorLoct, Priority)
                        VALUES (source.Plant, source.Material, source.MaterialDescription, source.UoM, source.MovingUnitPrice, source.CurrentStock, source.SafetyStock, source.MatType, source.StorLoct, source.Priority);

                    DROP TABLE #Staging_Sparepart;";

                await conn.ExecuteAsync(mergeUpsertQuery);
            }
        }

        // 4. INKREMENTAL BULK INSERT YP14 (Actual Sparepart Cost - Skip Identik & Sargable)
        public async Task InsertBulkYP14Async(List<SAP_YP14> dataList)
        {
            if (dataList == null || !dataList.Any()) return;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                await EnsureIndexesExistAsync(conn);

                string createStagingQuery = @"
                    CREATE TABLE #Staging_YP14 (
                        OrderType NVARCHAR(50),
                        OrderNo NVARCHAR(100),
                        Description NVARCHAR(500),
                        DocumentDate DATETIME,
                        MaterialNo NVARCHAR(100),
                        MaterialDescription NVARCHAR(500),
                        Qty FLOAT,
                        PricePerUnit DECIMAL(18, 2),
                        UoM NVARCHAR(50),
                        MaterialCost DECIMAL(18, 2),
                        WorkCenter NVARCHAR(100),
                        EquipmentDescription NVARCHAR(500),
                        CostCenter NVARCHAR(100),
                        FuncLoc NVARCHAR(100)
                    );";
                await conn.ExecuteAsync(createStagingQuery);

                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
                {
                    bulkCopy.DestinationTableName = "#Staging_YP14";
                    var table = dataList.ToDataTable();

                    var columnsToMap = new[] { "OrderType", "OrderNo", "Description", "DocumentDate", "MaterialNo", "MaterialDescription", "Qty", "PricePerUnit", "UoM", "MaterialCost", "WorkCenter", "EquipmentDescription", "CostCenter", "FuncLoc" };
                    foreach (var col in columnsToMap) bulkCopy.ColumnMappings.Add(col, col);

                    await bulkCopy.WriteToServerAsync(table);
                }

                string mergeInsertQuery = @"
                    INSERT INTO tbl_SAP_YP14 (
                        OrderType, OrderNo, Description, DocumentDate, MaterialNo, MaterialDescription, 
                        Qty, PricePerUnit, UoM, MaterialCost, WorkCenter, EquipmentDescription, CostCenter, FuncLoc
                    )
                    SELECT 
                        s.OrderType, s.OrderNo, s.Description, s.DocumentDate, s.MaterialNo, s.MaterialDescription, 
                        s.Qty, s.PricePerUnit, s.UoM, s.MaterialCost, s.WorkCenter, s.EquipmentDescription, s.CostCenter, s.FuncLoc
                    FROM #Staging_YP14 s
                    WHERE NOT EXISTS (
                        SELECT 1 FROM tbl_SAP_YP14 t
                        WHERE (t.OrderNo = s.OrderNo OR (t.OrderNo IS NULL AND s.OrderNo IS NULL))
                          AND (t.DocumentDate = s.DocumentDate OR (t.DocumentDate IS NULL AND s.DocumentDate IS NULL))
                          AND (t.MaterialNo = s.MaterialNo OR (t.MaterialNo IS NULL AND s.MaterialNo IS NULL))
                          AND (t.Qty = s.Qty OR (t.Qty IS NULL AND s.Qty IS NULL))
                          AND (t.PricePerUnit = s.PricePerUnit OR (t.PricePerUnit IS NULL AND s.PricePerUnit IS NULL))
                          AND (t.MaterialCost = s.MaterialCost OR (t.MaterialCost IS NULL AND s.MaterialCost IS NULL))
                          AND (t.WorkCenter = s.WorkCenter OR (t.WorkCenter IS NULL AND s.WorkCenter IS NULL))
                    );

                    DROP TABLE #Staging_YP14;";

                await conn.ExecuteAsync(mergeInsertQuery);
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