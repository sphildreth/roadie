using System;

namespace Roadie.Dlna.Server.Metadata
{
    public interface IMetaInfo
    {
        DateTime InfoDate { get; }

        long? InfoSize { get; }
    }
}