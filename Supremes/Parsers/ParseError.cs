using Supremes.Parsers;

namespace Supremes.Parsers
{
    /// <summary>
    /// A Parse Error records an error in the input HTML that occurs in either the tokenisation or the tree building phase.
    /// </summary>
    public sealed class ParseError
    {
        internal ParseError(CharacterReader reader, string errorMsg)
        {
            Position = reader.Pos();
            CursorPos = reader.CursorPos();
            this.ErrorMessage = errorMsg;
        }
        
        internal ParseError(CharacterReader reader, string errorFormat, params object[] args)
        {
            Position = reader.Pos();
            CursorPos = reader.CursorPos();
            ErrorMessage = string.Format(errorFormat, args);
        }

        internal ParseError(int pos, string errorMsg)
        {
            this.Position = pos;
            CursorPos = pos.ToString();
            this.ErrorMessage = errorMsg;
        }

        internal ParseError(int pos, string errorFormat, params object[] args)
        {
            this.ErrorMessage = string.Format(errorFormat, args);
            CursorPos = pos.ToString();
            this.Position = pos;
        }

        /// <summary>
        /// Retrieve the error message.
        /// </summary>
        /// <returns>the error message.</returns>
        public string ErrorMessage { get; }

        /// <summary>
        /// Retrieves the offset of the error.
        /// </summary>
        /// <returns>error offset within input</returns>
        public int Position { get; }

        /// <summary>
        ///  Get the formatted line:column cursor position where the error occurred.
        /// </summary>
        public string CursorPos { get; }

        /// <summary>
        /// Converts the value of this instance to a string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"<{CursorPos}>: {ErrorMessage}";
        }
    }
}
