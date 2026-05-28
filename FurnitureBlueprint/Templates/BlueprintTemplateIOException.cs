using System;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.Templates
{
    public sealed class BlueprintTemplateIOException : Exception
    {
        public BlueprintTemplateIOException(string message) : base(message) { }

        public BlueprintTemplateIOException(string message, Exception inner) : base(message, inner) { }
    }
}
