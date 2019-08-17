using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlobViewer.Common;

namespace SlobViewer.Slob
{
  public class StoreItemBase
  {
    protected string[] _content;
    protected byte[] _compressedContent;
    protected byte[] _contentIds;

    protected void DecompressContent(Encoding encoding, string compressionMethod, byte[] buffer)
    {
      Func<Stream, Stream> createInflaterStream = null;
      switch (compressionMethod)
      {
        case "lzma":
        case "lzma2":
          {
            createInflaterStream = (inStream) =>
            {
              var outStream = new MemoryStream();

              int iDictionaryByte = 30;
              int dictionarySize = iDictionaryByte == 40 ? -1 : (2 + iDictionaryByte % 2) << (11 + iDictionaryByte / 2);


              inStream.Read(buffer, 0, 6); // Read 6 header bytes. currently, we do not know exactly what those header bytes mean. Seems that byte[5] is the encoding prop, and byte[3] and [4] the compressed length
              byte[] properties = new byte[5];
              properties[0] = buffer[5];
              Array.Copy(BitConverter.GetBytes(dictionarySize), 0, properties, 1, 4); // copy dictionary size into array

              var decoder = new SevenZip.Compression.LZMA.Decoder();
              decoder.SetDecoderProperties(properties);
              long outSize = long.MaxValue;
              long compressedSize = inStream.Length - inStream.Position;
              decoder.Code(inStream, outStream, compressedSize, outSize, null);
              outStream.Seek(0, SeekOrigin.Begin);
              return outStream;
            };

            DecompressWithInflaterStream(createInflaterStream, encoding, buffer);
          }
          break;
        case "zlib":
          createInflaterStream = (s) => new ICSharpCode.SharpZipLib.Zip.Compression.Streams.InflaterInputStream(s);
          DecompressWithInflaterStream(createInflaterStream, encoding, buffer);
          break;
        case "bz2":
        case "bzip2":
          createInflaterStream = (s) => new ICSharpCode.SharpZipLib.BZip2.BZip2InputStream(s);
          DecompressWithInflaterStream(createInflaterStream, encoding, buffer);
          break;
        case "gzip":
          createInflaterStream = (s) => new ICSharpCode.SharpZipLib.GZip.GZipInputStream(s);
          DecompressWithInflaterStream(createInflaterStream, encoding, buffer);
          break;
        case "lzw":
          createInflaterStream = (s) => new ICSharpCode.SharpZipLib.Lzw.LzwInputStream(s);
          DecompressWithInflaterStream(createInflaterStream, encoding, buffer);
          break;
        default:
          throw new NotImplementedException();
      }
    }

    protected void DecompressWithInflaterStream(Func<Stream, Stream> createDeflaterStream, Encoding encoding, byte[] buffer)
    {
      _content = _content ?? new string[_contentIds.Length];
      using (var inStream = new MemoryStream(_compressedContent))
      {

        using (var outStream = createDeflaterStream(inStream))
        {
          var offsets = new uint[_contentIds.Length];

          for (int i = 0; i < _contentIds.Length; ++i)
          {
            outStream.Read(buffer, 0, 4);
            offsets[i] = BigEndianBitConverter.ToUInt32(buffer, 0);
          }

          long position = 4 * _contentIds.Length;
          long positionAfterTable = position;

          for (int i = 0; i < _contentIds.Length; ++i)
          {
            var toSkip = position - (offsets[i] + positionAfterTable);
            if (0 != toSkip)
            {
              Skip(outStream, toSkip, buffer);
            }
            else if (toSkip < 0)
            {
              throw new InvalidOperationException("Can not go backwards");
            }
            try
            {
              _content[i] = BigEndianBitConverter.ReadBigText(outStream, encoding, buffer, ref position);
            }
            catch (Exception e)
            {
              System.Diagnostics.Debugger.Launch();

              _content[i] = "Content is not available because lzma2 decompression reached end before position were content was supposed to be";
            }
          }
        }
      }
    }

    void Skip(Stream stream, long count, byte[] buffer)
    {
      var l = buffer.Length;
      while (count > 0)
      {

        var rd = stream.Read(buffer, 0, (int)Math.Min(l, count));
        count -= rd;
      }
    }

  }
}
