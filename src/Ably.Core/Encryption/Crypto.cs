using System;
using System.Net;
using Ably.Rest;

namespace Ably.Encryption
{
    ///<summary>Specifies the block cipher mode to use for encryption.</summary>
    public enum CipherMode
    {
        ///<summary>
        /// The Cipher Block Chaining (CBC) mode introduces feedback. Before each plain text
        /// block is encrypted, it is combined with the cipher text of the previous block
        /// by a bitwise exclusive OR operation. This ensures that even if the plain text
        /// contains many identical blocks, they will each encrypt to a different cipher
        /// text block. The initialization vector is combined with the first plain text block
        /// by a bitwise exclusive OR operation before the block is encrypted. If a single
        /// bit of the cipher text block is mangled, the corresponding plain text block will
        /// also be mangled. In addition, a bit in the subsequent block, in the same position
        /// as the original mangled bit, will be mangled.
        /// </summary>
        CBC = 1,

        ///<summary>
        /// The Electronic Codebook (ECB) mode encrypts each block individually. Any blocks
        /// of plain text that are identical and in the same message, or that are in a different
        /// message encrypted with the same key, will be transformed into identical cipher
        /// text blocks. Important: This mode is not recommended because it opens the door
        /// for multiple security exploits. If the plain text to be encrypted contains substantial
        /// repetition, it is feasible for the cipher text to be broken one block at a time.
        /// It is also possible to use block analysis to determine the encryption key. Also,
        /// an active adversary can substitute and exchange individual blocks without detection,
        /// which allows blocks to be saved and inserted into the stream at other points
        /// without detection.
        ///</summary>
        ECB = 2,

        ///<summary>
        /// The Output Feedback (OFB) mode processes small increments of plain text into
        /// cipher text instead of processing an entire block at a time. This mode is similar
        /// to CFB; the only difference between the two modes is the way that the shift register
        /// is filled. If a bit in the cipher text is mangled, the corresponding bit of plain
        /// text will be mangled. However, if there are extra or missing bits from the cipher
        /// text, the plain text will be mangled from that point on.
        ///</summary>
        OFB = 3,

        ///<summary>
        /// The Cipher Feedback (CFB) mode processes small increments of plain text into
        /// cipher text, instead of processing an entire block at a time. This mode uses
        /// a shift register that is one block in length and is divided into sections. For
        /// example, if the block size is 8 bytes, with one byte processed at a time, the
        /// shift register is divided into eight sections. If a bit in the cipher text is
        /// mangled, one plain text bit is mangled and the shift register is corrupted. This
        /// results in the next several plain text increments being mangled until the bad
        /// bit is shifted out of the shift register. The default feedback size can vary
        /// by algorithm, but is typically either 8 bits or the number of bits of the block
        /// size. You can alter the number of feedback bits by using the System.Security.Cryptography.SymmetricAlgorithm.FeedbackSize
        /// property. Algorithms that support CFB use this property to set the feedback.
        ///</summary>
        CFB = 4,

        ///<summary>
        /// The Cipher Text Stealing (CTS) mode handles any length of plain text and produces
        /// cipher text whose length matches the plain text length. This mode behaves like
        /// the CBC mode for all but the last two blocks of the plain text.
        ///</summary>
        CTS = 5
    }

    public class Crypto
    {
        public const String DefaultAlgorithm = "AES";
        public const int DefaultKeylength = 128; ///bits
        public const int DefaultBlocklength = 16; ///bytes
        public const CipherMode DefaultMode = CipherMode.CBC;

        internal static CipherParams GetDefaultParams()
        {
            return Platform.IoC.crypto.GetDefaultParams();
        }

        internal static IChannelCipher GetCipher( ChannelOptions opts )
        {
            CipherParams p = opts.CipherParams ?? GetDefaultParams();
            return Platform.IoC.crypto.GetCipher( p );
        }

        internal static IChannelCipher GetCipher( CipherParams p )
        {
            return Platform.IoC.crypto.GetCipher( p );
        }

        internal static string ComputeHMacSha256( string text, string key )
        {
            return Platform.IoC.crypto.ComputeHMacSha256( text, key );
        }

        /* public static CipherParams GetDefaultParams()
        {
            using (var aes = new AesCryptoServiceProvider())
            {
                aes.KeySize = DefaultKeylength;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.BlockSize = DefaultBlocklength * 8;
                aes.GenerateKey();
                return new CipherParams(aes.Key);
            }
        }

        public static CipherParams GetDefaultParams(byte[] key)
        {
            return new CipherParams(key);
        }

        public static IChannelCipher GetCipher(ChannelOptions opts)
        {
            CipherParams @params = opts.CipherParams ?? GetDefaultParams();

            if (string.Equals(@params.Algorithm, DefaultAlgorithm, StringComparison.CurrentCultureIgnoreCase))
                return new AesCipher(@params);

            throw new AblyException("Currently only the AES encryption algorith is supported", 50000, HttpStatusCode.InternalServerError);
        } */
    }
}