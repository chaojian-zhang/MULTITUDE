namespace Airi.TheSystem
{
    internal class Properties
    {
        /// <summary>
        /// Path to memory characters
        /// </summary>
        public static string CharacterSheetPath { get; internal set; } = "Airi.InternalResources.DataSheets.CharacterSheet.txt";
        /// <summary>
        /// Path to a descriptive experience of Airi
        /// </summary>
        public static string ExperienceSheetPath { get; internal set; } = "Airi.InternalResources.DataSheets.ExperienceSheet.txt";
        /// <summary>
        /// Path to other knowledge definitions
        /// </summary>
        public static string KnowledgeSheetPath { get; internal set; } = "Airi.InternalResources.DataSheets.KnowledgeSheet.txt";
        /// <summary>
        /// Path to default patterns for syntax recognition.
        /// </summary>
        public static string PatternDefinitionPath { get; internal set; } = "Airi.InternalResources.AiriDefaultPatterns.pat";
        /// <summary>
        /// Path to default dictionary items
        /// </summary>
        public static string VocabularySheetPath { get; internal set; } = "Airi.InternalResources.DataSheets.VocabularySheet.txt";
    }
}
