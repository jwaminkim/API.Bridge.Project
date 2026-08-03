namespace API.Palantir.Models
{
    public class ErrorModel
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow; // 에러 발생 시간 (UTC 권장)
        public int StatusCode { get; set; }                         // HTTP 상태 코드
        public string ErrorId { get; set; }                         // 고유 에러 ID (예: 로그 추적용 GUID)	
        public string? StackTrace { get; set; }                     // 개발자용 스택 트레이스 (개발환경에서만 노출 권장)
        public string? Source { get; set; }                         // 예외 발생 위치 (ex.TargetSite?.DeclaringType?.FullName)
        public string? RequestPath { get; set; }                     // 요청 경로
        public string? Method { get; set; }                          // 요청 메서드 (GET, POST 등)
        public string? QueryString { get; set; }                    // Get QueryString
        public string? RequestBody { get; set; }                    // Post Body
        public List<string>? Messages { get; set; } = new();        // Validation 실패 등 다중 메시지	
    }
}
