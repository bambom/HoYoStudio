using System;
using System.Collections;
using System.IO;

namespace AssetStudio
{
    public class BlkFile : HoYoFile
    {
        public const string RootPath = "D:\\WORK\\Genshin\\GI_Out_AB";
        public const string OutputPath = "D:\\WORK\\Genshin\\GI_Org_AB";
        byte[] output;
        string cacheFilename;
        public BlkFile(FileReader reader,string fileName)
        {
            reader.Endian = EndianType.LittleEndian;

            var magic = reader.ReadStringToNull();
            if (magic != "blk")
                throw new Exception("not a blk");

            var count = reader.ReadInt32();
            var key = reader.ReadBytes(count);
            reader.ReadBytes(count);

            var blockSize = reader.ReadUInt16();
            var data = reader.ReadBytes((int)(reader.Length - reader.Position));

            output = Crypto.Decrypt(key, data, blockSize);
            cacheFilename = fileName.Replace(OutputPath, RootPath);

            string directoryPath = Path.GetDirectoryName(cacheFilename);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            File.WriteAllBytes(cacheFilename, output);
        }


        public BlkFile(FileReader reader)
        {
            reader.Endian = EndianType.LittleEndian;

            var magic = reader.ReadStringToNull();
            if (magic != "blk")
                throw new Exception("not a blk");

            var count = reader.ReadInt32();
            var key = reader.ReadBytes(count);
            reader.ReadBytes(count);

            var blockSize = reader.ReadUInt16();
            var data = reader.ReadBytes((int)(reader.Length - reader.Position));

            data = Crypto.Decrypt(key, data, blockSize);


            using (var ms = new MemoryStream(data))
            using (var subReader = new EndianBinaryReader(ms, reader.Endian))
            {
                long pos = -1;
                try
                {
                    if (reader.BundlePos.Length != 0)
                    {
                        for (int i = 0; i < reader.BundlePos.Length; i++)
                        {
                            pos = reader.BundlePos[i];
                            subReader.Position = pos;
                            var mhy0 = new Mhy0File(subReader, reader.FullPath);
                            Bundles.Add(pos, mhy0.FileList);
                        }
                    }
                    else
                    {
                        while (subReader.Position != subReader.BaseStream.Length)
                        {
                            pos = subReader.Position;
                            var mhy0 = new Mhy0File(subReader, reader.FullPath);
                            Bundles.Add(pos, mhy0.FileList);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load a mhy0 at {string.Format("0x{0:x8}", pos)} in {Path.GetFileName(reader.FullPath)}");
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
}
