using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Roadie.Dlna.Utility
{
    public sealed class StreamPump : IDisposable
    {
        private readonly byte[] buffer;

        private readonly SemaphoreSlim sem = new SemaphoreSlim(0, 1);

        public Stream Input { get; }

        public Stream Output { get; }

        public StreamPump(Stream inputStream, Stream outputStream, int bufferSize)
        {
            buffer = new byte[bufferSize];
            Input = inputStream;
            Output = outputStream;
        }

        public void Dispose()
        {
            sem.Dispose();
        }

        public void Pump(StreamPumpCallback callback)
        {
            try
            {
                Input.BeginRead(buffer, 0, buffer.Length, readResult =>
                {
                    try
                    {
                        var read = Input.EndRead(readResult);
                        if (read <= 0)
                        {
                            Finish(StreamPumpResult.Delivered, callback);
                            return;
                        }

                        try
                        {
                            Output.BeginWrite(buffer, 0, read, writeResult =>
                    {
                        try
                        {
                            Output.EndWrite(writeResult);
                            Pump(callback);
                        }
                        catch (Exception)
                        {
                            Finish(StreamPumpResult.Aborted, callback);
                        }
                    }, null);
                        }
                        catch (Exception)
                        {
                            Finish(StreamPumpResult.Aborted, callback);
                        }
                    }
                    catch (Exception)
                    {
                        Finish(StreamPumpResult.Aborted, callback);
                    }
                }, null);
            }
            catch (Exception)
            {
                Finish(StreamPumpResult.Aborted, callback);
            }
        }

        public bool Wait(int timeout)
        {
            return sem.Wait(timeout);
        }

        private void Finish(StreamPumpResult result, StreamPumpCallback callback)
        {
            //https://stackoverflow.com/a/55516918/74071
            var task = Task.Run(() => callback(this, result));
            task.Wait();

            try
            {
                sem.Release();
            }
            catch (ObjectDisposedException)
            {
                // ignore
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"StreamPump.Finish Ex [{ ex.Message }]");
            }
        }
    }
}