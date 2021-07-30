using System;
using System.Buffers;
using System.Buffers.Text;
using System.Runtime.CompilerServices;

namespace JwsDetachedStreaming.IO
{
    public static class Base64Url
    {
        private const byte BASE64_CHARACTER62 = (byte)'+';
        private const byte BASE64_CHARACTER63 = (byte)'/';
        private const byte BASE64_URL_CHARACTER62 = (byte)'-';
        private const byte BASE64_URL_CHARACTER63 = (byte)'_';
        private const byte BASE64_PAD_CHARACTER = (byte)'=';

        public static OperationStatus Encode(ReadOnlySpan<byte> input, Span<byte> output, out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true)
        {
            var status = Base64.EncodeToUtf8(input, output, out bytesConsumed, out bytesWritten, isFinalBlock);

            if (isFinalBlock)
            {
                var reductionSize = 0;
                if (output[bytesWritten - 1] == BASE64_PAD_CHARACTER)
                {
                    reductionSize = 1;
                }

                if (output[bytesWritten - 2] == BASE64_PAD_CHARACTER)
                {
                    reductionSize = 2;
                }

                bytesWritten -= reductionSize;
            }

            for (var index = 0; index < bytesWritten; index++)
            {
                switch (output[index])
                {
                    case BASE64_CHARACTER62:
                        output[index] = BASE64_URL_CHARACTER62;
                        break;
                    case BASE64_CHARACTER63:
                        output[index] = BASE64_URL_CHARACTER63;
                        break;
                }
            }

            return status;
        }

        public static OperationStatus Decode(ReadOnlySpan<byte> input, Span<byte> output, out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true)
        {
            var mod = input.Length % 4;
            if (mod == 1)
            {
                throw new FormatException("Unable to decode input as Base64url encoded bytes");
            }

            var decodedLength = input.Length + (4 - mod) % 4;

            byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(decodedLength);
            input.CopyTo(sharedBuffer);

            try
            {
                for (var index = 0; index < sharedBuffer.Length; index++)
                {
                    switch (sharedBuffer[index])
                    {
                        case BASE64_URL_CHARACTER62:
                            sharedBuffer[index] = BASE64_CHARACTER62;
                            break;
                        case BASE64_URL_CHARACTER63:
                            sharedBuffer[index] = BASE64_CHARACTER63;
                            break;
                    }
                }

                if (decodedLength > input.Length)
                {
                    sharedBuffer[input.Length] = BASE64_PAD_CHARACTER;
                    if (input.Length + 2 == decodedLength)
                    {
                        sharedBuffer[input.Length + 1] = BASE64_PAD_CHARACTER;
                    }
                }

                var status = Base64.DecodeFromUtf8(sharedBuffer.AsSpan(0, decodedLength), output, out bytesConsumed, out bytesWritten, isFinalBlock);
                return status;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(sharedBuffer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetMaxEncodedLength(int length)
        {
            return (length + 2) / 3 * 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetMaxDecodedLength(int length)
        {
            return ((length + 2) >> 2) * 3;
        }
    }
}
