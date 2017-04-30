using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Airi.TheSystem.Syntax;
using Airi.TheSystem.Instruction;
using System.Reflection;

namespace Airi.TheSystem.Memory
{
    /// <summary>
    /// Provides interface for accessing patterns
    /// </summary>
    /// <interpretation>
    /// A pattern is called a pattern because
    ///     - It has identifiable features
    ///     - It repeats
    /// </interpretation>
    internal class PatternMemory
    {
        public PatternMemory()
        {
            Vocabulary = new VocabularyManager();
            RecognizedPatterns = new Dictionary<string, Pattern>();
            LoadPatterns();
        }

        // Currently loaded patterns
        Dictionary<string, Pattern> RecognizedPatterns { get; set; }
        VocabularyManager Vocabulary;

        // For loading patterns from internal SPatternExp files
        private void LoadPatterns()
        {
            StreamReader reader;
            try
            {
                reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(Airi.Properties.Resources.PatternDefinitionPath));
                if(reader != null)
                {
                    string fileContent = reader.ReadToEnd();
                    string[] lines = fileContent.Split(new char[] { '\n', '\r' });

                    foreach (string line in lines)
                    {
                        if (line == String.Empty) continue;
                        // Add a pattern to memory
                        Pattern pattern = GeneratePatternFromExpression(line);
                        if (pattern != null)
                            RecognizedPatterns.Add(pattern.Name, pattern);
                    }
                }
            }
            catch
            {
                throw new Exception();
            }
        }

        // For validating grammar of external SPatternExp files
        // Output erros to console window when encountered
        private void ValidatePatternDefinitions(string filePath)
        {
            int nErrors = 0;
            // Check file existence
            if (File.Exists(filePath))
            {
                string fileContent = File.ReadAllText(filePath);
                string[] lines = fileContent.Split(new char[] { '\n', '\r' });

                int lineNumber = 0;
                foreach (string line in lines)
                {
                    if (line == String.Empty) continue;
                    try
                    {
                        // Check whether indicate a valid pattern by try to generate a pattern from it
                        GeneratePatternFromExpression(line);
                    }
                    catch (InvalidPatternStringException e)
                    {
                        System.Console.WriteLine(string.Format("Invalid syntaxt at line {0}: {1}"), lineNumber, e.Message);
                        nErrors++;
                    }
                    lineNumber++;
                }
            }

            if(nErrors == 0)
            {
                System.Console.WriteLine("Pattern definition is valid without error.");
            }
            else
            {
                System.Console.WriteLine(string.Format("Total errors encountered: {0}"), nErrors);
            }
        }

        // Interaction Interface: Find which patterns a given sentence matches (Ideally any sentence matches only one pattern because we require EXACT match but designers can make mistakes so we return all matches)
        public List<PatternInstance> FindMatchPatterns(string sentence)
        {
            List<PatternInstance> matchedPatterns = new List<PatternInstance>();

            foreach (KeyValuePair<string, Pattern> entry in RecognizedPatterns)
            {
                PatternInstance result = entry.Value.Match(sentence, Vocabulary);
                if (result != null) matchedPatterns.Add(result);
            }

            return matchedPatterns;
        }

        private enum PatternExtractionState
        {
            SpecificWord,
            VarietyWord,
            WordAttribute,
            SubPattern,
            Choice,
            WhiteSpace, // Whitespace for a new pattern, or beginning state for a choice
            Tag,
            CategoryInclusive,
            PendingUnknown,
            PendingCategoryExclusive
        }

