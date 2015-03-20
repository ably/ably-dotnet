using System.Collections;
using System.Collections.Generic;

namespace Ably
{
    public static class Defaults
    {
        public const int Limit = 100;
    }

    public interface IPartialResult<out T> : IEnumerable<T>
    {
        bool HasNext { get; }
        DataRequestQuery NextQuery { get; }
        DataRequestQuery InitialResultQuery { get; }
        DataRequestQuery CurrentResultQuery { get; }
    }

    public class PartialResult<T> : List<T>, IPartialResult<T>
    {
        private readonly int _limit;

        public PartialResult(int limit = Defaults.Limit)
        {
            _limit = limit;
        }

        public bool HasNext { get { return Count > _limit; } }
        public DataRequestQuery NextQuery { get; set; }
        public DataRequestQuery InitialResultQuery { get; set; }
        public DataRequestQuery CurrentResultQuery { get; set; }
    }
}