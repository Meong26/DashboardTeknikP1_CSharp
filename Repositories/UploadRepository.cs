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
                if (tableName == "tbl_SAP_YP11" || tableName == "tbl_SAP_YR21" || tableName == "tbl_SAP_Sparepart")
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
                    // Karena data duplikat sudah disatukan di Controller C#, kita bisa langsung INSERT biasa
                    string query = @"INSERT INTO tbl_SAP_Sparepart 
                                    (PlantCode, MType, MatGrp, MatrGroupDescription, SLoc, MaterialNo, 
                                    MaterialNoDescription, TotalQtyStock, BUn, MvgAvgPriceIDR, 
                                    TotValuatedStockIDR, DateOfLastMvt, LamaTdkBergerakDay, StorBin, SafetyStock) 
                                    VALUES 
                                    (@Plant, @MType, @MatGrp, @MatDesc, @SLoc, @MatNo, @MatNoDesc, 
                                    @Qty, @BUn, @Price, @TotVal, @LastMvt, @SlowMoving, @StorBin, @Safety)";

                    using (SqlCommand cmd = new SqlCommand(query, conn, trans))
                    {
                        foreach (var data in dataList)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@Plant", data.PlantCode ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@MType", data.MType ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@MatGrp", data.MatGrp ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@MatDesc", data.MatrGroupDescription ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@SLoc", data.SLoc ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@MatNo", data.MaterialNo ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@MatNoDesc", data.MaterialNoDescription ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Qty", data.TotalQtyStock);
                            cmd.Parameters.AddWithValue("@BUn", data.BUn ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Price", data.MvgAvgPriceIDR);
                            cmd.Parameters.AddWithValue("@TotVal", data.TotValuatedStockIDR);
                            cmd.Parameters.AddWithValue("@LastMvt", data.DateOfLastMvt);
                            cmd.Parameters.AddWithValue("@SlowMoving", data.LamaTdkBergerakDay);
                            cmd.Parameters.AddWithValue("@StorBin", data.StorBin ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Safety", data.SafetyStock);

                            cmd.ExecuteNonQuery();
                        }
                    }
                    trans.Commit();
                }
            }
        }
    }
}