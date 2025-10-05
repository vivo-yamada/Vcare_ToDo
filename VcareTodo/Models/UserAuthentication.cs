namespace VcareTodo.Models
{
    public class UserAuthentication
    {
        public string ユーザーコード { get; set; } = string.Empty;
        public string パスワードハッシュ { get; set; } = string.Empty;
        public string ソルト { get; set; } = string.Empty;
        public DateTime 作成日 { get; set; }
        public DateTime? 最終ログイン { get; set; }
        public int ログイン失敗回数 { get; set; } = 0;
        public bool アカウントロック { get; set; } = false;
        public DateTime? ロック解除時刻 { get; set; }
    }
}