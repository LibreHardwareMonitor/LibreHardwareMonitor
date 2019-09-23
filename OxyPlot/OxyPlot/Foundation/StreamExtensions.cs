namespace OxyPlot
{
    using System.IO;

    /// <summary>
    /// Implements <see cref="Stream"/> extension methods.
    /// </summary>
    public static class StreamExtensions
    {
        /// <summary>
        /// Copies to the specified stream.
        /// </summary>
        /// <param name="input">The input stream.</param>
        /// <param name="output">The output stream.</param>
        public static void CopyTo(this Stream input, Stream output)
        {
            var buffer = new byte[32768];
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, read);
            }
        }
    }
}