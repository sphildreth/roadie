namespace Roadie.Library.Configuration
{
    public class Inspector : IInspector
    {
        /// <summary>
        ///     When true then make a copy of files to new destination versus moving files to destination.
        /// </summary>
        public bool DoCopyFiles { get; set; }

        /// <summary>
        ///     When true then don't modify any files only report what would be done.
        /// </summary>
        public bool IsInReadOnlyMode { get; set; }
    }
}