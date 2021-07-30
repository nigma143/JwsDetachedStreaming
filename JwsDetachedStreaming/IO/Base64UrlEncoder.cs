using System;
using System.Buffers;
using System.Text;

namespace JwsDetachedStreaming.IO
{
    public static class Base64UrlEncoder
    {
        public static string EncodeToString(string input)
        {
            return Encoding.UTF8.GetString(Encode(Encoding.ASCII.GetBytes(input)).Span);
        }

        public static string EncodeToString(ReadOnlySpan<byte> input)
        {
            return Encoding.UTF8.GetString(Encode(input).Span);
        }

        public static string DecodeToString(string input)
        {
            return Encoding.ASCII.GetString(Decode(Encoding.UTF8.GetBytes(input)).Span);
        }

        public static string DecodeToString(ReadOnlySpan<byte> input)
        {
            return Encoding.ASCII.GetString(Decode(input).Span);
        }

        public static Memory<byte> Encode(ReadOnlySpan<byte> input)
        {
            var output = new byte[Base64Url.GetMaxEncodedLength(input.Length)];
            
            var result = Base64Url.Encode(input, output, out _, out var bytesWritten);
            if (result != OperationStatus.Done)
            {
                throw new FormatException(result.ToString());
            }
            
            return new Memory<byte>(output, 0, bytesWritten); 
        }

        public static Memory<byte> Decode(ReadOnlySpan<byte> input)
        {
            var output = new byte[Base64Url.GetMaxDecodedLength(input.Length)+2];

            var result = Base64Url.Decode(input, output, out _, out var bytesWritten);
            if (result != OperationStatus.Done)
            {
                throw new FormatException(result.ToString());
            }

            return new Memory<byte>(output, 0, bytesWritten);
        }
    }
}
