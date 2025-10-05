# SQL Server接続用PowerShellスクリプト
$connectionString = "Server=192.168.1.19\SQLEXPRESS;Database=VcareDB;User Id=sa;Password=vivo0117##;"

# 接続とコマンドの作成
$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$connection.Open()

Write-Host "=== T_社員テーブルのカラム情報 ===" -ForegroundColor Green
$query1 = @"
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
    ORDINAL_POSITION
"@

$command = New-Object System.Data.SqlClient.SqlCommand($query1, $connection)
$reader = $command.ExecuteReader()

while ($reader.Read()) {
    Write-Host "カラム名: $($reader['COLUMN_NAME'])"
    Write-Host "  データ型: $($reader['DATA_TYPE'])"
    Write-Host "  最大長: $($reader['CHARACTER_MAXIMUM_LENGTH'])"
    Write-Host "  NULL許可: $($reader['IS_NULLABLE'])"
    Write-Host "  デフォルト: $($reader['COLUMN_DEFAULT'])"
    Write-Host "----------------------------------------"
}
$reader.Close()

Write-Host ""
Write-Host "=== T_システム管理台帳テーブルのカラム情報 ===" -ForegroundColor Green
$query2 = @"
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
    ORDINAL_POSITION
"@

$command = New-Object System.Data.SqlClient.SqlCommand($query2, $connection)
$reader = $command.ExecuteReader()

while ($reader.Read()) {
    Write-Host "カラム名: $($reader['COLUMN_NAME'])"
    Write-Host "  データ型: $($reader['DATA_TYPE'])"
    Write-Host "  最大長: $($reader['CHARACTER_MAXIMUM_LENGTH'])"
    Write-Host "  NULL許可: $($reader['IS_NULLABLE'])"
    Write-Host "  デフォルト: $($reader['COLUMN_DEFAULT'])"
    Write-Host "----------------------------------------"
}
$reader.Close()

$connection.Close()