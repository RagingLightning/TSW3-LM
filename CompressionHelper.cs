﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using GvasFormat.Serialization;
using GvasFormat.Serialization.UETypes;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace TSW3LM
{
    internal class CompressionHelper
    {
        private static readonly byte[] Signature = { 0xc1, 0x83, 0x2a, 0x9e };

        public static Game.Livery? DecompressReskin(UEGenericStructProperty structProperty)
        {
            var compressedReskin = structProperty.Properties.FirstOrDefault(p => p is UEArrayProperty && p.Name == "CompressedReskin") as UEArrayProperty;
            var byteString = compressedReskin?.Items?.FirstOrDefault() as UEByteProperty;

            if (byteString == null)
                return null;

            var byteArray = Utils.HexStringToByteArray(byteString.Value);
            using var memoryStream = new MemoryStream(byteArray);
            using var binaryReader = new BinaryReader(memoryStream);
            var bytes = new List<byte>();

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
            var bytes = ms.ToArray().ToList();

            // Re-add the bytes necessary for a TSW3 livery
            bytes.Insert(0, 85);
            bytes.Insert(1, 7);
            bytes.Insert(2, 0);
            bytes.Insert(3, 0);

            // Deflate
            using var outputByteStream = new MemoryStream();
            using var deflater = new DeflaterOutputStream(outputByteStream);
            var byteArray = bytes.ToArray();
            deflater.Write(byteArray, 0, byteArray.Length);
            deflater.Finish();
            outputByteStream.Position = 0;
            var deflatedBytes = outputByteStream.ToArray();


            // Now reassemble the whole thing
            var header = new byte[] { 193, 131, 42, 158, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0 };
            using var compressedMemoryStream = new MemoryStream();
            using var compressedWriter = new BinaryWriter(compressedMemoryStream);

            compressedWriter.Write(header, 0, header.Length);

            // Write the compressed bytes (twice)
            compressedWriter.WriteInt64(deflatedBytes.Length);
            compressedWriter.WriteInt64(byteArray.Length);
            compressedWriter.WriteInt64(deflatedBytes.Length);
            compressedWriter.WriteInt64(byteArray.Length);

            // Write the body
            compressedWriter.Write(deflatedBytes, 0, deflatedBytes.Length);

            // Serialize the thing back to a string
            compressedMemoryStream.Position = 0;
            var compressedBytes = compressedMemoryStream.ToArray();
            var compressedString = Utils.ByteArrayToHexString(compressedBytes);

            // And build the result
            var idProperty = structProperty.Properties.Find(p => p.Name == "ID");

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
            var maximumChunkSize = binaryReader.ReadInt64();

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

    }
}
