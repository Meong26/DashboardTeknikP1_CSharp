using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;

namespace DashboardTeknikP1.Helpers
{
    public static class DataTableHelper
    {
        public static DataTable ToDataTable<T>(this IEnumerable<T> data)
        {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(T));
            DataTable table = new DataTable();
            
            foreach (PropertyDescriptor prop in properties)
            {
                // Menangani tipe Nullable (misal int?, double?, DateTime?)
                table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            }
            
            if (data != null)
            {
                foreach (T item in data)
                {
                    DataRow row = table.NewRow();
                    foreach (PropertyDescriptor prop in properties)
                    {
                        row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
                    }
                    table.Rows.Add(row);
                }
            }
            return table;
        }
    }
}