        // Helper for continously reading definition file lines
        private bool bCommentBlock = false;
        /// <summary>
        /// Generate a pattern object from pattern definition string
        /// This function can also be used to validate input expressions
        /// </summary>
        /// <param name="patternString">A line input from definition file</param>
        /// <returns>Returns a valid pattern if found, null if empty, or throw InvalidPatternStringException when input pattern string isn't recognized SPatternExp format</returns>
        // Implementated using a state machine; Credit: inspired by RegExp and mentioned by Charlie in 243 lab
        private Pattern GeneratePatternFromExpression(string patternString)
        {
            // Preprocessing
            patternString = patternString.Trim();   // Remove starting and trailing white spaces

            // Comments handling
            // "//" Style comments: just strip it
            if (patternString.Contains("//")) patternString = patternString.Substring(0, patternString.IndexOf("//"));
            // "/**/" Style comments: just strip them as well; If they do not form a pair in current line then handle specially
            int startComment = patternString.IndexOf("/*");
            int endComment = patternString.IndexOf("*/");
            while (startComment != -1 || endComment != -1)
            {
                if (startComment != -1) // Enter a coment block
                {
                    bCommentBlock = true;
                }
                if (endComment != -1)   // Exit it
                {
                    bCommentBlock = false;
                }
                if (startComment != -1 && endComment != -1) // Embeded comment block in a line
                {
                    patternString = patternString.Substring(0, startComment) + patternString.Substring(endComment + 2);
                }

                // Check for multiple comment blocks in a single line
                startComment = patternString.IndexOf("/*");
                endComment = patternString.IndexOf("*/");
            }
            if (bCommentBlock)   // If we are still in comment block then continue
                return null;

            // Ignore white spaces
            if (String.IsNullOrWhiteSpace(patternString) == true) return null;

            // Check pattern validity: pattern must contain a name, must have at least one element after the name, All elements must be properly enclosed
            if (patternString.Contains(":") == false)
            {
                throw new InvalidPatternStringException("Pattern doesn't have a name and it's recommended (required) to have one");
            }
            if (patternString.Substring(patternString.LastIndexOf(':') + 1).Split(new char[] { ' ' }).Count() < 1) // <Improvement> This is not yet a robust check
            {
                throw new InvalidPatternStringException("Pattern contain not enough elements after pattern name");
            }
            if (CountSubStringOccurrences(patternString, "\"") % 2 != 0)
            {
                throw new InvalidPatternStringException("Pattern name not well bound by \"\"");
            }
            if (CountSubStringOccurrences(patternString, "'") % 2 != 0)
            {
                throw new InvalidPatternStringException("Pattern word phrase elemetns not well bound by '");
            }
            // Check for [] and <> and ()
            if (patternString.Contains("[") && (CountSubStringOccurrences(patternString, "[") != CountSubStringOccurrences(patternString, "]")))
            {
                throw new InvalidPatternStringException("Pattern choise elements not well bound by []");
            }
            if (patternString.Contains("<") && (CountSubStringOccurrences(patternString, "<") != (CountSubStringOccurrences(patternString, ">") - CountSubStringOccurrences(patternString, "-->"))))
            {
                throw new InvalidPatternStringException("Pattern word attribute elements not well bound by <>");
            }
            if (patternString.Contains("(") && (CountSubStringOccurrences(patternString, "<") != (CountSubStringOccurrences(patternString, ")"))))
            {
                throw new InvalidPatternStringException("Pattern word attribute elements not well bound by <>");
            }
            else
            {
                // Basic Information extraction
                // Get pattern name
                string patternName = patternString.Substring(0, patternString.IndexOf(':'));
                // Extract action for later processing
                string actionName = null;
                int actionIndex = patternString.IndexOf("-->");
                if (actionIndex != -1)
                {
                    actionName = patternString.Substring(actionIndex + 3).Replace(" ", String.Empty);
                    patternString = patternString.Substring(0, actionIndex);
                }
                Pattern newPattern = new Pattern(patternName, actionName);
                // Get remaining string
                patternString = patternString.Substring(patternString.LastIndexOf(':') + 1).Replace('\t', ' ');

                // Initialize Elemet extracting states
                bInOptionalScope = false;
                bInWordPhraseScope = false;
                bInSubPatternNameScope = false;
                bInChoiceScope = false;
                bInAttributeScope = false;

                // State Variables
                int location = 0;   // Index of already processed characters
                elementCounter = 0;
                bLastCharacter = false;
                PatternExtractionState patternExtractionState = PatternExtractionState.WhiteSpace;

                // Iteratively walk through all characters
                foreach (char c in patternString)
                {
                    if ((location + 1) == patternString.Length) bLastCharacter = true;
                    else bLastCharacter = false;
                    switch (patternExtractionState)
                    {
                        // Theoretically all below states except white space shouldn't return null -- it indicates an error
                        case PatternExtractionState.SpecificWord:
                            {
                                PatternElement extractedElement = ExtractSpecificWord(c, patternString, location);
                                if (extractedElement != null)
                                {
                                    newPattern.Elements.Add(extractedElement);
                                    patternExtractionState = PatternExtractionState.WhiteSpace;
                                }
                                else throw new InvalidPatternStringException(string.Format("Invalid state definition inside PatternExtractionState.SpecificWord at index {0} of pattern string {1}: this is a program logic error", location, patternString));
                            }
                            break;
                        case PatternExtractionState.VarietyWord:
                            {
                                PatternElement extractedElement = ExtractVarietyWord(c, patternString, location);
                                if (extractedElement != null)
                                {
                                    newPattern.Elements.Add(extractedElement);
                                    patternExtractionState = PatternExtractionState.WhiteSpace;
                                }
                                else throw new InvalidPatternStringException(string.Format("Invalid state definition inside PatternExtractionState.VarietyWord at index {0} of pattern string {1}: this is a program logic error", location, patternString));
                            }
                            break;
                        case PatternExtractionState.WordAttribute:
                            {
                                PatternElement extractedElement = ExtractWordAttribute(c, patternString, location);
                                if (extractedElement != null)
                                {
                                    newPattern.Elements.Add(extractedElement);
                                    patternExtractionState = PatternExtractionState.WhiteSpace;
                                }
                                else throw new InvalidPatternStringException(string.Format("Invalid state definition inside PatternExtractionState.WordAttribute at index {0} of pattern string {1}: this is a program logic error", location, patternString));
                            }
                            break;
                        case PatternExtractionState.SubPattern:
                            {
                                PatternElement extractedElement = ExtractSubPattern(c, patternString, location, patternName);
                                if (extractedElement != null)
                                {
                                    newPattern.Elements.Add(extractedElement);
                                    patternExtractionState = PatternExtractionState.WhiteSpace;
                                }
                                else throw new InvalidPatternStringException(string.Format("Invalid state definition inside PatternExtractionState.SubPattern at index {0} of pattern string {1}: this is a program logic error", location, patternString));
                            }
                            break;
                        case PatternExtractionState.Choice:
                            {
                                PatternElement extractedElement = ExtractChoice(c, patternString, location, patternName);
                                if (extractedElement != null)
                                {
                                    newPattern.Elements.Add(extractedElement);
                                    patternExtractionState = PatternExtractionState.WhiteSpace;
                                }
                                else throw new InvalidPatternStringException(string.Format("Invalid state definition inside PatternExtractionState.Choice at index {0} of pattern string {1}: this is a program logic error", location, patternString));
                            }
                            break;
                        case PatternExtractionState.WhiteSpace:
                            // Common cases
                            if (c == '(')
                            {
                                bInOptionalScope = true;
                            }
                            if (c == ')')
                            {
                                if (bInOptionalScope) bInOptionalScope = false;
                                else throw new InvalidPatternStringException(string.Format("Invalid optional element definition at index {0} of pattern string {1}: unbalanced () scope", location, patternString));
                            }
                            if (c == '\'')
                            {
                                bInWordPhraseScope = true;
                                patternExtractionState = PatternExtractionState.SpecificWord;
                            }
                            if(englishCharacters.Contains(Char.ToLower(c)))
                            {
                                patternExtractionState = PatternExtractionState.SpecificWord;
                            }
                            if(c == '\"')
                            {
                                bInSubPatternNameScope = true;
                                patternExtractionState = PatternExtractionState.SubPattern;
                            }
                            if(c == '~')
                            {
                                patternExtractionState = PatternExtractionState.VarietyWord;
                            }
                            if(c == '<')
                            {
                                bInAttributeScope = true;
                                patternExtractionState = PatternExtractionState.WordAttribute;
                            }
                            if (c == '[')
                            {
                                bInChoiceScope = true;
                                choiceExtractionState = PatternExtractionState.WhiteSpace;
                                choiceCandidates = new List<PatternElement>();
                                patternExtractionState = PatternExtractionState.Choice;
                            }
                            if (c == '@')
                            {
                                patternExtractionState = PatternExtractionState.CategoryInclusive;
                            }
                            if (c == '#')
                            {
                                patternExtractionState = PatternExtractionState.Tag;
                            }
                            // Special handling
                            if (c == '!')
                            {
                                patternExtractionState = PatternExtractionState.PendingCategoryExclusive;
                            }
                            if (c == '?')
                            {
                                patternExtractionState = PatternExtractionState.PendingUnknown;
                            }
                            if(":,.-".Contains(c))  // ?! specially handled in above two cases
                            {
                                newPattern.Elements.Add(new PatternElement(PatternElementType.Punctuation, c.ToString(), bInOptionalScope));  // Puncuations can be optional
                            }
                            // If none of above is satisfied then we are probably in an embarrasing situation
                            if(patternExtractionState == PatternExtractionState.WhiteSpace && c != ' ')
                            {
                                throw new InvalidPatternStringException(string.Format("Unrecognized pattern definition symbol \'{0}\' at index {1} of pattern string \"{2}\"", c, location, patternString));
                            }
                            // Ending condition check
                            if(bLastCharacter && (bInOptionalScope  || bInWordPhraseScope || bInSubPatternNameScope || bInChoiceScope || bInAttributeScope))
                            {
                                throw new InvalidPatternStringException(string.Format("Unbalanced scope definition at end of line in pattern string {0}", patternString));
                            }
                            break;
                        case PatternExtractionState.Tag:
                            {
                                PatternElement extractedElement = ExtractTag(c, patternString, location);
                                if (extractedElement != null)
                                {
                                    newPattern.Elements.Add(extractedElement);
                                    patternExtractionState = PatternExtractionState.WhiteSpace;
                                }
                                else throw new InvalidPatternStringException(string.Format("Invalid state definition inside PatternExtractionState.Tag at index {0} of pattern string {1}: this is a program logic error", location, patternString));
                            }
                            break;
                        case PatternExtractionState.CategoryInclusive:
                            {
                                PatternElement extractedElement = ExtractCategoryInclusive(c, patternString, location);
                                if (extractedElement != null)
                                {
                                    newPattern.Elements.Add(extractedElement);
                                    patternExtractionState = PatternExtractionState.WhiteSpace;
                                }
                                else throw new InvalidPatternStringException(string.Format("Invalid state definition inside PatternExtractionState.CategoryInclusive at index {0} of pattern string {1}: this is a program logic error", location, patternString));
                            }
                            break;
                        case PatternExtractionState.PendingUnknown:
                            {
                                PatternElement extractedElement = ExtractPendingUnknown(c, patternString, location);
                                if (extractedElement != null)
                                {
                                    newPattern.Elements.Add(extractedElement);
                                    patternExtractionState = PatternExtractionState.WhiteSpace;
                                }
                                else throw new InvalidPatternStringException(string.Format("Invalid state definition inside PatternExtractionState.PendingUnknown at index {0} of pattern string {1}: this is a program logic error", location, patternString));
                            }
                            break;
                        case PatternExtractionState.PendingCategoryExclusive:
                            {
                                PatternElement extractedElement = ExtractPendingCategoryExclusive(c, patternString, location);
                                if (extractedElement != null)
                                {
                                    newPattern.Elements.Add(extractedElement);
                                    patternExtractionState = PatternExtractionState.WhiteSpace;
                                }
                                else throw new InvalidPatternStringException(string.Format("Invalid state definition inside PatternExtractionState.PendingCategoryExclusive at index {0} of pattern string {1}: this is a program logic error", location, patternString));
                            }
                            break;
                        default:
                            break;
                    }

                    location++;
                }

                return newPattern;
            }
        }

