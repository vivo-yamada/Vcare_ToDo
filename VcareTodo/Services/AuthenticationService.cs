using VcareTodo.Data;
using VcareTodo.Models;
using BCrypt.Net;

namespace VcareTodo.Services
{
    public class AuthenticationService
    {
        private readonly DatabaseContext _db;
        private const int MaxLoginAttempts = 5;
        private const int LockoutDurationMinutes = 30;

        public AuthenticationService(DatabaseContext db)
        {
            _db = db;
        }

        /// <summary>
        /// パスワードをハッシュ化（新規登録用）
        /// </summary>
        public (string hashedPassword, string salt) HashPassword(string password)
        {
            var salt = BCrypt.Net.BCrypt.GenerateSalt(12);
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, salt);
            return (hashedPassword, salt);
        }

        /// <summary>
        /// パスワードをハッシュ化して新しいユーザー認証情報を作成
        /// </summary>
        public async Task<bool> CreateUserAuthenticationAsync(string userCode, string password)
        {
            try
            {
                var salt = BCrypt.Net.BCrypt.GenerateSalt(12);
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, salt);

                var query = @"
                    INSERT INTO T_ユーザー認証 (ユーザーコード, パスワードハッシュ, ソルト, 作成日)
                    VALUES (@ユーザーコード, @パスワードハッシュ, @ソルト, GETDATE())";

                await _db.ExecuteAsync(query, new
                {
                    ユーザーコード = userCode,
                    パスワードハッシュ = hashedPassword,
                    ソルト = salt
                });

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// ユーザー認証を実行
        /// </summary>
        public async Task<(bool Success, string Message, Employee? Employee)> AuthenticateAsync(string userCode, string password)
        {
            // 社員情報を取得
            var employeeQuery = @"
                SELECT コード, 氏名, メールアドレス
                FROM T_社員
                WHERE コード = @UserCode";

            var employee = await _db.QueryFirstOrDefaultAsync<Employee>(employeeQuery, new { UserCode = userCode });

            if (employee == null)
            {
                return (false, "ユーザーIDまたはパスワードが正しくありません。", null);
            }

            // 認証情報を取得
            var authQuery = @"
                SELECT ユーザーコード, パスワードハッシュ, ソルト, ログイン失敗回数, アカウントロック, ロック解除時刻
                FROM T_ユーザー認証
                WHERE ユーザーコード = @UserCode";

            var auth = await _db.QueryFirstOrDefaultAsync<UserAuthentication>(authQuery, new { UserCode = userCode });

            if (auth == null)
            {
                return (false, "ユーザーIDまたはパスワードが正しくありません。", null);
            }

            // アカウントロック確認
            if (auth.アカウントロック)
            {
                if (auth.ロック解除時刻.HasValue && DateTime.Now < auth.ロック解除時刻.Value)
                {
                    var remainingMinutes = (int)(auth.ロック解除時刻.Value - DateTime.Now).TotalMinutes;
                    return (false, $"アカウントがロックされています。{remainingMinutes}分後に再試行してください。", null);
                }
                else if (auth.ロック解除時刻.HasValue && DateTime.Now >= auth.ロック解除時刻.Value)
                {
                    // ロック解除
                    await UnlockAccountAsync(userCode);
                    auth.アカウントロック = false;
                    auth.ログイン失敗回数 = 0;
                }
            }

            // パスワード検証
            bool passwordValid = BCrypt.Net.BCrypt.Verify(password, auth.パスワードハッシュ);

            if (passwordValid)
            {
                // ログイン成功 - 失敗回数をリセット、最終ログイン時刻を更新
                await UpdateLoginSuccessAsync(userCode);
                return (true, "ログインが成功しました。", employee);
            }
            else
            {
                // ログイン失敗 - 失敗回数を増加
                await UpdateLoginFailureAsync(userCode);

                var newFailureCount = auth.ログイン失敗回数 + 1;
                if (newFailureCount >= MaxLoginAttempts)
                {
                    await LockAccountAsync(userCode);
                    return (false, $"ログイン失敗回数が上限に達しました。アカウントを{LockoutDurationMinutes}分間ロックします。", null);
                }
                else
                {
                    var remainingAttempts = MaxLoginAttempts - newFailureCount;
                    return (false, $"ユーザーIDまたはパスワードが正しくありません。残り{remainingAttempts}回の試行が可能です。", null);
                }
            }
        }

        /// <summary>
        /// パスワードを変更
        /// </summary>
        public async Task<bool> ChangePasswordAsync(string userCode, string oldPassword, string newPassword)
        {
            var authQuery = @"
                SELECT パスワードハッシュ
                FROM T_ユーザー認証
                WHERE ユーザーコード = @UserCode";

            var currentHash = await _db.QueryFirstOrDefaultAsync<string>(authQuery, new { UserCode = userCode });

            if (currentHash == null || !BCrypt.Net.BCrypt.Verify(oldPassword, currentHash))
            {
                return false;
            }

            var salt = BCrypt.Net.BCrypt.GenerateSalt(12);
            var newHashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword, salt);

            var updateQuery = @"
                UPDATE T_ユーザー認証
                SET パスワードハッシュ = @パスワードハッシュ, ソルト = @ソルト
                WHERE ユーザーコード = @ユーザーコード";

            await _db.ExecuteAsync(updateQuery, new
            {
                パスワードハッシュ = newHashedPassword,
                ソルト = salt,
                ユーザーコード = userCode
            });

            return true;
        }

        private async Task UpdateLoginSuccessAsync(string userCode)
        {
            var query = @"
                UPDATE T_ユーザー認証
                SET 最終ログイン = GETDATE(), ログイン失敗回数 = 0, アカウントロック = 0, ロック解除時刻 = NULL
                WHERE ユーザーコード = @UserCode";

            await _db.ExecuteAsync(query, new { UserCode = userCode });
        }

        private async Task UpdateLoginFailureAsync(string userCode)
        {
            var query = @"
                UPDATE T_ユーザー認証
                SET ログイン失敗回数 = ログイン失敗回数 + 1
                WHERE ユーザーコード = @UserCode";

            await _db.ExecuteAsync(query, new { UserCode = userCode });
        }

        private async Task LockAccountAsync(string userCode)
        {
            var lockUntil = DateTime.Now.AddMinutes(LockoutDurationMinutes);
            var query = @"
                UPDATE T_ユーザー認証
                SET アカウントロック = 1, ロック解除時刻 = @LockUntil
                WHERE ユーザーコード = @UserCode";

            await _db.ExecuteAsync(query, new { UserCode = userCode, LockUntil = lockUntil });
        }

        private async Task UnlockAccountAsync(string userCode)
        {
            var query = @"
                UPDATE T_ユーザー認証
                SET アカウントロック = 0, ログイン失敗回数 = 0, ロック解除時刻 = NULL
                WHERE ユーザーコード = @UserCode";

            await _db.ExecuteAsync(query, new { UserCode = userCode });
        }
    }
}