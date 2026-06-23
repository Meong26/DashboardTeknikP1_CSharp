using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using DashboardTeknikP1.Models;

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
                    string query = $"TRUNCATE TABLE {tableName}";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        // 1. BULK INSERT YP11 (Downtime)
        public void InsertBulkYP11(List<SAP_YP11> dataList)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                // Menggunakan Transaction agar eksekusi ribuan baris disatukan (jauh lebih cepat)
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    string query = @"INSERT INTO tbl_SAP_YP11 
                                    (WeekKalendarIndofood, FunctionLocation, NotificationType, NotificationDesc, 
                                    NotificationDate, TotalDownTimeInMinutes, DownTimeStartTime, DownTimeEndTime, 
                                    ActivityText, WageGroup_GroupShift, MasterReceipt, ProcessOrder, 
                                    WorkCenterPPDesc, DownTimeCode_ActivityCodeDesc) 
                                    VALUES 
                                    (@Week, @FuncLoc, @Type, @Desc, @Date, @TotalMin, @StartTime, @EndTime, 
                                    @Activity, @WageGroup, @Receipt, @Order, @WorkCenter, @DownTimeCode)";

                    using (SqlCommand cmd = new SqlCommand(query, conn, trans))
                    {
                        foreach (var data in dataList)
                        {
                            cmd.Parameters.Clear(); // Bersihkan parameter untuk baris berikutnya
                            cmd.Parameters.AddWithValue("@Week", data.WeekKalendarIndofood ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@FuncLoc", data.FunctionLocation ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Type", data.NotificationType ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Desc", data.NotificationDesc ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Date", data.NotificationDate);
                            cmd.Parameters.AddWithValue("@TotalMin", data.TotalDownTimeInMinutes);
                            cmd.Parameters.AddWithValue("@StartTime", data.DownTimeStartTime);
                            cmd.Parameters.AddWithValue("@EndTime", data.DownTimeEndTime);
                            cmd.Parameters.AddWithValue("@Activity", data.ActivityText ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@WageGroup", data.WageGroup_GroupShift ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Receipt", data.MasterReceipt ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Order", data.ProcessOrder ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@WorkCenter", data.WorkCenterPPDesc ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@DownTimeCode", data.DownTimeCode_ActivityCodeDesc ?? (object)DBNull.Value);

                            cmd.ExecuteNonQuery();
                        }
                    }
                    trans.Commit(); // Kunci semua data masuk ke database sekaligus
                }
            }
        }

        // 2. BULK INSERT YR21 (Produksi & Jam Terencana)
        public void InsertBulkYR21(List<SAP_YR21> dataList)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    string query = @"INSERT INTO tbl_SAP_YR21 
                                    (WeekOfBasicFinishedDate, PostingDate, ResourceName, WageGroup, GroupName, 
                                    PlannedHour, ActualHour, StdOutputPcs, DelivQtyPcs, EffectivityPO_Pct, 
                                    Efficiency_Pct, Ach_Pct) 
                                    VALUES 
                                    (@Week, @PostingDate, @Resource, @Wage, @Group, @Planned, @Actual, 
                                    @StdOut, @Deliv, @Effectivity, @Efficiency, @Ach)";

                    using (SqlCommand cmd = new SqlCommand(query, conn, trans))
                    {
                        foreach (var data in dataList)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@Week", data.WeekOfBasicFinishedDate ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@PostingDate", data.PostingDate);
                            cmd.Parameters.AddWithValue("@Resource", data.ResourceName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Wage", data.WageGroup ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Group", data.GroupName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Planned", data.PlannedHour);
                            cmd.Parameters.AddWithValue("@Actual", data.ActualHour);
                            cmd.Parameters.AddWithValue("@StdOut", data.StdOutputPcs);
                            cmd.Parameters.AddWithValue("@Deliv", data.DelivQtyPcs);
                            cmd.Parameters.AddWithValue("@Effectivity", data.EffectivityPO_Pct);
                            cmd.Parameters.AddWithValue("@Efficiency", data.Efficiency_Pct);
                            cmd.Parameters.AddWithValue("@Ach", data.Ach_Pct);

                            cmd.ExecuteNonQuery();
                        }
                    }
                    trans.Commit();
                }
            }
        }

        // 3. BULK INSERT SPAREPART (Suku Cadang & EWS)
        public void InsertBulkSparepart(List<SAP_Sparepart> dataList)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    string query = @"INSERT INTO tbl_SAP_Sparepart 
                                    (Plant, Material, MaterialDescription, UoM, MovingUnitPrice, 
                                     CurrentStock, SafetyStock, MatType, StorLoct, Priority) 
                                    VALUES 
                                    (@Plant, @Material, @MaterialDescription, @UoM, @MovingUnitPrice, 
                                     @CurrentStock, @SafetyStock, @MatType, @StorLoct, @Priority)";
                                    
                    using (SqlCommand cmd = new SqlCommand(query, conn, trans))
                    {
                        foreach (var data in dataList)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@Plant", data.Plant ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Material", data.Material ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@MaterialDescription", data.MaterialDescription ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@UoM", data.UoM ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@MovingUnitPrice", data.MovingUnitPrice);
                            cmd.Parameters.AddWithValue("@CurrentStock", data.CurrentStock);
                            cmd.Parameters.AddWithValue("@SafetyStock", data.SafetyStock);
                            cmd.Parameters.AddWithValue("@MatType", data.MatType ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@StorLoct", data.StorLoct ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Priority", string.IsNullOrEmpty(data.Priority) ? (object)DBNull.Value : data.Priority);

                            cmd.ExecuteNonQuery();
                        }
                    }
                    trans.Commit();
                }
            }
        }

        // 4. BULK INSERT YP14 (Actual Sparepart Cost)
        public void InsertBulkYP14(List<SAP_YP14> dataList)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    string query = @"INSERT INTO tbl_SAP_YP14 
                                    (OrderType, OrderNo, Description, DocumentDate, MaterialNo, MaterialDescription, 
                                     Qty, PricePerUnit, UoM, MaterialCost, WorkCenter, EquipmentDescription, CostCenter) 
                                    VALUES 
                                    (@Type, @OrderNo, @Desc, @DocDate, @MatNo, @MatDesc, 
                                     @Qty, @Price, @UoM, @MatCost, @WorkCenter, @EquipDesc, @CostCenter)";

                    using (SqlCommand cmd = new SqlCommand(query, conn, trans))
                    {
                        foreach (var data in dataList)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@Type", data.OrderType ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@OrderNo", data.OrderNo ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Desc", data.Description ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@DocDate", data.DocumentDate);
                            cmd.Parameters.AddWithValue("@MatNo", data.MaterialNo ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@MatDesc", data.MaterialDescription ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Qty", data.Qty);
                            cmd.Parameters.AddWithValue("@Price", data.PricePerUnit);
                            cmd.Parameters.AddWithValue("@UoM", data.UoM ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@MatCost", data.MaterialCost);
                            cmd.Parameters.AddWithValue("@WorkCenter", data.WorkCenter ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@EquipDesc", data.EquipmentDescription ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@CostCenter", data.CostCenter ?? (object)DBNull.Value);

                            cmd.ExecuteNonQuery();
                        }
                    }
                    trans.Commit();
                }
            }
        }

        public List<string> GetExistingPriorities()
        {
            var list = new List<string>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT Material FROM tbl_SAP_Sparepart WHERE Priority = 'Y'";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader["Material"] != DBNull.Value)
                                list.Add(reader["Material"].ToString().Trim());
                        }
                    }
                }
            }
            return list;
        }
    }
}