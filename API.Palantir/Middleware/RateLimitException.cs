namespace API.Palantir.Middleware
{
    public class RateLimitException : Exception
    {
        public RateLimitException(string message) : base(message) { }
    }
}
