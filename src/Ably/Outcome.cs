namespace Ably
{
    public class Outcome
    {
        public bool Successful { get; set; }

        public ErrorInfo Error { get; set; }

    
        public static Outcome Fail(ErrorInfo error)
        {
            return new Outcome
            {
                Successful = false,
                Error = error
            };
        }

        public static Outcome Success()
        {
            return new Outcome
            {
                Successful = true
            };
        }
    }
}
