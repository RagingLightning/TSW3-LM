using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GvasFormat.Serialization;
using GvasFormat.Serialization.UETypes;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace TSW3LM
{
    internal class CompressionHelper
    {
        private static readonly byte[] Signature = { 0xc1, 0x83, 0x2a, 0x9e };
        private static readonly int MaximumChunkSize = 131072; // 0x20000

        public static Game.Livery? DecompressReskin(UEGenericStructProperty structProperty)
        {
            var compressedReskin = structProperty.Properties.FirstOrDefault(p => p is UEArrayProperty && p.Name == "CompressedReskin") as UEArrayProperty;

            if (compressedReskin?.Items?.FirstOrDefault() is not UEByteProperty byteString)
                return null;

            var byteArray = Utils.HexStringToByteArray(byteString.Value);
            using var memoryStream = new MemoryStream(byteArray);
            using var binaryReader = new BinaryReader(memoryStream);
            var bytes = new List<byte>();

            // Inflate chunks
            do
            {
                var chunk = ReadChunk(binaryReader);
                bytes.AddRange(chunk);
            } while (binaryReader.BaseStream.Position < binaryReader.BaseStream.Length);

            var livery = bytes.ToArray();
            var decompressedLivery = Utils.ConvertTSW3(livery, true);
            return decompressedLivery;
        }

        public static UEGenericStructProperty CompressReskin(UEGenericStructProperty structProperty)
        {
            // Convert the uncompressed livery into TSW3s compression format
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            structProperty.SerializeStructProp(writer);

            ms.Position = 0;
            var bytes = ms.ToArray();

            using var byteStream = new MemoryStream();
            using var byteWriter = new BinaryWriter(byteStream);
            byteWriter.WriteInt32(bytes.Length);
            byteWriter.Write(bytes);
            var idProperty = structProperty.Properties.Find(p => p.Name == "ID");

            byteStream.Position = 0;
            bytes = byteStream.ToArray();
            if (idProperty != null)
                return CompressReskin(idProperty, bytes);
            throw new FormatException("Livery doesn't contain an ID-Property");
        }

        public static UEGenericStructProperty CompressReskin(UEProperty idProperty, IEnumerable<byte> bytes)
        {
            if (idProperty == null)
                throw new ArgumentNullException(nameof(idProperty));

            if (idProperty.Name != "ID")
                throw new ArgumentException("Property is not an ID property", nameof(idProperty));

            // split livery into Chunks
            var chunks = bytes.Chunk(MaximumChunkSize);
            using var outputByteStream = new MemoryStream();

            // Deflate Chunks
            foreach (var chunk in chunks)
            {
                var compressed = CompressChunk(chunk);
                outputByteStream.Write(compressed);
            }

            // Encode for ByteProperty
            outputByteStream.Position = 0;
            var compressedBytes = outputByteStream.ToArray();
            var compressedString = Utils.ByteArrayToHexString(compressedBytes);

            // And build the result
            var arrayProperty = new UEArrayProperty
            {
                Name = "CompressedReskin",
                Type = "ArrayProperty",
                ItemType = "ByteProperty",
                Count = compressedBytes.Length,
                ValueLength = compressedBytes.Length + 4,
                Items = new UEProperty[]
                {
                    new UEByteProperty
                    {
                        Value = compressedString
                    }
                }
            };

            var compressedProperty = new UEGenericStructProperty
            {
                Name = "CompressedReskins",
                Type = "StructProperty",
                StructType = "CompressedReskin",
                Properties = new List<UEProperty>
                {
                    idProperty,
                    arrayProperty,
                    new UENoneProperty()
                }
            };

            compressedProperty.ValueLength = Utils.DetermineValueLength(compressedProperty, r =>
            {
                r.ReadUEString(); //name
                r.ReadUEString(); //type
                r.ReadInt64(); //valueLength
                r.ReadUEString();
                r.ReadBytes(16);
                r.ReadByte(); //terminator
                return r.BaseStream.Length - r.BaseStream.Position;
            });

            return compressedProperty;
        }

        private static byte[] ReadChunk(BinaryReader binaryReader)
        {
            // First portion contains the signature
            var headerBytes = binaryReader.ReadBytes(4);

            if (!headerBytes.SequenceEqual(Signature))
                throw new InvalidOperationException($"Invalid signature. Got {BitConverter.ToString(headerBytes)}, expected {BitConverter.ToString(Signature)}");

            // The next 4 bytes are a spacer which can be ignored.
            binaryReader.ReadBytes(4);

            // Then comes the maximum chunk size
            var liveryMaxChunkSize = binaryReader.ReadInt64();
            if (liveryMaxChunkSize != MaximumChunkSize)
                Log.Message($"Default chunk size is {MaximumChunkSize}, livery reports {liveryMaxChunkSize}", level: Log.LogLevel.WARNING);

            // next 8 bytes store compressed size
            var compressedSize = binaryReader.ReadInt64();
            var input = new byte[compressedSize];

            // next 8 bytes store decompressed size
            var decompressedSize = binaryReader.ReadInt64();
            var output = new byte[decompressedSize];

            // for whatever reason they repeat the previous 16 bytes, we disregard this
            binaryReader.ReadInt64();
            binaryReader.ReadInt64();

            Log.Message("reading compressed file");

            // now begins the actual reading

            // the input will be however long the input file length is
            for (int i = 0; i < input.Length; i++)
            {
                input[i] = binaryReader.ReadByte();
            }

            // decompress using zlib
            var inflater = new Inflater();

            // do stuff I saw in the java docs
            inflater.SetInput(input, 0, input.Length);

            Log.Message("Decompressing using zlib");

            try
            {
                var resultLength = inflater.Inflate(output);
                inflater.Reset();

                Log.Message("Length of decompressed: " + resultLength);
            }
            catch (Exception e)
            {
                Log.Exception("Failed inflating", e);
                throw;
            }

            return output;
        }

        private static byte[] CompressChunk(byte[] input)
        {
            using var deflateStream = new MemoryStream();
            using var deflater = new DeflaterOutputStream(deflateStream);

            deflater.Write(input, 0, input.Length);
            deflater.Finish();

            // Now reassemble the whole thing
            var header = new byte[] { 0xC1, 0x83, 0x2A, 0x9E, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0 };
            deflateStream.Position = 0;
            var compressedBytes = deflateStream.ToArray();

            using var compressedStream = new MemoryStream();
            using var compressedWriter = new BinaryWriter(compressedStream);
            compressedWriter.Write(Signature);
            compressedWriter.Write(new byte[] { 0x00, 0x00, 0x00, 0x00, });

            // Write the compressed bytes (twice)
            compressedWriter.WriteInt64(MaximumChunkSize);
            compressedWriter.WriteInt64(compressedBytes.Length);
            compressedWriter.WriteInt64(input.Length);
            compressedWriter.WriteInt64(compressedBytes.Length);
            compressedWriter.WriteInt64(input.Length);

            // Write the body
            compressedWriter.Write(compressedBytes);

            compressedStream.Position = 0;
            var result = compressedStream.ToArray();
            return result;
        }

    }
}
