using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terraria;
using Terraria.ModLoader.IO;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.Templates
{
    /// <summary>
    /// .eopjbp 样板房包读写：目录三文件（meta.json + structure.tag + replace.bin）或单文件打包。
    /// </summary>
    public static class BlueprintTemplateIO
    {
        public const string Extension = ".eopjbp";
        public const string MetaFileName = "meta.json";
        /// <summary>EBST 二进制结构层；放在 Data/ 下，勿放入 Assets/（会被 tML 当贴图解析）。</summary>
        public const string StructureFileName = "structure.ebst";
        public const string ReplaceFileName = "replace.bin";

        /// <summary>模组内嵌模板根目录（相对 ModSources 根，打包进 .tmod）。</summary>
        public const string BuiltinTemplatesModPath = "Data/Blueprint/Templates";

        private static readonly byte[] PackageMagic = Encoding.ASCII.GetBytes("EOPJBP");
        private const byte PackageVersion = 1;

        private static readonly byte[] StructureMagic = Encoding.ASCII.GetBytes("EBST");
        private const byte StructureFormatVersion = 1;
        private const int StructureBytesPerCell = 4;

        private static readonly byte[] ReplaceMagic = Encoding.ASCII.GetBytes("EBPR");
        private const byte ReplaceFormatVersion = 1;
        private const int ReplaceBytesPerCell = 7;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        public static string PlayerTemplatesDirectory =>
            Path.Combine(Main.SavePath, "EvenMoreOverpoweredJourney", "BlueprintTemplates");

        public static void SaveDirectory(BlueprintTemplate template, string directoryPath)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException("Directory path required.", nameof(directoryPath));

            Directory.CreateDirectory(directoryPath);
            BlueprintTemplateMeta meta = BlueprintTemplateMeta.FromTemplate(template);
            File.WriteAllText(
                Path.Combine(directoryPath, MetaFileName),
                JsonSerializer.Serialize(meta, JsonOptions),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            WriteStructureTag(template, Path.Combine(directoryPath, StructureFileName));
            WriteReplaceBin(template, Path.Combine(directoryPath, ReplaceFileName));
        }

        public static BlueprintTemplate LoadDirectory(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException("Directory path required.", nameof(directoryPath));
            if (!Directory.Exists(directoryPath))
                throw new BlueprintTemplateIOException($"Template directory not found: {directoryPath}");

            string metaPath = Path.Combine(directoryPath, MetaFileName);
            string structurePath = Path.Combine(directoryPath, StructureFileName);
            string replacePath = Path.Combine(directoryPath, ReplaceFileName);
            EnsureFileExists(metaPath);
            EnsureFileExists(structurePath);
            EnsureFileExists(replacePath);

            BlueprintTemplateMeta meta = JsonSerializer.Deserialize<BlueprintTemplateMeta>(
                File.ReadAllText(metaPath, Encoding.UTF8), JsonOptions)
                ?? throw new BlueprintTemplateIOException($"Invalid meta.json: {metaPath}");

            StructureCell[] structure = ReadStructureTag(structurePath, meta.Width, meta.Height);
            ReplaceRule[] rules = ReadReplaceBin(replacePath, meta.Width, meta.Height);

            var template = new BlueprintTemplate(
                string.IsNullOrEmpty(meta.Id) ? Path.GetFileName(directoryPath.TrimEnd(Path.DirectorySeparatorChar)) : meta.Id,
                meta.DisplayNameKey ?? "",
                meta.Width,
                meta.Height,
                structure,
                rules);

            meta.ValidateAgainst(template);
            return template;
        }

        public static void SavePackage(BlueprintTemplate template, string packagePath)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));
            if (string.IsNullOrWhiteSpace(packagePath))
                throw new ArgumentException("Package path required.", nameof(packagePath));

            if (!packagePath.EndsWith(Extension, StringComparison.OrdinalIgnoreCase))
                packagePath += Extension;

            BlueprintTemplateMeta meta = BlueprintTemplateMeta.FromTemplate(template);
            byte[] metaBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(meta, JsonOptions));
            byte[] structureBytes = EncodeStructureTag(template);
            byte[] replaceBytes = EncodeReplaceBin(template);

            string dir = Path.GetDirectoryName(packagePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            using var stream = File.Create(packagePath);
            using var writer = new BinaryWriter(stream);
            writer.Write(PackageMagic);
            writer.Write(PackageVersion);
            writer.Write(metaBytes.Length);
            writer.Write(structureBytes.Length);
            writer.Write(replaceBytes.Length);
            writer.Write(metaBytes);
            writer.Write(structureBytes);
            writer.Write(replaceBytes);
        }

        public static BlueprintTemplate LoadPackage(string packagePath)
        {
            if (string.IsNullOrWhiteSpace(packagePath))
                throw new ArgumentException("Package path required.", nameof(packagePath));
            if (!File.Exists(packagePath))
                throw new BlueprintTemplateIOException($"Package not found: {packagePath}");

            using var stream = File.OpenRead(packagePath);
            using var reader = new BinaryReader(stream);
            byte[] magic = reader.ReadBytes(PackageMagic.Length);
            if (!MagicMatches(magic, PackageMagic))
                throw new BlueprintTemplateIOException($"Invalid package magic in {packagePath}.");

            byte version = reader.ReadByte();
            if (version != PackageVersion)
                throw new BlueprintTemplateIOException($"Unsupported package version {version} in {packagePath}.");

            int metaLen = reader.ReadInt32();
            int structureLen = reader.ReadInt32();
            int replaceLen = reader.ReadInt32();
            if (metaLen < 0 || structureLen < 0 || replaceLen < 0)
                throw new BlueprintTemplateIOException($"Invalid section lengths in {packagePath}.");

            byte[] metaBytes = reader.ReadBytes(metaLen);
            byte[] structureBytes = reader.ReadBytes(structureLen);
            byte[] replaceBytes = reader.ReadBytes(replaceLen);

            BlueprintTemplateMeta meta = JsonSerializer.Deserialize<BlueprintTemplateMeta>(
                Encoding.UTF8.GetString(metaBytes), JsonOptions)
                ?? throw new BlueprintTemplateIOException("Package meta.json is empty.");

            StructureCell[] structure = DecodeStructureTag(structureBytes, meta.Width, meta.Height);
            ReplaceRule[] rules = DecodeReplaceBin(replaceBytes, meta.Width, meta.Height);

            var template = new BlueprintTemplate(
                meta.Id,
                meta.DisplayNameKey ?? "",
                meta.Width,
                meta.Height,
                structure,
                rules);

            meta.ValidateAgainst(template);
            return template;
        }

        public static bool TryLoadFromModAssets(Terraria.ModLoader.Mod mod, string templateId, out BlueprintTemplate template)
        {
            template = null;
            if (mod == null || string.IsNullOrWhiteSpace(templateId))
                return false;

            string basePath = $"{BuiltinTemplatesModPath}/{templateId}";
            string metaPath = $"{basePath}/{MetaFileName}";
            string structurePath = $"{basePath}/{StructureFileName}";
            string replacePath = $"{basePath}/{ReplaceFileName}";

            try
            {
                byte[] metaBytes = mod.GetFileBytes(metaPath);
                byte[] structureBytes = mod.GetFileBytes(structurePath);
                byte[] replaceBytes = mod.GetFileBytes(replacePath);
                if (metaBytes == null || metaBytes.Length == 0
                    || structureBytes == null || structureBytes.Length == 0
                    || replaceBytes == null || replaceBytes.Length == 0)
                    return false;

                BlueprintTemplateMeta meta = JsonSerializer.Deserialize<BlueprintTemplateMeta>(
                    Encoding.UTF8.GetString(metaBytes), JsonOptions)
                    ?? throw new BlueprintTemplateIOException($"Invalid meta.json for template {templateId}.");

                StructureCell[] structure = DecodeStructureTag(structureBytes, meta.Width, meta.Height);
                ReplaceRule[] rules = DecodeReplaceBin(replaceBytes, meta.Width, meta.Height);
                template = new BlueprintTemplate(
                    string.IsNullOrEmpty(meta.Id) ? templateId : meta.Id,
                    meta.DisplayNameKey ?? "",
                    meta.Width,
                    meta.Height,
                    structure,
                    rules);
                meta.ValidateAgainst(template);
                return true;
            }
            catch (Exception ex)
            {
                FurnitureBlueprintLog.Warn($"TryLoadFromModAssets failed id={templateId}: {ex.Message}");
                template = null;
                return false;
            }
        }

        public static bool TryLoadDirectory(string directoryPath, out BlueprintTemplate template)
        {
            template = null;
            try
            {
                if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
                    return false;
                if (!File.Exists(Path.Combine(directoryPath, MetaFileName)))
                    return false;
                template = LoadDirectory(directoryPath);
                return true;
            }
            catch (Exception ex)
            {
                FurnitureBlueprintLog.Warn($"TryLoadDirectory failed path={directoryPath}: {ex.Message}");
                return false;
            }
        }

        public static bool TryLoadPackage(string packagePath, out BlueprintTemplate template)
        {
            template = null;
            try
            {
                if (string.IsNullOrWhiteSpace(packagePath) || !File.Exists(packagePath))
                    return false;
                template = LoadPackage(packagePath);
                return true;
            }
            catch (Exception ex)
            {
                FurnitureBlueprintLog.Warn($"TryLoadPackage failed path={packagePath}: {ex.Message}");
                return false;
            }
        }

        public static bool TemplatesEqual(BlueprintTemplate a, BlueprintTemplate b)
        {
            if (a == null || b == null)
                return false;
            if (a.Width != b.Width || a.Height != b.Height)
                return false;
            if (!string.Equals(a.Id, b.Id, StringComparison.Ordinal))
                return false;
            if (!string.Equals(a.DisplayNameKey, b.DisplayNameKey, StringComparison.Ordinal))
                return false;

            int count = a.Width * a.Height;
            for (int i = 0; i < count; i++)
            {
                if (!StructureCellsEqual(a.Structure[i], b.Structure[i]))
                    return false;
                if (!ReplaceRulesEqual(a.ReplaceRules[i], b.ReplaceRules[i]))
                    return false;
            }

            return true;
        }

        private static void WriteStructureTag(BlueprintTemplate template, string path)
        {
            byte[] bytes = EncodeStructureTag(template);
            File.WriteAllBytes(path, bytes);
        }

        private static byte[] EncodeStructureTag(BlueprintTemplate template)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            writer.Write(StructureMagic);
            writer.Write(StructureFormatVersion);
            writer.Write(template.Width);
            writer.Write(template.Height);

            foreach (StructureCell cell in template.Structure)
            {
                writer.Write((byte)cell.Content);
                writer.Write((byte)cell.Kind);
                writer.Write((byte)(cell.HasWall ? 1 : 0));
                writer.Write((byte)(cell.Flip ? 1 : 0));
            }

            return ms.ToArray();
        }

        private static StructureCell[] ReadStructureTag(string path, int width, int height)
        {
            byte[] bytes = File.ReadAllBytes(path);
            return DecodeStructureTag(bytes, width, height);
        }

        private static StructureCell[] DecodeStructureTag(byte[] bytes, int width, int height)
        {
            int count = width * height;
            if (bytes.Length >= StructureMagic.Length && MagicMatches(bytes.AsSpan(0, StructureMagic.Length), StructureMagic))
                return DecodeStructureBinary(bytes, width, height);

            return DecodeStructureLegacyTagIo(bytes, width, height);
        }

        private static StructureCell[] DecodeStructureBinary(byte[] bytes, int width, int height)
        {
            int count = width * height;
            int expectedLen = StructureMagic.Length + 1 + 4 + 4 + count * StructureBytesPerCell;
            if (bytes.Length < expectedLen)
                throw new BlueprintTemplateIOException($"structure.tag too short ({bytes.Length} < {expectedLen}).");

            using var ms = new MemoryStream(bytes);
            using var reader = new BinaryReader(ms);
            byte[] magic = reader.ReadBytes(StructureMagic.Length);
            if (!MagicMatches(magic, StructureMagic))
                throw new BlueprintTemplateIOException("Invalid structure.tag magic.");

            byte version = reader.ReadByte();
            if (version != StructureFormatVersion)
                throw new BlueprintTemplateIOException($"Unsupported structure.tag version {version}.");

            int fileWidth = reader.ReadInt32();
            int fileHeight = reader.ReadInt32();
            if (fileWidth != width || fileHeight != height)
                throw new BlueprintTemplateIOException(
                    $"structure.tag size {fileWidth}x{fileHeight} != meta {width}x{height}.");

            var structure = new StructureCell[count];
            for (int i = 0; i < count; i++)
            {
                structure[i] = new StructureCell
                {
                    Content = (StructureCellContent)reader.ReadByte(),
                    Kind = (FurnitureSlotKind)reader.ReadByte(),
                    HasWall = reader.ReadByte() != 0,
                    Flip = reader.ReadByte() != 0
                };
            }

            return structure;
        }

        private static StructureCell[] DecodeStructureLegacyTagIo(byte[] bytes, int width, int height)
        {
            int count = width * height;
            TagCompound tag;
            using (var ms = new MemoryStream(bytes))
                tag = TagIO.FromStream(ms, compressed: true);

            int ver = tag.ContainsKey("ver") ? tag.GetInt("ver") : 0;
            if (ver != BlueprintTemplate.FormatVersion)
                throw new BlueprintTemplateIOException($"Unsupported legacy structure.tag version {ver}.");

            IList<TagCompound> cells = tag.GetList<TagCompound>("cells");
            if (cells.Count != count)
                throw new BlueprintTemplateIOException($"structure.tag cell count {cells.Count} != {count}.");

            var structure = new StructureCell[count];
            for (int i = 0; i < count; i++)
            {
                TagCompound c = cells[i];
                structure[i] = new StructureCell
                {
                    Content = (StructureCellContent)c.GetByte("c"),
                    Kind = (FurnitureSlotKind)c.GetByte("k"),
                    HasWall = c.GetBool("w"),
                    Flip = c.GetBool("f")
                };
            }

            return structure;
        }

        private static void WriteReplaceBin(BlueprintTemplate template, string path)
        {
            File.WriteAllBytes(path, EncodeReplaceBin(template));
        }

        private static byte[] EncodeReplaceBin(BlueprintTemplate template)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            writer.Write(ReplaceMagic);
            writer.Write(ReplaceFormatVersion);
            writer.Write(template.Width);
            writer.Write(template.Height);

            foreach (ReplaceRule rule in template.ReplaceRules)
            {
                writer.Write((byte)rule.Mode);
                writer.Write((byte)rule.SlotKind);
                writer.Write(rule.GroupId);
                writer.Write(rule.FixedItemType);
            }

            return ms.ToArray();
        }

        private static ReplaceRule[] ReadReplaceBin(string path, int width, int height)
        {
            return DecodeReplaceBin(File.ReadAllBytes(path), width, height);
        }

        private static ReplaceRule[] DecodeReplaceBin(byte[] bytes, int width, int height)
        {
            int count = width * height;
            int expectedLen = ReplaceMagic.Length + 1 + 4 + 4 + count * ReplaceBytesPerCell;
            if (bytes.Length < expectedLen)
                throw new BlueprintTemplateIOException($"replace.bin too short ({bytes.Length} < {expectedLen}).");

            using var ms = new MemoryStream(bytes);
            using var reader = new BinaryReader(ms);
            byte[] magic = reader.ReadBytes(ReplaceMagic.Length);
            if (!MagicMatches(magic, ReplaceMagic))
                throw new BlueprintTemplateIOException("Invalid replace.bin magic.");

            byte version = reader.ReadByte();
            if (version != ReplaceFormatVersion)
                throw new BlueprintTemplateIOException($"Unsupported replace.bin version {version}.");

            int fileWidth = reader.ReadInt32();
            int fileHeight = reader.ReadInt32();
            if (fileWidth != width || fileHeight != height)
                throw new BlueprintTemplateIOException(
                    $"replace.bin size {fileWidth}x{fileHeight} != meta {width}x{height}.");

            var rules = new ReplaceRule[count];
            for (int i = 0; i < count; i++)
            {
                rules[i] = new ReplaceRule
                {
                    Mode = (ReplaceMode)reader.ReadByte(),
                    SlotKind = (FurnitureSlotKind)reader.ReadByte(),
                    GroupId = reader.ReadByte(),
                    FixedItemType = reader.ReadInt32()
                };
            }

            return rules;
        }

        private static bool StructureCellsEqual(StructureCell a, StructureCell b) =>
            a.Content == b.Content
            && a.Kind == b.Kind
            && a.HasWall == b.HasWall
            && a.Flip == b.Flip;

        private static bool ReplaceRulesEqual(ReplaceRule a, ReplaceRule b) =>
            a.Mode == b.Mode
            && a.SlotKind == b.SlotKind
            && a.GroupId == b.GroupId
            && a.FixedItemType == b.FixedItemType;

        private static bool MagicMatches(ReadOnlySpan<byte> actual, byte[] expected)
        {
            if (actual.Length != expected.Length)
                return false;
            for (int i = 0; i < expected.Length; i++)
            {
                if (actual[i] != expected[i])
                    return false;
            }
            return true;
        }

        private static bool MagicMatches(byte[] actual, byte[] expected)
        {
            if (actual.Length != expected.Length)
                return false;
            for (int i = 0; i < expected.Length; i++)
            {
                if (actual[i] != expected[i])
                    return false;
            }
            return true;
        }

        private static void EnsureFileExists(string path)
        {
            if (!File.Exists(path))
                throw new BlueprintTemplateIOException($"Missing template file: {path}");
        }
    }
}