        //// Pattern Extract Helpers
        // Elemet extracting states
        private bool bInOptionalScope = false;
        private bool bInWordPhraseScope = false;
        private bool bInSubPatternNameScope = false;
        private bool bInChoiceScope = false;
        private bool bInAttributeScope = false;
        private bool bLastCharacter = false;    // Whether current character is last of the pattern definition; Useful in determing ending condition other than white space, e.g. for words
        private int elementCounter = 0;    // Count progression in next word; Used only is certain states when needed
        private PatternExtractionState choiceExtractionState = PatternExtractionState.WhiteSpace;
        List<PatternElement> choiceCandidates;
        // All possible notations: a-z '' "" ~ <*+> :,.?!- [|] !@ @ # ???
        private readonly string englishCharacters = "abcdefghijklmnopqrstuvwxyz"; // Used often below for comparison so create a variable for it
        private readonly string punctuationCharacters = ":,.?!-";
        private PatternElement ExtractSpecificWord(char c, string patternString, int location)
        {
            if (bInWordPhraseScope)
            {
                if (englishCharacters.Contains(Char.ToLower(c))) elementCounter++;
                else if (c == ' ') elementCounter++;
                else if (c == '\'')
                {
                    elementCounter = 0;
                    bInWordPhraseScope = false;
                    return new PatternElement(PatternElementType.SpecificWord, patternString.Substring(location - elementCounter, elementCounter), bInOptionalScope && !bInChoiceScope);
                }
                else throw new InvalidPatternStringException(string.Format("Invalid symbol \'{0}\' at index {1} of pattern string \"{2}\"", c, location, patternString));
                if(bLastCharacter) throw new InvalidPatternStringException(string.Format("Incomplete word definition \'{0}\' at index {1} of pattern string \"{2}\": reaching end of definition", c, location, patternString));
            }
            else
            {
                if (englishCharacters.Contains(Char.ToLower(c))) elementCounter++;
                else if (c == ' ' || punctuationCharacters.Contains(c) || bLastCharacter)
                {
                    if (bInOptionalScope && !bInChoiceScope)
                    {
                        throw new InvalidPatternStringException(string.Format("Invalid word definition {0} at index {1} of pattern string \"{2}\": unbalanced () scope", patternString.Substring(location - elementCounter, elementCounter), location, patternString));
                    }
                    else
                    {
                        bInWordPhraseScope = false; // <Debug> Seems unnecessary
                        elementCounter = 0;
                        return new PatternElement(PatternElementType.SpecificWord, patternString.Substring(location - elementCounter, elementCounter), false);
                    }
                }
                else if (c == '\'') throw new InvalidPatternStringException(string.Format("Invalid word definition {0} at index {1} of pattern string \"{2}\": unbalanced \'\' scope", patternString.Substring(location - elementCounter, elementCounter), location, patternString));
                else if (c == ')')
                {
                    if (bInOptionalScope && !bInChoiceScope)
                    {
                        bInOptionalScope = false;
                        elementCounter = 0;
                        return new PatternElement(PatternElementType.SpecificWord, patternString.Substring(location - elementCounter, elementCounter), true);
                    }
                    else throw new InvalidPatternStringException(string.Format("Invalid word definition {0} at index {1} of pattern string \"{2}\": unbalanced () scope", patternString.Substring(location - elementCounter, elementCounter), location, patternString));
                }
                else throw new InvalidPatternStringException(string.Format("Invalid symbol \'{0}\' at index {1} of pattern string \"{2}\"", c, location, patternString));
            }
            return null;
        }
        private PatternElement ExtractVarietyWord(char c, string patternString, int location)
        {
            // Normally variety word doesn't need to use a phrase scope since phrases generally don't contain corresponding varieties but it doesn't hurt to support it
            if (bInWordPhraseScope)
            {
                if (englishCharacters.Contains(Char.ToLower(c))) elementCounter++;
                else if (c == ' ') elementCounter++;
                else if (c == '\'')
                {
                    bInWordPhraseScope = false;
                    elementCounter = 0;
                    return new PatternElement(PatternElementType.VarietyWord, patternString.Substring(location - elementCounter, elementCounter), bInOptionalScope && !bInChoiceScope);
                }
                else throw new InvalidPatternStringException(string.Format("Invalid symbol \'{0}\' at index {1} of pattern string \"{2}\"", c, location, patternString));
                if (bLastCharacter) throw new InvalidPatternStringException(string.Format("Incomplete word definition \'{0}\' at index {1} of pattern string \"{2}\": reaching end of definition", c, location, patternString));
            }
            else
            {
                if (englishCharacters.Contains(Char.ToLower(c))) elementCounter++;
                else if (c == ' ' || punctuationCharacters.Contains(c) || bLastCharacter)
                {
                    if (elementCounter != 0)
                    {
                        if (bInOptionalScope && !bInChoiceScope)
                        {
                            throw new InvalidPatternStringException(string.Format("Invalid variety word definition {0} at index {1} of pattern string \"{2}\": unbalanced () scope", patternString.Substring(location - elementCounter, elementCounter), location, patternString));
                        }
                        else
                        {
                            bInWordPhraseScope = false;
                            elementCounter = 0;
                            return new PatternElement(PatternElementType.VarietyWord, patternString.Substring(location - elementCounter, elementCounter), bInOptionalScope && !bInChoiceScope);
                        }
                    }
                    else throw new InvalidPatternStringException(string.Format("Expression incomplete for variety word at index {0} of pattern string {1}", location, patternString));
                }
                else if (c == '\'') throw new InvalidPatternStringException(string.Format("Invalid word definition {0} at index {1} of pattern string \"{2}\": unbalanced \'\' scope", patternString.Substring(location - elementCounter, elementCounter), location, patternString));
                else if (c == ')')
                {
                    if (bInOptionalScope && !bInChoiceScope)
                    {
                        bInOptionalScope = false;
                        elementCounter = 0;
                        return new PatternElement(PatternElementType.VarietyWord, patternString.Substring(location - elementCounter, elementCounter), true);
                    }
                    else throw new InvalidPatternStringException(string.Format("Invalid word definition {0} at index {1} of pattern string \"{2}\": unbalanced () scope", patternString.Substring(location - elementCounter, elementCounter), location, patternString));
                }
                else throw new InvalidPatternStringException(string.Format("Invalid symbol \'{0}\' at index {1} of pattern string \"{2}\"", c, location, patternString));
            }
            return null;
        }
        private PatternElement ExtractWordAttribute(char c, string patternString, int location)
        {
            // We just validate input (properly formated with known attributes and valid use of * and +, and leave actual parsiing later during matching, so we don't need to change pattern objects' definition)
            bool bInfinite = false;
            if (englishCharacters.Contains(Char.ToLower(c))) elementCounter++;
            else if (c == '>')
            {
                if (elementCounter != 0)
                {
                    elementCounter = 0;
                    bInAttributeScope = false;
                    return new PatternElement(PatternElementType.WordAttribute, patternString.Substring(location - elementCounter, elementCounter), bInOptionalScope && !bInChoiceScope);
                }
                else throw new InvalidPatternStringException(string.Format("Expression incomplete for attribute at index {0} of pattern string {1}", location, patternString));
            }
            else if (c == '*') { bInfinite = true; elementCounter++; }
            else if (c == '+')
            {
                if (bInfinite == true) throw new InvalidPatternStringException(string.Format("Invalid attribute definition {0} at index {1} of pattern string \"{2}\": repeated attribute matches can have only one element", patternString.Substring(location - elementCounter, elementCounter), location, patternString));
                else
                {
                    // Validate attribute
                    int lastPlusSignIndex = patternString.LastIndexOf('+', location - 1);
                    int attributeLength = location - 1 - lastPlusSignIndex - 1;
                    string tempAttribtue = patternString.Substring(lastPlusSignIndex, attributeLength);
                    WordAttribute testAttribute;
                    if (Enum.TryParse(tempAttribtue, out testAttribute))
                        if (Enum.IsDefined(typeof(WordAttribute), testAttribute) | testAttribute.ToString().Contains(","))
                            elementCounter++;
                        else
                            throw new InvalidPatternStringException(string.Format("Invalid attribute definition {0} at index {1} of pattern string \"{2}\": attribute symbol undefined", patternString.Substring(location - elementCounter, elementCounter), location, patternString));
                    else
                        throw new InvalidPatternStringException(string.Format("Invalid attribute definition {0} at index {1} of pattern string \"{2}\": attribute symbol undefined", patternString.Substring(location - elementCounter, elementCounter), location, patternString));
                }
            }
            else throw new InvalidPatternStringException(string.Format("Invalid symbol \'{0}\' at index {1} of pattern string \"{2}\"", c, location, patternString));
            return null;
        }
        private PatternElement ExtractSubPattern(char c, string patternString, int location, string patternName)
        {
            if (englishCharacters.Contains(Char.ToLower(c)) || c == ' ') elementCounter++;
            else if (c == '\"')
            {
                string subPatternName = patternString.Substring(location - elementCounter, elementCounter);
                if (RecognizedPatterns.ContainsKey(subPatternName))
                {
                    if (subPatternName == patternName && !bInOptionalScope && !bInChoiceScope)
                    {
                        throw new InvalidPatternStringException(string.Format("Recursive subpattern \"{0}\" isn't optional at index {1} of pattern string \"{2}\": this can cause pattern defective", subPatternName, location - elementCounter, patternString));
                    }
                    else
                    {
                        elementCounter = 0;
                        bInSubPatternNameScope = false;
                        return new PatternElement(RecognizedPatterns[subPatternName], bInOptionalScope && !bInChoiceScope);
                    }
                }
                else
                {
                    throw new InvalidPatternStringException(string.Format("Expression refers to undefined subpattern \'{0}\' at index {1} of pattern string \"{2}\"", subPatternName, location - elementCounter, patternString));
                }
            }
            else throw new InvalidPatternStringException(string.Format("Invalid symbol \'{0}\' at index {1} of pattern string \"{2}\"", c, location, patternString));
            return null;
        }
        private PatternElement ExtractChoice(char c, string patternString, int location, string patternName)
        {
            bool fakeLastCharacterBackup = bLastCharacter;
            switch (choiceExtractionState)
            {
                case PatternExtractionState.SpecificWord:
                    if (c == '|')
                    {
                        bLastCharacter = true;
                    }
                    {
                        PatternElement extractedElement = ExtractSpecificWord(c, patternString, location);
                        if (extractedElement != null)
                        {
                            choiceCandidates.Add(extractedElement);
                            choiceExtractionState = PatternExtractionState.WhiteSpace;
                        }
                        else throw new InvalidPatternStringException(string.Format("Invalid state definition inside PatternExtractionState.SpecificWord inside choice at index {0} of pattern string {1}: this is a program logic error", location, patternString));
                    }
                    bLastCharacter = fakeLastCharacterBackup;
                    break;
                case PatternExtractionState.VarietyWord:
                    if (c == '|')
                    {
                        bLastCharacter = true;
                    }
                    {
                        PatternElement extractedElement = ExtractVarietyWord(c, patternString, location);
                        if (extractedElement != null)
                        {
                            choiceCandidates.Add(extractedElement);
                            choiceExtractionState = PatternExtractionState.WhiteSpace;
                        }
                        else throw new InvalidPatternStringException(string.Format("Invalid state definition inside PatternExtractionState.VaerietyWord inside choice at index {0} of pattern string {1}: this is a program logic error", location, patternString));
                    }
                    bLastCharacter = fakeLastCharacterBackup;
                    break;
                case PatternExtractionState.WordAttribute:
                    {
                        PatternElement extractedElement = ExtractWordAttribute(c, patternString, location);
                        if (extractedElement != null)
                        {
                            choiceCandidates.Add(extractedElement);
                            choiceExtractionState = PatternExtractionState.WhiteSpace;
                        }
                        else throw new InvalidPatternStringException(string.Format("Invalid state definition inside PatternExtractionState.WordAttribute at index {0} of pattern string {1}: this is a program logic error", location, patternString));
                    }
                    break;
                case PatternExtractionState.SubPattern:
                    {
                        PatternElement extractedElement = ExtractSubPattern(c, patternString, location, patternName);
                        if (extractedElement != null)
                        {
                            choiceCandidates.Add(extractedElement);
                            choiceExtractionState = PatternExtractionState.WhiteSpace;
                        }
                        else throw new InvalidPatternStringException(string.Format("Invalid state definition inside PatternExtractionState.SubPattern inside choice at index {0} of pattern string {1}: this is a program logic error", location, patternString));
                    }
                    break;
                case PatternExtractionState.WhiteSpace:
                    // Common cases
                    if (c == '(')
                    {
                        throw new InvalidPatternStringException(string.Format("Invalid use of optional element definition at index {0} of pattern string {1}: choice options don't support optional because they are optional by default", location, patternString));
                    }
                    else if (c == ')')
                    {
                        throw new InvalidPatternStringException(string.Format("Invalid use of optional element definition at index {0} of pattern string {1}: choice options don't support optional because they are optional by default", location, patternString));
                    }
                    else if (c == '\'')
                    {
                        bInWordPhraseScope = true;
                        choiceExtractionState = PatternExtractionState.SpecificWord;
                    }
                    else if (englishCharacters.Contains(Char.ToLower(c)))
                    {
                        choiceExtractionState = PatternExtractionState.SpecificWord;
                    }
                    else if (c == '\"')
                    {
                        bInSubPatternNameScope = true;
                        choiceExtractionState = PatternExtractionState.SubPattern;
                    }
                    else if (c == '~')
                    {
                        choiceExtractionState = PatternExtractionState.VarietyWord;
                    }
                    else if (c == '<')
                    {
                        bInAttributeScope = true;
                        choiceExtractionState = PatternExtractionState.WordAttribute;
                    }
                    else if (c == '[')
                    {
                        throw new InvalidPatternStringException(string.Format("Invalid use of choice element definition at index {0} of pattern string {1}: choice cannot contain more choices", location, patternString));
                    }
                    else if (c == ']')
                    {
                        if(bInAttributeScope || bInSubPatternNameScope || bInWordPhraseScope) throw new InvalidPatternStringException(string.Format("Invalid choice definition at index {0} of pattern string {1}: incomplete scope detected", location, patternString));
                        bInChoiceScope = false;
                        choiceExtractionState = PatternExtractionState.WhiteSpace;
                        if (choiceCandidates.Count != 0)
                        {
                            return new PatternElement(choiceCandidates, bInOptionalScope);
                        }
                        else
                        {
                            throw new InvalidPatternStringException(string.Format("Empty choice at index {0} of pattern string \"{1}\"", location, patternString));
                        }
                    }
                    else if (c == '@')
                    {
                        choiceExtractionState = PatternExtractionState.CategoryInclusive;
                    }
                    else if (c == '#')
                    {
                        choiceExtractionState = PatternExtractionState.Tag;
                    }
                    // Special handling
                    else if (c == '!')
                    {
                        choiceExtractionState = PatternExtractionState.PendingCategoryExclusive;
                    }
                    else if (c == '?')
                    {
                        throw new InvalidPatternStringException(string.Format("Invalid use of unkown element definition at index {0} of pattern string {1}: choice options don't support unkown because that cause undetermed bahavior", location, patternString));
                    }
                    else
                    {
                        throw new InvalidPatternStringException(string.Format("Unrecognized pattern definition symbol \'{0}\' at index {1} of pattern string \"{2}\"", c, location, patternString));
                    }
                    // Ending condition check
                    if (bLastCharacter && (bInOptionalScope || bInWordPhraseScope || bInSubPatternNameScope || bInChoiceScope || bInAttributeScope))
                    {
                        throw new InvalidPatternStringException(string.Format("Unbalanced scope definition at end of line in pattern string {0}", patternString));
                    }
                    break;
                case PatternExtractionState.Tag:
                    if (c == '|')
                    {
                        bLastCharacter = true;
                    }
                    {
                        PatternElement extractedElement = ExtractTag(c, patternString, location);
                        if (extractedElement != null)
                        {
                            choiceCandidates.Add(extractedElement);
                            choiceExtractionState = PatternExtractionState.WhiteSpace;
                        }
                        else throw new InvalidPatternStringException(string.Format("Invalid state definition inside PatternExtractionState.Tag inside choice at index {0} of pattern string {1}: this is a program logic error", location, patternString));
                    }
                    bLastCharacter = fakeLastCharacterBackup;
                    break;
                case PatternExtractionState.CategoryInclusive:
                    if (c == '|')
                    {
                        bLastCharacter = true;
                    }
                    {
                        PatternElement extractedElement = ExtractCategoryInclusive(c, patternString, location);
                        if (extractedElement != null)
                        {
                            choiceCandidates.Add(extractedElement);
                            choiceExtractionState = PatternExtractionState.WhiteSpace;
                        }
                        else throw new InvalidPatternStringException(string.Format("Invalid state definition inside PatternExtractionState.CategoryInclusive inside choice at index {0} of pattern string {1}: this is a program logic error", location, patternString));
                    }
                    bLastCharacter = fakeLastCharacterBackup;
                    break;
                case PatternExtractionState.PendingUnknown:
                    if (c == '|')
                    {
                        bLastCharacter = true;
                    }
                    {
                        PatternElement extractedElement = ExtractPendingUnknown(c, patternString, location);
                        if (extractedElement != null)
                        {
                            choiceCandidates.Add(extractedElement);
                            choiceExtractionState = PatternExtractionState.WhiteSpace;
                        }
                        else throw new InvalidPatternStringException(string.Format("Invalid state definition inside PatternExtractionState.PendingUnknown inside choice at index {0} of pattern string {1}: this is a program logic error", location, patternString));
                    }
                    bLastCharacter = fakeLastCharacterBackup;
                    break;
                case PatternExtractionState.PendingCategoryExclusive:
                    if (c == '|')
                    {
                        bLastCharacter = true;
                    }
                    {
                        PatternElement extractedElement = ExtractPendingCategoryExclusive(c, patternString, location);
                        if (extractedElement != null)
                        {
                            choiceCandidates.Add(extractedElement);
                            choiceExtractionState = PatternExtractionState.WhiteSpace;
                        }
                        else throw new InvalidPatternStringException(string.Format("Invalid state definition inside PatternExtractionState.PendingCategoryExclusive inside choice at index {0} of pattern string {1}: this is a program logic error", location, patternString));
                    }
                    bLastCharacter = fakeLastCharacterBackup;
                    break;
                default:
                    break;
            }
            return null;
        }
        private PatternElement ExtractTag(char c, string patternString, int location)
        {
            if (englishCharacters.Contains(Char.ToLower(c))) elementCounter++;
            else if (c == ' ' || punctuationCharacters.Contains(c) || bLastCharacter)
            {
                if (elementCounter != 0)
                {
                    if (bInOptionalScope && !bInChoiceScope)
                    {
                        throw new InvalidPatternStringException(string.Format("Invalid tag definition {0} at index {1} of pattern string \"{2}\": unbalanced () scope", patternString.Substring(location - elementCounter, elementCounter), location, patternString));
                    }
                    else
                    {
                        elementCounter = 0;
                        return new PatternElement(PatternElementType.Tag, patternString.Substring(location - elementCounter, elementCounter), false);
                    }
                }
                else throw new InvalidPatternStringException(string.Format("Expression incomplete for tag at index {0} of pattern string {1}", location, patternString));
            }
            else if (c == ')')
            {
                if (bInOptionalScope && !bInChoiceScope)
                {
                    bInOptionalScope = false;
                    elementCounter = 0;
                    return new PatternElement(PatternElementType.Tag, patternString.Substring(location - elementCounter, elementCounter), true);
                }
                else throw new InvalidPatternStringException(string.Format("Invalid tag definition {0} at index {1} of pattern string \"{2}\": unbalanced () scope", patternString.Substring(location - elementCounter, elementCounter), location, patternString));
            }
            else throw new InvalidPatternStringException(string.Format("Invalid symbol \'{0}\' at index {1} of pattern string \"{2}\"", c, location, patternString));
            return null;
        }
        private PatternElement ExtractCategoryInclusive(char c, string patternString, int location)
        {

            if (englishCharacters.Contains(Char.ToLower(c))) elementCounter++;
            else if (c == ' ' || punctuationCharacters.Contains(c) || bLastCharacter)
            {
                if (elementCounter != 0)
                {
                    if (bInOptionalScope && !bInChoiceScope)
                    {
                        throw new InvalidPatternStringException(string.Format("Invalid category definition {0} at index {1} of pattern string \"{2}\": unbalanced () scope", patternString.Substring(location - elementCounter, elementCounter), location, patternString));
                    }
                    else
                    {
                        elementCounter = 0;
                        return new PatternElement(PatternElementType.CategoryInclude, patternString.Substring(location - elementCounter, elementCounter), false);
                    }
                }
                else throw new InvalidPatternStringException(string.Format("Expression incomplete for category at index {0} of pattern string {1}", location, patternString));
            }
            else if (c == ')')
            {
                if (bInOptionalScope && !bInChoiceScope)
                {
                    bInOptionalScope = false;
                    elementCounter = 0;
                    return new PatternElement(PatternElementType.CategoryInclude, patternString.Substring(location - elementCounter, elementCounter), true);
                }
                else throw new InvalidPatternStringException(string.Format("Invalid category definition {0} at index {1} of pattern string \"{2}\": unbalanced () scope", patternString.Substring(location - elementCounter, elementCounter), location, patternString));
            }
            else throw new InvalidPatternStringException(string.Format("Invalid symbol \'{0}\' at index {1} of pattern string \"{2}\"", c, location, patternString));
            return null;
        }
        private PatternElement ExtractPendingUnknown(char c, string patternString, int location)
        {
            if (c == '?')
            {
                elementCounter++;
            }
            // Ending condition: only if encounter white space, end of line, or other breaks to handle situations like ???? (redundant ? symbols)
            else if (c == ' ' || bLastCharacter)
            {
                if (bInOptionalScope && !bInChoiceScope) throw new InvalidPatternStringException(string.Format("Invalid unknown phrase definition {0} at index {1} of pattern string \"{2}\": unbalanced () scope", patternString.Substring(location - elementCounter, elementCounter), location, patternString));
                if (elementCounter == 3)
                {
                    elementCounter = 0;
                    return new PatternElement(PatternElementType.UnknownPhrase, "???", false);
                }
                else throw new InvalidPatternStringException(string.Format("Invalid unknown phrase definition {0} at index {1} of pattern string \"{2}\": auxiliar ? used", patternString.Substring(location - elementCounter, elementCounter), location, patternString));
            }
            else if (c == ')' && elementCounter == 3)
            {
                if (bInOptionalScope && !bInChoiceScope)
                {
                    elementCounter = 0;
                    return new PatternElement(PatternElementType.UnknownPhrase, "???", true);
                }
                else throw new InvalidPatternStringException(string.Format("Invalid unknown phrase definition {0} at index {1} of pattern string \"{2}\": cannot find beginning of () scope", patternString.Substring(location - elementCounter, elementCounter), location, patternString));
            }
            else
            {
                throw new InvalidPatternStringException(string.Format("Invalid definition of unknown symbol ??? at index {0} of pattern string {1}: possibly missing a space or used more '?' ?", location, patternString));
            }
            return null;
        }
        private PatternElement ExtractPendingCategoryExclusive(char c, string patternString, int location)
        {
            if (c == '@' && elementCounter == 0)
            {
                // Safe and continue
            }
            else if (englishCharacters.Contains(Char.ToLower(c)))
            {
                elementCounter++;
            }
            else if (c == ' ' && elementCounter != 0)
            {
                if (bInOptionalScope && !bInChoiceScope) throw new InvalidPatternStringException(string.Format("Invalid exclusive category definition {0} at index {1} of pattern string \"{2}\": unbalanced () scope", patternString.Substring(location - elementCounter, elementCounter), location, patternString));
                elementCounter = 0;
                return new PatternElement(PatternElementType.CategoryExclude, patternString.Substring(location - elementCounter, elementCounter), false);
            }
            else if (c == ')' && elementCounter != 0)
            {
                bInOptionalScope = false;
                elementCounter = 0;
                return new PatternElement(PatternElementType.CategoryExclude, patternString.Substring(location - elementCounter, elementCounter), bInOptionalScope && !bInChoiceScope);
            }
            else if (bLastCharacter)
            {
                if (bInOptionalScope && !bInChoiceScope) throw new InvalidPatternStringException(string.Format("Invalid exclusive category definition {0} at index {1} of pattern string \"{2}\": unbalanced () scope", patternString.Substring(location - elementCounter, elementCounter), location, patternString));
                elementCounter = 0;
                return new PatternElement(PatternElementType.CategoryExclude, patternString.Substring(location - elementCounter, elementCounter), false);
            }
            else
            {
                throw new InvalidPatternStringException(string.Format("Invalid exclusive category definition {0} at index {1} of pattern string \"{2}\": unkown character", patternString.Substring(location - elementCounter, elementCounter), location, patternString));
            }
            return null;
        }

