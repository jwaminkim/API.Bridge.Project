namespace API.Palantir.Models
{
    public class ResponseModel<T>
    {
        public bool IsSuccess { get; set; } = false;
        public T? ReturnData { get; set; }
        public List<string>? Message { get; set; } = new();
        public string? ErrorId { get; set; }
    }
}
