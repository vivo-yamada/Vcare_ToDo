-- セキュアな認証テーブルの作成
CREATE TABLE T_ユーザー認証 (
    ユーザーコード NVARCHAR(8) PRIMARY KEY,
    パスワードハッシュ NVARCHAR(255) NOT NULL,
    ソルト NVARCHAR(255) NOT NULL,
    作成日 DATETIME DEFAULT GETDATE(),
    最終ログイン DATETIME,
    ログイン失敗回数 INT DEFAULT 0,
    アカウントロック BIT DEFAULT 0,
    ロック解除時刻 DATETIME NULL,
    CONSTRAINT FK_ユーザー認証_社員 FOREIGN KEY (ユーザーコード) REFERENCES T_社員(コード)
);

-- インデックスの作成
CREATE INDEX IX_ユーザー認証_最終ログイン ON T_ユーザー認証(最終ログイン);
CREATE INDEX IX_ユーザー認証_アカウントロック ON T_ユーザー認証(アカウントロック);

-- 初期データの挿入例（実際の社員コードに合わせて修正してください）
-- パスワードは全て "password123" にハッシュ化されています
-- 本番環境では各ユーザーが個別にパスワードを設定してください

-- EXAMPLE: ユーザーコード '001' のパスワードを 'password123' に設定
-- INSERT INTO T_ユーザー認証 (ユーザーコード, パスワードハッシュ, ソルト)
-- VALUES ('001', '$2a$11$example_hash_here', '$2a$11$example_salt_here');

PRINT '認証テーブルが正常に作成されました。';
PRINT '初期ユーザーデータを手動で追加してください。';