        // Algorithm: http://stackoverflow.com/questions/541954/how-would-you-count-occurrences-of-a-string-within-a-string
        // Regarding Pass By Value: http://stackoverflow.com/questions/10792603/how-are-strings-passed-in-net, https://msdn.microsoft.com/en-us/library/system.string.copy(v=vs.110).aspx
        // Count substring's numbero f occurrences
        private int CountSubStringOccurrences(string originalString, string substring)
        {
            return (originalString.Length - originalString.Replace(substring, string.Empty).Length) / substring.Length;
        }

        // For Unloading currently learnt patterns into a file
        public void UnloadPatterns(string filePath)
        {
            throw new NotImplementedException();
        }

        // Return the overlap part of two strings (number of characters overlap), or -1 if no overlap
        public int GetStringOverlapLowerCase(string string1, string string2)
        {
            string1 = string1.ToLower();
            string2 = string2.ToLower();

            int overlap;
            int shortStringLength;
            if (string1.Length > string2.Length)
            {
                overlap = string1.IndexOf(string2);
                shortStringLength = string2.Length;
            }
            else
            {
                overlap = string2.IndexOf(string1);
                shortStringLength = string2.Length;
            }

            // If no overlap
            if (overlap == -1)
                shortStringLength = -1; 

            return shortStringLength;
        }
    }

    /// <summary>
    /// Patterns contain invalid definitions
    /// </summary>
    [Serializable]
    internal class InvalidPatternStringException : Exception
    {
        public InvalidPatternStringException() { }
        public InvalidPatternStringException(string message) : base(message) { }
        public InvalidPatternStringException(string message, Exception inner) : base(message, inner) { }
        protected InvalidPatternStringException(
          global::System.Runtime.Serialization.SerializationInfo info,
          global::System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
