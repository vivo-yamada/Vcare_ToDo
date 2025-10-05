import pyodbc
import json

# 接続文字列
conn_str = (
    'DRIVER={ODBC Driver 17 for SQL Server};'
    'SERVER=192.168.1.19\\SQLEXPRESS;'
    'DATABASE=VcareDB;'
    'UID=sa;'
    'PWD=vivo0117##'
)

try:
    # データベースに接続
    conn = pyodbc.connect(conn_str)
    cursor = conn.cursor()

    # T_社員テーブルのカラム情報を取得
    query_employee = """
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
    """

    print("=== T_社員テーブルのカラム情報 ===")
    cursor.execute(query_employee)
    columns = cursor.fetchall()

    for col in columns:
        print(f"カラム名: {col[0]}")
        print(f"  データ型: {col[1]}")
        print(f"  最大長: {col[2]}")
        print(f"  NULL許可: {col[3]}")
        print(f"  デフォルト: {col[4]}")
        print("-" * 40)

    # T_システム管理台帳のカラム情報を取得
    query_system = """
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
    """

    print("\n=== T_システム管理台帳テーブルのカラム情報 ===")
    cursor.execute(query_system)
    columns = cursor.fetchall()

    for col in columns:
        print(f"カラム名: {col[0]}")
        print(f"  データ型: {col[1]}")
        print(f"  最大長: {col[2]}")
        print(f"  NULL許可: {col[3]}")
        print(f"  デフォルト: {col[4]}")
        print("-" * 40)

    cursor.close()
    conn.close()

except Exception as e:
    print(f"エラーが発生しました: {e}")