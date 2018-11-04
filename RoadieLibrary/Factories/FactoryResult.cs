using System.Collections.Generic;

namespace Roadie.Library.Factories
{
    public class FactoryResult<T>
    {
        public T Data { get; set; }
        public IEnumerable<string> Errors { get; set; }
        public bool IsSuccess { get; set; }
        public long OperationTime { get; set; }
    }
}