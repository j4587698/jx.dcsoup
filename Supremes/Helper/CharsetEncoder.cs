using System.Text;

namespace Supremes.Helper
{
    /// <summary>
    /// 
    /// </summary>
    internal class CharsetEncoder
    {
        private readonly Encoder encoder;
        private readonly Encoding encoding;

        public CharsetEncoder(Encoding enc)
        {
            this.encoder = enc.GetEncoder();
            this.encoder.Fallback = EncoderFallback.ExceptionFallback;
            encoding = enc;
        }

        public bool CanEncode(char[] chars)
        {
            try
            {
                encoder.GetByteCount(chars, 0, chars.Length, true);
                return true;
            }
            catch (EncoderFallbackException)
            {
                return false;
            }
        }
        
        public string CharsetName => encoding.WebName;
    }
}
