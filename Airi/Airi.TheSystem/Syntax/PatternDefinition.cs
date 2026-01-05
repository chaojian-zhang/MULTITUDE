using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Airi.TheSystem.Syntax
{
    /// <summary>
    /// Helper facility to defines recognized patterns; Requires careful settup so patterns contain least overlap possible
    /// Provides check to ensure the types are safe
    /// This type is totally unnecessary since we can just use a function to check existing file's validity instead of providing a state machine
    /// </summary>
    class PatternDefinition
    {
        static void GenerateDefaultPatternSet(string outputFilePath, bool bBinary)
        {
            // Pattern Definitions
            PatternDefinition Def = new PatternDefinition();
            // Pattern: "Good [Morning Night Afternoon]"
            //Def.BeginPattern();
            //Def.AddConstant("Good");
            //Def.BeginChoice();
            //Def.AddConstant("Morning");
            //Def.AddConstant("Night");
            //Def.AddConstant("Afternoon");
            //Def.EndChoice();
            //Def.EndPattern();
            // ...

            // Save to File
            GeneratePatternsToFile(outputFilePath, bBinary);
        }

        static void GeneratePatternsToFile(string outputFilePath, bool bBinary)
        {
            // Save to file
            // ...
        }

        static bool ValidatePatternFile(string filePath)
        {
            throw new NotImplementedException();
        }

        public void BeginPattern(string patternName) { }
        public void EndPattern() { }
        public void BeginSubPattern() { }
        public void EndSubPattern() { }
        public void BeginChoice() { }
        public void EndChoice() { }

        public void AddConstant(string word) { }
        public void AddVariety(string signature) { }
        public void AddAttribute(string type) { }

        private int CurrentPattern = 0;
        private int CurrentChoice = 0;

        public List<Pattern> Patterns { get; set; }
        public Pattern TempSubPattern{ get; set; }
    }
}
