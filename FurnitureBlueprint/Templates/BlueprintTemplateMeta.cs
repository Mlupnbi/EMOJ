using System.Text.Json;
using System.Text.Json.Serialization;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.Templates
{
    /// <summary>.eopjbp 包内 meta.json 对应 DTO。</summary>
    public sealed class BlueprintTemplateMeta
    {
        public const int CurrentFormatVersion = BlueprintTemplate.FormatVersion;

        [JsonPropertyName("formatVersion")]
        public int FormatVersion { get; set; } = CurrentFormatVersion;

        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("displayNameKey")]
        public string DisplayNameKey { get; set; } = "";

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        public static BlueprintTemplateMeta FromTemplate(BlueprintTemplate template) => new()
        {
            FormatVersion = CurrentFormatVersion,
            Id = template.Id,
            DisplayNameKey = template.DisplayNameKey ?? "",
            Width = template.Width,
            Height = template.Height
        };

        public void ValidateAgainst(BlueprintTemplate template)
        {
            if (template == null)
                throw new System.ArgumentNullException(nameof(template));

            if (FormatVersion != CurrentFormatVersion)
                throw new BlueprintTemplateIOException(
                    $"Unsupported meta formatVersion={FormatVersion} (expected {CurrentFormatVersion}).");

            if (Width != template.Width || Height != template.Height)
                throw new BlueprintTemplateIOException(
                    $"Meta size {Width}x{Height} != template {template.Width}x{template.Height}.");

            if (!string.IsNullOrEmpty(Id) && !string.Equals(Id, template.Id, System.StringComparison.Ordinal))
                throw new BlueprintTemplateIOException($"Meta id '{Id}' != template id '{template.Id}'.");
        }
    }
}
