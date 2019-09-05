using System;

namespace Roadie.Dlna.Server
{
    public interface IVolatileMediaServer
    {
        bool Rescanning { get; set; }

        event EventHandler Changed;

        void Rescan();
    }
}