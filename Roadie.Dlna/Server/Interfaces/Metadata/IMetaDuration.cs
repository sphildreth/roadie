using System;

namespace Roadie.Dlna.Server.Metadata
{
    public interface IMetaDuration
    {
        TimeSpan? MetaDuration { get; }
    }
}