using System;
using System.Data.SqlClient;
using System.Text;

class GetTableInfo
{
    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        string connectionString = @"Server=192.168.1.19\SQLEXPRESS;Database=VcareDB;User Id=sa;Password=vivo0117##;";

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                Console.WriteLine("=== T_社員テーブルのカラム情報 ===");

                string query1 = @"
                    SELECT
                        COLUMN_NAME,
                        DATA_TYPE,
                        CHARACTER_MAXIMUM_LENGTH,
                        IS_NULLABLE,
                        COLUMN_DEFAULT
                    FROM
                        INFORMATION_SCHEMA.COLUMNS
                    WHERE
                        TABLE_NAME = N'T_社員'
                    ORDER BY
                        ORDINAL_POSITION";

                using (SqlCommand command = new SqlCommand(query1, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Console.WriteLine($"カラム名: {reader["COLUMN_NAME"]}");
                        Console.WriteLine($"  データ型: {reader["DATA_TYPE"]}");
                        Console.WriteLine($"  最大長: {reader["CHARACTER_MAXIMUM_LENGTH"]}");
                        Console.WriteLine($"  NULL許可: {reader["IS_NULLABLE"]}");
                        Console.WriteLine($"  デフォルト: {reader["COLUMN_DEFAULT"]}");
                        Console.WriteLine("----------------------------------------");
                    }
                }

                Console.WriteLine("\n=== T_システム管理台帳テーブルのカラム情報 ===");

                string query2 = @"
                    SELECT
                        COLUMN_NAME,
                        DATA_TYPE,
                        CHARACTER_MAXIMUM_LENGTH,
                        IS_NULLABLE,
                        COLUMN_DEFAULT
                    FROM
                        INFORMATION_SCHEMA.COLUMNS
                    WHERE
                        TABLE_NAME = N'T_システム管理台帳'
                    ORDER BY
                        ORDINAL_POSITION";

                using (SqlCommand command = new SqlCommand(query2, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Console.WriteLine($"カラム名: {reader["COLUMN_NAME"]}");
                        Console.WriteLine($"  データ型: {reader["DATA_TYPE"]}");
                        Console.WriteLine($"  最大長: {reader["CHARACTER_MAXIMUM_LENGTH"]}");
                        Console.WriteLine($"  NULL許可: {reader["IS_NULLABLE"]}");
                        Console.WriteLine($"  デフォルト: {reader["COLUMN_DEFAULT"]}");
                        Console.WriteLine("----------------------------------------");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"エラー: {ex.Message}");
            }
        }
    }
}