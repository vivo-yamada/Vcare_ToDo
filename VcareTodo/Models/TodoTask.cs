namespace VcareTodo.Models
{
    public class TodoTask
    {
        public decimal ID { get; set; }
        public string? 内容 { get; set; }
        public string? 取引先コード { get; set; }
        public string? 取引先名 { get; set; }
        public string? 部署コード { get; set; }
        public string? 部署名 { get; set; }
        public int? PLANID { get; set; }
        public string? PlanName { get; set; }
        public decimal? SYSTEMID { get; set; }
        public string? SystemName { get; set; }
        public string? 対応方法 { get; set; }
        public decimal? 見積工数 { get; set; }
        public decimal? 実績工数 { get; set; }
        public decimal? 保守工数 { get; set; }
        public DateTime? 作業予定日 { get; set; }
        public DateTime? 修正完了日 { get; set; }
        public string? 起案者 { get; set; }
        public string? 担当者 { get; set; }
        public string? 分類 { get; set; }
        public string? 備考 { get; set; }
        public decimal? 分類コード { get; set; }
        public string? 状態 { get; set; }
        public DateTime? 起案日 { get; set; }
        public DateTime? 新規登録日 { get; set; }
    }
}