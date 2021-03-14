using System;
using System.Security.Cryptography;
using System.Text;

namespace JwsDetachedStreaming
{
    public static class Base64UrlEncoder
    {
        public static byte[] EncodeFromString(string input)
        {
            return Encode(Encoding.UTF8.GetBytes(input));
        }

        public static byte[] DecodeFromString(string input)
        {
            return Decode(Encoding.UTF8.GetBytes(input));
        }

        public static byte[] Encode(byte[] inputBuffer)
        {
            return Transform(new ToBase64UrlTransform(), inputBuffer);
        }

        public static byte[] Decode(byte[] inputBuffer)
        {
            return Transform(new FromBase64UrlTransform(), inputBuffer);
        }

        private static byte[] Transform(ICryptoTransform transform, byte[] inputBuffer)
        {
            if (inputBuffer == null)
                throw new ArgumentNullException("inputBuffer");

            var outputBuffer = new byte[inputBuffer.Length * transform.OutputBlockSize];
            var outputOffset = 0;
            var inputOffset = 0;

            if (transform.CanTransformMultipleBlocks)
            {
                var inputCount = (inputBuffer.Length / transform.InputBlockSize) * transform.InputBlockSize;

                outputOffset += transform.TransformBlock(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset);
                inputOffset += inputCount;
            }

            var inputRemain = inputBuffer.Length - inputOffset;

            while (transform.InputBlockSize <= inputRemain)
            {
                outputOffset += transform.TransformBlock(inputBuffer, inputOffset, transform.InputBlockSize, outputBuffer, outputOffset);

                inputOffset += transform.InputBlockSize;
                inputRemain -= transform.InputBlockSize;
            }

            var finalBlock = transform.TransformFinalBlock(inputBuffer, inputOffset, inputBuffer.Length - inputOffset);

            if (outputBuffer.Length != outputOffset + finalBlock.Length)
                Array.Resize(ref outputBuffer, outputOffset + finalBlock.Length);

            Buffer.BlockCopy(finalBlock, 0, outputBuffer, outputOffset, finalBlock.Length);

            return outputBuffer;
        }
    }
}
