using System.Linq;
using System.Security.Cryptography;

namespace JwsDetachedStreaming
{
    public class FromBase64UrlTransform : ICryptoTransform
    {
        private const int BLOCK_SIZE = 4;

        private static byte _base64Character62 = (byte)'+';
        private static byte _base64Character63 = (byte)'/';
        private static byte _base64UrlCharacter62 = (byte)'-';
        private static byte _base64UrlCharacter63 = (byte)'_';

        private static byte _base64PadCharacter = (byte)'=';

        private readonly FromBase64Transform _base64Transform = new FromBase64Transform();
        
        public bool CanReuseTransform => _base64Transform.CanReuseTransform;
        public bool CanTransformMultipleBlocks => _base64Transform.CanTransformMultipleBlocks;
        public int InputBlockSize => _base64Transform.InputBlockSize;
        public int OutputBlockSize => _base64Transform.OutputBlockSize;

        private int _blockIndex = -1;

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            TransformToBase64Block(inputBuffer, inputOffset, inputCount);

            var length = _base64Transform.TransformBlock(
                inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset);
            
            return length;
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            TransformToBase64Block(inputBuffer, inputOffset, inputCount);

            var tmp = new byte[inputCount + GetPadLength()];
            for (var i = 0; i < inputCount; i++)
            {
                tmp[i] = inputBuffer[inputOffset + i];
            }

            for (var i = inputCount; i < tmp.Length; i++)
            {
                tmp[i] = _base64PadCharacter;
            }

            return _base64Transform.TransformFinalBlock(tmp.ToArray(), 0, tmp.Length);
        }

        private int GetPadLength()
        {
            if (_blockIndex != -1)
            {
                return BLOCK_SIZE - _blockIndex;
            }
            else
            {
                return 0;
            }
        }

        private void TransformToBase64Block(byte[] inputBuffer, int inputOffset, int length)
        {
            if (length > 0)
            {
                if (_blockIndex == -1)
                {
                    _blockIndex = 0;
                }
                else
                {
                    if (_blockIndex == BLOCK_SIZE)
                    {
                        _blockIndex = 0;
                    }
                }

                _blockIndex += length;
            }

            for (var index = inputOffset; index < length + inputOffset; index++)
            {
                if (inputBuffer[index] == _base64UrlCharacter62)
                {
                    inputBuffer[index] = _base64Character62;
                }
                else if (inputBuffer[index] == _base64UrlCharacter63)
                {
                    inputBuffer[index] = _base64Character63;
                }
            }
        }

        public void Dispose()
        {
            _base64Transform.Dispose();
        }
    }
}

