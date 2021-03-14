using System.Linq;
using System.Security.Cryptography;

namespace JwsDetachedStreaming
{
    public class ToBase64UrlTransform : ICryptoTransform
    {
        private static byte _base64Character62 = (byte)'+';
        private static byte _base64Character63 = (byte)'/';
        private static byte _base64UrlCharacter62 = (byte)'-';
        private static byte _base64UrlCharacter63 = (byte)'_';

        private static byte _base64PadCharacter = (byte)'=';

        private readonly ToBase64Transform _base64Transform = new ToBase64Transform();

        public bool CanReuseTransform => _base64Transform.CanReuseTransform;
        public bool CanTransformMultipleBlocks => _base64Transform.CanTransformMultipleBlocks;
        public int InputBlockSize => _base64Transform.InputBlockSize;
        public int OutputBlockSize => _base64Transform.OutputBlockSize;

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            var length = _base64Transform.TransformBlock(
                inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset);

            TransformToBase64Block(outputBuffer, outputOffset, length);

            return length;
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            var result = _base64Transform.TransformFinalBlock(inputBuffer, inputOffset, inputCount);

            TransformToBase64Block(result, 0, result.Length);

            //remove = or ==
            for (var index = 0; index < result.Length; index++)
            {
                if (result[index] == _base64PadCharacter)
                {
                    return result.Take(index).ToArray();
                }
            }

            return result;
        }

        private void TransformToBase64Block(byte[] outputBuffer, int outputOffset, int length)
        {
            for (var index = outputOffset; index < length + outputOffset; index++)
            {
                if (outputBuffer[index] == _base64Character62)
                {
                    outputBuffer[index] = _base64UrlCharacter62;
                }
                else if (outputBuffer[index] == _base64Character63)
                {
                    outputBuffer[index] = _base64UrlCharacter63;
                }
            }
        }

        public void Dispose()
        {
            _base64Transform.Dispose();
        }
    }
}
