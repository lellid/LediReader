using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlobViewer.Common
{
  /// <summary>
  /// Converter that uses big endian convention to convert to/from byte arrays and streams.
  /// </summary>
  public static class BigEndianBitConverter
  {
    /// <summary>
    /// Converts a byte sequence into a <see cref="UInt16"/> value.
    /// </summary>
    /// <param name="buffer">The buffer of bytes.</param>
    /// <param name="offset">The storage position in the buffer .</param>
    /// <returns>The value converted from the bytes in the buffer at position <paramref name="offset"/> and thereafter.</returns>
    public static UInt16 ToUInt16(byte[] buffer, int offset)
    {
      return (UInt16)((buffer[offset] << 8) + buffer[offset + 1]);
    }

    /// <summary>
    /// Converts a byte sequence into a <see cref="UInt162"/> value.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="offset">A helper buffer.</param>
    /// <returns>The value converted from the bytes in the stream.</returns>
    public static UInt16 ToUInt16(System.IO.Stream stream, byte[] buffer)
    {
      var rd = stream.Read(buffer, 0, 2);
      if (2 != rd)
        throw new System.IO.EndOfStreamException();
      return ToUInt16(buffer, 0);
    }

    /// <summary>
    /// Converts a byte sequence into a <see cref="UInt32"/> value.
    /// </summary>
    /// <param name="buffer">The buffer of bytes.</param>
    /// <param name="offset">The storage position in the buffer .</param>
    /// <returns>The value converted from the bytes in the buffer at position <paramref name="offset"/> and thereafter.</returns>
    public static UInt32 ToUInt32(byte[] buffer, int offset)
    {
      return (UInt32)((buffer[offset] << 24) + (buffer[offset + 1] << 16) + (buffer[offset + 2] << 8) + buffer[offset + 3]);
    }

    /// <summary>
    /// Converts a byte sequence into a <see cref="UInt32"/> value.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="offset">A helper buffer.</param>
    /// <returns>The value converted from the bytes in the stream.</returns>
    public static UInt32 ToUInt32(System.IO.Stream stream, byte[] buffer)
    {
      var rd = stream.Read(buffer, 0, 4);
      if (4 != rd)
        throw new System.IO.EndOfStreamException();
      return ToUInt32(buffer, 0);
    }


    /// <summary>
    /// Converts a byte sequence into a <see cref="UInt64"/> value.
    /// </summary>
    /// <param name="buffer">The buffer of bytes.</param>
    /// <param name="offset">The storage position in the buffer .</param>
    /// <returns>The value converted from the bytes in the buffer at position <paramref name="offset"/> and thereafter.</returns>
    public static UInt64 ToUInt64(byte[] buffer, int offset)
    {
      UInt64 result = 0;

      for (int i = 0; i < 8; ++i)
      {
        result <<= 8;
        result += buffer[i + offset];
      }
      return result;
    }

    /// <summary>
    /// Converts a byte sequence into a <see cref="UInt64"/> value.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="offset">A helper buffer.</param>
    /// <returns>The value converted from the bytes in the stream.</returns>
    public static UInt64 ToUInt64(System.IO.Stream stream, byte[] buffer)
    {
      var rd = stream.Read(buffer, 0, 8);
      if (8 != rd)
        throw new System.IO.EndOfStreamException();
      return ToUInt64(buffer, 0);
    }

    /// <summary>
    /// Converts a <see cref="UInt16"/> value and stores its byte representation in a buffer.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="buffer">The buffer.</param>
    /// <param name="offset">The storage position in the buffer .</param>
    public static void ToBuffer(UInt16 value, byte[] buffer, int offset)
    {
      buffer[offset + 0] = (byte)((value >> 8) & 0xFF);
      buffer[offset + 1] = (byte)((value) & 0xFF);
    }

    /// <summary>
    /// Converts a <see cref="UInt32"/> value and stores its byte representation in a buffer.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="buffer">The buffer.</param>
    /// <param name="offset">The storage position in the buffer .</param>
    public static void ToBuffer(UInt32 value, byte[] buffer, int offset)
    {
      buffer[offset + 0] = (byte)((value >> 24) & 0xFF);
      buffer[offset + 1] = (byte)((value >> 16) & 0xFF);
      buffer[offset + 2] = (byte)((value >> 8) & 0xFF);
      buffer[offset + 3] = (byte)((value) & 0xFF);
    }

    /// <summary>
    /// Converts a <see cref="UInt64"/> value and stores its byte representation in a buffer.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="buffer">The buffer.</param>
    /// <param name="offset">The storage position in the buffer .</param>
    public static void ToBuffer(UInt64 value, byte[] buffer, int offset)
    {
      buffer[offset + 0] = (byte)((value >> 56) & 0xFF);
      buffer[offset + 1] = (byte)((value >> 48) & 0xFF);
      buffer[offset + 2] = (byte)((value >> 40) & 0xFF);
      buffer[offset + 3] = (byte)((value >> 32) & 0xFF);
      buffer[offset + 4] = (byte)((value >> 24) & 0xFF);
      buffer[offset + 5] = (byte)((value >> 16) & 0xFF);
      buffer[offset + 6] = (byte)((value >> 8) & 0xFF);
      buffer[offset + 7] = (byte)((value) & 0xFF);
    }

    /// <summary>
    /// Converts a <see cref="UInt16"/> value and stores its byte representation in a stream.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="stream">The stream to write the byte representation of <paramref name="value"/> to.</param>
    public static void WriteUInt16(UInt16 value, System.IO.Stream stream)
    {
      stream.WriteByte((byte)((value >> 8) & 0xFF));
      stream.WriteByte((byte)((value >> 0) & 0xFF));
    }

    /// <summary>
    /// Converts a <see cref="UInt32"/> value and stores its byte representation in a stream.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="stream">The stream to write the byte representation of <paramref name="value"/> to.</param>
    public static void WriteUInt32(UInt32 value, System.IO.Stream stream)
    {
      stream.WriteByte((byte)((value >> 24) & 0xFF));
      stream.WriteByte((byte)((value >> 16) & 0xFF));
      stream.WriteByte((byte)((value >> 8) & 0xFF));
      stream.WriteByte((byte)((value >> 0) & 0xFF));
    }


    /// <summary>
    /// Converts a <see cref="UInt64"/> value and stores its byte representation in a stream.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="stream">The stream to write the byte representation of <paramref name="value"/> to.</param>
    public static void WriteUInt64(UInt64 value, System.IO.Stream stream)
    {
      stream.WriteByte((byte)((value >> 56) & 0xFF));
      stream.WriteByte((byte)((value >> 48) & 0xFF));
      stream.WriteByte((byte)((value >> 40) & 0xFF));
      stream.WriteByte((byte)((value >> 32) & 0xFF));
      stream.WriteByte((byte)((value >> 24) & 0xFF));
      stream.WriteByte((byte)((value >> 16) & 0xFF));
      stream.WriteByte((byte)((value >> 8) & 0xFF));
      stream.WriteByte((byte)((value >> 0) & 0xFF));
    }


    /// <summary>
    /// Reads a tiny text from a stream. A tiny text consist of one byte designating the length of the stream,
    /// followed by the byte representation of the string. The string is decoded using the given <paramref name="encoding"/>.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="encoding">The text encoding.</param>
    /// <param name="buffer">The buffer used to temporarily store the bytes, should have a length of at least 255.</param>
    /// <returns>The read tiny string.</returns>
    public static string ReadTinyText(System.IO.Stream stream, System.Text.Encoding encoding, byte[] buffer)
    {
      int len = stream.ReadByte();
      var rd = stream.Read(buffer, 0, len);

      if (rd != len)
        throw new System.IO.EndOfStreamException();


      return encoding.GetString(buffer, 0, len);
    }


    /// <summary>
    /// Writes a tiny text to a stream. A tiny text consist of one byte designating the length of the stream,
    /// followed by the byte representation of the string. The string is encoded using the given <paramref name="encoding"/>.
    /// </summary>
    /// <param name="value">The string to write. The byte representation of the string must not exceed 255 bytes.</param>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="encoding">The encoding used to convert the string to its byte representation.</param>
    /// <exception cref="InvalidOperationException">The byte representation of the string exceeds 255 bytes.</exception>
    public static void WriteTinyText(string value, System.IO.Stream stream, System.Text.Encoding encoding)
    {
      var bytes = encoding.GetBytes(value);
      if (bytes.Length > 255)
        throw new InvalidOperationException();

      stream.WriteByte((byte)bytes.Length);
      stream.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Reads a text from a stream. A text consist of two bytes (big endian order) designating the length of the stream,
    /// followed by the byte representation of the string. The string is decoded using the given <paramref name="encoding"/>.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="encoding">The text encoding.</param>
    /// <param name="buffer">The buffer used to temporarily store the bytes, should have a length of at least 65535.</param>
    /// <returns>The read string.</returns>
    public static string ReadText(System.IO.Stream stream, System.Text.Encoding encoding, byte[] buffer)
    {
      stream.Read(buffer, 0, 2);
      ushort len = ToUInt16(buffer, 0);
      var rd = stream.Read(buffer, 0, len);

      if (rd != len)
        throw new System.IO.EndOfStreamException();

      return encoding.GetString(buffer, 0, len);
    }


    /// <summary>
    /// Writes a text to a stream. A text consist of two bytes (in big endian order) designating the length of the stream,
    /// followed by the byte representation of the string. The string is encoded using the given <paramref name="encoding"/>.
    /// </summary>
    /// <param name="value">The string to write. The byte representation of the string must not exceed 65535 bytes.</param>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="encoding">The encoding used to convert the string to its byte representation.</param>
    /// <exception cref="InvalidOperationException">The byte representation of the string exceeds 65535 bytes.</exception>
    public static void WriteText(string value, System.IO.Stream stream, System.Text.Encoding encoding)
    {
      var bytes = encoding.GetBytes(value);
      if (bytes.Length > 65535)
        throw new InvalidOperationException();

      WriteUInt16((UInt16)bytes.Length, stream);
      stream.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Reads a big text from a stream. A big text consist of four bytes (big endian order) designating the length of the stream,
    /// followed by the byte representation of the string. The string is decoded using the given <paramref name="encoding"/>.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="encoding">The text encoding.</param>
    /// <param name="buffer">The buffer used to temporarily store the bytes, should have a sufficient length.</param>
    /// <returns>The read string.</returns>
    public static string ReadBigText(System.IO.Stream stream, System.Text.Encoding encoding, byte[] buffer)
    {
      stream.Read(buffer, 0, 4);
      var len = (int)ToUInt32(buffer, 0);
      var rd = stream.Read(buffer, 0, len);

      if (rd != len)
        throw new System.IO.EndOfStreamException();

      return encoding.GetString(buffer, 0, len);
    }

    /// <summary>
    /// Reads a big text from a stream. A big text consist of four bytes (big endian order) designating the length of the stream,
    /// followed by the byte representation of the string. The string is decoded using the given <paramref name="encoding"/>.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="encoding">The text encoding.</param>
    /// <param name="buffer">The buffer used to temporarily store the bytes, should have a sufficient length.</param>
    /// <param name="position">Tracks the position inside the stream (for streams that don't support Position by itself).</param>
    /// <returns>The read string.</returns>
    public static string ReadBigText(System.IO.Stream stream, System.Text.Encoding encoding, byte[] buffer, ref long position)
    {
      stream.Read(buffer, 0, 4);
      var len = (int)ToUInt32(buffer, 0);
      var rd = stream.Read(buffer, 0, len);

      if (len != rd)
        throw new InvalidOperationException("End of file reached before entire string could be read.");

      position += 4 + rd;
      return encoding.GetString(buffer, 0, len);
    }

    /// <summary>
    /// Writes a big text to a stream. A text consist of four bytes (in big endian order) designating the length of the stream,
    /// followed by the byte representation of the string. The string is encoded using the given <paramref name="encoding"/>.
    /// </summary>
    /// <param name="value">The string to write. The byte representation of the string must not exceed 2147483647 bytes.</param>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="encoding">The encoding used to convert the string to its byte representation.</param>
    /// <exception cref="InvalidOperationException">The byte representation of the string exceeds 2147483647 bytes.</exception>
    public static void WriteBigText(string value, System.IO.Stream stream, System.Text.Encoding encoding)
    {
      var bytes = encoding.GetBytes(value);
      if (bytes.Length > 2147483647)
        throw new InvalidOperationException();
      WriteUInt32((UInt32)bytes.Length, stream);
      stream.Write(bytes, 0, bytes.Length);
    }
  }
}
