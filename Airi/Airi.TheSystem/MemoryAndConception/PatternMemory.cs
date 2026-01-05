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
            // try
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
            //catch(Exception e)
            //{
            //    throw;
            //}
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
                if (result != null && result.bPartialMatch == false) matchedPatterns.Add(result);
            }

            return matchedPatterns;
        }

        //<Pending deprecation> private enum PatternExtractionState
        //{
        //    SpecificWord,
        //    VarietyWord,
        //    WordAttribute,
        //    SubPattern,
        //    Choice,
        //    WhiteSpace, // Whitespace for a new pattern, or beginning state for a choice
        //    Tag,
        //    CategoryInclusive,
        //    PendingUnknown,
        //    PendingCategoryExclusive
        //}

        // Helper for continously reading definition file lines
        private bool bCommentBlock = false;
        /// <summary>
        /// Generate a pattern object from pattern definition string
        /// This function can also be used to validate input expressions
        /// </summary>
        /// <param name="patternString">A line input from definition file</param>
        /// <returns>Returns a valid pattern if found, null if empty, or throw InvalidPatternStringException when input pattern string isn't recognized SPatternExp format</returns>
        // Implementated using a state machine; Credit: inspired by RegExp and mentioned by Charlie in 243 lab
        // How it works: Given a line, we count elements (characters, words, seperators etc.) to decide which are which
        // In the end it turns out a state machine isn't needed and is troublesome to work with
        private Pattern GeneratePatternFromExpression(string patternString)
        {
            // Preprocessing
            patternString = patternString.Trim();   // Remove starting and trailing white spaces

            // Comments handling
            // "/**/" Style comments: just strip them; If they do not form a pair in current line then handle specially
            int startComment = patternString.IndexOf("/*");
            int endComment = patternString.IndexOf("*/");
            while (startComment != -1 || endComment != -1)
            {
                if (startComment != -1 && endComment != -1) // Embeded comment block in a line
                {
                    patternString = patternString.Substring(0, startComment) + patternString.Substring(endComment + 2);

                    // Check for multiple comment blocks in a single line
                    startComment = patternString.IndexOf("/*");
                    endComment = patternString.IndexOf("*/");
                }
                else if (startComment != -1) // Enter a coment block
                {
                    bCommentBlock = true;
                    break;
                }
                else if (endComment != -1)   // Exit it
                {
                    patternString = patternString.Substring(endComment + 2);
                    bCommentBlock = false;
                    break;
                }
            }
            if (bCommentBlock)   // If we are still in comment block then continue
                return null;
            // "//" Style comments: just strip it
            if (patternString.Contains("//")) patternString = patternString.Substring(0, patternString.IndexOf("//"));

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
            if (patternString.Contains("(") && (CountSubStringOccurrences(patternString, "(") != (CountSubStringOccurrences(patternString, ")"))))
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

                // Iteratively walk through all characters
                int location = 0; // Index into patternString
                while (location < patternString.Length)
                {
                    // Get character at current location
                    char c = patternString[location];

                    // Process elements depending on seperators
                    // Theoretically all cases except during white space shouldn't return null -- it indicates an error
                    // Common cases
                    if(c == ' ')
                    {
                        location++;
                        continue;
                    }
                    else if (c == '(')
                    {
                        bInOptionalScope = true;
                    }
                    else if (c == ')')
                    {
                        if (bInOptionalScope) bInOptionalScope = false;
                        else throw new InvalidPatternStringException(string.Format("Invalid optional element definition at index {0} of pattern string {1}: unbalanced () scope", location, patternString));
                    }
                    else if (c == '\'' || englishCharacters.Contains(Char.ToLower(c)))
                    {
                        if (c == '\'') bInWordPhraseScope = true;

                        PatternElement extractedElement = ExtractSpecificWord(patternString, ref location);
                        if (extractedElement != null)
                        {
                            newPattern.Elements.Add(extractedElement);
                        }
                        else throw new InvalidPatternStringException(string.Format("Invalid state definition inside PatternExtractionState.SpecificWord at index {0} of pattern string {1}: this is a program logic error", location, patternString));
                    }
                    else if (c == '\"')
                    {
                        bInSubPatternNameScope = true;
                        PatternElement extractedElement = ExtractSubPattern(patternString, ref location, newPattern);
                        if (extractedElement != null)
                        {
                            newPattern.Elements.Add(extractedElement);
                        }
                        else throw new InvalidPatternStringException(string.Format("Invalid state definition inside PatternExtractionState.SubPattern at index {0} of pattern string {1}: this is a program logic error", location, patternString));
                    }
                    else if (c == '~')
                    {
                        PatternElement extractedElement = ExtractVarietyWord(patternString, ref location);
                        if (extractedElement != null)
                        {
                            newPattern.Elements.Add(extractedElement);
                        }
                        else throw new InvalidPatternStringException(string.Format("Invalid state definition inside PatternExtractionState.VarietyWord at index {0} of pattern string {1}: this is a program logic error", location, patternString));
                    }
                    else if (c == '<')
                    {
                        bInAttributeScope = true;
                        PatternElement extractedElement = ExtractWordAttribute(patternString, ref location);
                        if (extractedElement != null)
                        {
                            newPattern.Elements.Add(extractedElement);
                        }
                        else throw new InvalidPatternStringException(string.Format("Invalid state definition inside PatternExtractionState.WordAttribute at index {0} of pattern string {1}: this is a program logic error", location, patternString));
                    }
                    else if (c == '[')
                    {
                        bInChoiceScope = true;
                        choiceCandidates = new List<PatternElement>();
                        PatternElement extractedElement = ExtractChoice(patternString, ref location, newPattern);
                        if (extractedElement != null)
                        {
                            newPattern.Elements.Add(extractedElement);
                        }
                        else throw new InvalidPatternStringException(string.Format("Invalid state definition inside PatternExtractionState.Choice at index {0} of pattern string {1}: this is a program logic error", location, patternString));
                    }
                    else if (c == '@')
                    {
                        PatternElement extractedElement = ExtractCategoryInclusive(patternString, ref location);
                        if (extractedElement != null)
                        {
                            newPattern.Elements.Add(extractedElement);
                        }
                        else throw new InvalidPatternStringException(string.Format("Invalid state definition inside PatternExtractionState.CategoryInclusive at index {0} of pattern string {1}: this is a program logic error", location, patternString));
                    }
                    else if (c == '#')
                    {
                        PatternElement extractedElement = ExtractTag(patternString, ref location);
                        if (extractedElement != null)
                        {
                            newPattern.Elements.Add(extractedElement);
                        }
                        else throw new InvalidPatternStringException(string.Format("Invalid state definition inside PatternExtractionState.Tag at index {0} of pattern string {1}: this is a program logic error", location, patternString));
                    }
                    // Special handling
                    else if (c == '!')
                    {
                        PatternElement extractedElement = ExtractPendingCategoryExclusive(patternString, ref location);
                        if (extractedElement != null)
                        {
                            newPattern.Elements.Add(extractedElement);
                        }
                        // Otherwise it's a punctuation
                        else newPattern.Elements.Add(new PatternElement(PatternElementType.Punctuation, c.ToString(), bInOptionalScope));
                        // else throw new InvalidPatternStringException(string.Format("Invalid state definition inside PatternExtractionState.PendingCategoryExclusive at index {0} of pattern string {1}: this is a program logic error", location, patternString));
                    }
                    else if (c == '?')
                    {
                        PatternElement extractedElement = ExtractPendingUnknown(patternString, ref location);
                        if (extractedElement != null)
                        {
                            newPattern.Elements.Add(extractedElement);
                        }
                        // Otherwise it's a punctuation
                        else newPattern.Elements.Add(new PatternElement(PatternElementType.Punctuation, c.ToString(), bInOptionalScope));
                        // else throw new InvalidPatternStringException(string.Format("Invalid state definition inside PatternExtractionState.PendingUnknown at index {0} of pattern string {1}: this is a program logic error", location, patternString));
                    }
                    else if (punctuationCharacters.Contains(c))  // Notice ?! are also specially handled in above two cases
                    {
                        newPattern.Elements.Add(new PatternElement(PatternElementType.Punctuation, c.ToString(), bInOptionalScope));  // Puncuations can be optional
                    }
                    // Ending condition check
                    else if ((location == patternString.Length - 1) && (bInOptionalScope || bInWordPhraseScope || bInSubPatternNameScope || bInChoiceScope || bInAttributeScope))   // Whether current character is last of the pattern definition
                    {
                        throw new InvalidPatternStringException(string.Format("Unbalanced scope definition at end of line in pattern string {0}", patternString));
                    }
                    // If none of above is satisfied then we are probably in an embarrasing situation
                    else
                    {
                        throw new InvalidPatternStringException(string.Format("Unrecognized pattern definition symbol \'{0}\' at index {1} of pattern string \"{2}\"", c, location, patternString));
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
        List<PatternElement> choiceCandidates;
        // All possible notations: a-z '' "" ~ <*+> :,.?!- [|] !@ @ # ???
        private static readonly string englishCharacters = "abcdefghijklmnopqrstuvwxyz"; // Represents legal characters that can occur in a word/phrase; Used often below for comparison so create a variable for it
        private static readonly string punctuationCharacters = ":,.?!-";
        // <Important Remark> All below extract functions beginning at current character location and extract specific elements pertaining to their type, then increment location variable to next location
        // <Important Remark> All extract functions consume only those location range that are valid for the specific type
        private PatternElement ExtractSpecificWord(string patternString, ref int location)
        {
            int characterCounter = 0; // Count characters that make up a word (or more accurately, phrase)
            bool bEndingSeperator = false;
            while(location <= patternString.Length)
            {
                char c = patternString[location];

                if(c == '\'' && bEndingSeperator == false)
                {
                    bInWordPhraseScope = true;
                    location++;
                    bEndingSeperator = true;
                    continue;
                }
                else if (bInWordPhraseScope)
                {
                    if (englishCharacters.Contains(Char.ToLower(c))) characterCounter++;
                    else if (c == ' ') characterCounter++;
                    else if (c == '\'')
                    {
                        bInWordPhraseScope = false;
                        return new PatternElement(PatternElementType.SpecificWord, patternString.Substring(location - characterCounter, characterCounter), bInOptionalScope && !bInChoiceScope);
                    }
                    else throw new InvalidPatternStringException(string.Format("Invalid symbol \'{0}\' at index {1} of pattern string \"{2}\"", c, location, patternString));
                    if (location == patternString.Length-1) throw new InvalidPatternStringException(string.Format("Incomplete word definition \'{0}\' at index {1} of pattern string \"{2}\": reaching end of definition", c, location, patternString));
                }
                else
                {
                    if (englishCharacters.Contains(Char.ToLower(c)))
                    {
                        characterCounter++;
                        if (location == patternString.Length - 1) return new PatternElement(PatternElementType.SpecificWord, patternString.Substring(location - characterCounter + 1, characterCounter), true); // Important to add 1 here
                    }
                    else if (c == ' ' || punctuationCharacters.Contains(c) || c == '(' || (bInChoiceScope && c == '|' || c == ']'))   // '|' for choice elements
                    {
                        if (bInOptionalScope && !bInChoiceScope)
                        {
                            throw new InvalidPatternStringException(string.Format("Invalid word definition {0} at index {1} of pattern string \"{2}\": unbalanced () scope", patternString.Substring(location - characterCounter, characterCounter), location, patternString));
                        }
                        else
                        {
                            PatternElement element = new PatternElement(PatternElementType.SpecificWord, patternString.Substring(location - characterCounter, characterCounter), false);
                            location--; // Don't consume the location that doesn't belong to us
                            return element;
                        }
                    }
                    else if (c == '\'') throw new InvalidPatternStringException(string.Format("Invalid word definition {0} at index {1} of pattern string \"{2}\": unbalanced \'\' scope", patternString.Substring(location - characterCounter, characterCounter), location, patternString));
                    else if (c == ')')
                    {
                        if (bInOptionalScope && !bInChoiceScope)
                        {
                            bInOptionalScope = false;
                            return new PatternElement(PatternElementType.SpecificWord, patternString.Substring(location - characterCounter, characterCounter), true);
                        }
                        else throw new InvalidPatternStringException(string.Format("Invalid word definition {0} at index {1} of pattern string \"{2}\": unbalanced () scope", patternString.Substring(location - characterCounter, characterCounter), location, patternString));
                    }
                    else throw new InvalidPatternStringException(string.Format("Invalid symbol \'{0}\' at index {1} of pattern string \"{2}\"", c, location, patternString));
                }
                location++;
            }
            return null;
        }
        private PatternElement ExtractVarietyWord(string patternString, ref int location)
        {
            int characterCounter = 0; // Count characters that make up a word
            bool bEndingSeperator = false;
            while (location <= patternString.Length)
            {
                char c = patternString[location];

                // Normally variety word doesn't need to use a phrase scope since phrases generally don't contain corresponding varieties but it doesn't hurt to support it
                if(c == '~') { location++; continue; }
                else if (c == '\'' && bEndingSeperator == false)
                {
                    bInWordPhraseScope = true;
                    bEndingSeperator = true;
                    location++;
                    continue;
                }
                else if (bInWordPhraseScope)
                {
                    if (englishCharacters.Contains(Char.ToLower(c))) characterCounter++;
                    else if (c == ' ') characterCounter++;
                    else if (c == '\'')
                    {
                        bInWordPhraseScope = false;
                        return new PatternElement(PatternElementType.VarietyWord, patternString.Substring(location - characterCounter, characterCounter), bInOptionalScope && !bInChoiceScope);
                    }
                    else throw new InvalidPatternStringException(string.Format("Invalid symbol \'{0}\' at index {1} of pattern string \"{2}\"", c, location, patternString));
                    if (location == patternString.Length - 1) throw new InvalidPatternStringException(string.Format("Incomplete word definition \'{0}\' at index {1} of pattern string \"{2}\": reaching end of definition", c, location, patternString));
                }
                else
                {
                    if (englishCharacters.Contains(Char.ToLower(c)))
                    {
                        characterCounter++;
                        if (location == patternString.Length - 1) return new PatternElement(PatternElementType.VarietyWord, patternString.Substring(location - characterCounter, characterCounter), true);
                    }
                    else if (c == ' ' || punctuationCharacters.Contains(c) || c == '(' || (bInChoiceScope && c == '|' || c == ']'))   // '|' for choice elements
                    {
                        if (characterCounter != 0)
                        {
                            if (bInOptionalScope && !bInChoiceScope)
                            {
                                throw new InvalidPatternStringException(string.Format("Invalid variety word definition {0} at index {1} of pattern string \"{2}\": unbalanced () scope", patternString.Substring(location - characterCounter, characterCounter), location, patternString));
                            }
                            else
                            {
                                PatternElement element = new PatternElement(PatternElementType.VarietyWord, patternString.Substring(location - characterCounter, characterCounter), bInOptionalScope && !bInChoiceScope);
                                location--; // Don't consume the location that doesn't belong to us
                                return element;
                            }
                        }
                        else throw new InvalidPatternStringException(string.Format("Expression incomplete for variety word at index {0} of pattern string {1}", location, patternString));
                    }
                    else if (c == '\'') throw new InvalidPatternStringException(string.Format("Invalid word definition {0} at index {1} of pattern string \"{2}\": unbalanced \'\' scope", patternString.Substring(location - characterCounter, characterCounter), location, patternString));
                    else if (c == ')')
                    {
                        if (bInOptionalScope && !bInChoiceScope)
                        {
                            bInOptionalScope = false;
                            return new PatternElement(PatternElementType.VarietyWord, patternString.Substring(location - characterCounter, characterCounter), true);
                        }
                        else throw new InvalidPatternStringException(string.Format("Invalid word definition {0} at index {1} of pattern string \"{2}\": unbalanced () scope", patternString.Substring(location - characterCounter, characterCounter), location, patternString));
                    }
                    else throw new InvalidPatternStringException(string.Format("Invalid symbol \'{0}\' at index {1} of pattern string \"{2}\"", c, location, patternString));
                }

                location++;
            }

            return null;
        }
        private PatternElement ExtractWordAttribute(string patternString, ref int location)
        {
            int characterCounter = 0; // Count characters that make up a word
            bool bEndingSeperator = false;
            while (location <= patternString.Length)
            {
                char c = patternString[location];

                // We just validate input (properly formated with known attributes and valid use of * and +, and leave actual parsiing later during matching, so we don't need to change pattern objects' definition)
                bool bInfinite = false;
                if (c == '<')
                {
                    if (bEndingSeperator) { throw new InvalidPatternStringException(string.Format("Invalid double occurence of \'{0}\' at index {1} of pattern string \"{2}\"", c, location, patternString)); }
                    else { bEndingSeperator = true; location++; continue; }
                }
                else if (englishCharacters.Contains(Char.ToLower(c))) characterCounter++;
                else if (c == '>')
                {
                    if (characterCounter != 0)
                    {
                        // Check condition for <any>
                        string attributeText = patternString.Substring(location - characterCounter, characterCounter);
                        if(attributeText.IndexOf("any") != -1 && attributeText.IndexOf("any") != 0 && attributeText.Length != 3) throw new InvalidPatternStringException(string.Format("Expression invalid for <any> attribute at index {0} of pattern string {1}", location, patternString));
                        // Return for valid terms
                        bInAttributeScope = false;
                        return new PatternElement(PatternElementType.WordAttribute, patternString.Substring(location - characterCounter, characterCounter), bInOptionalScope && !bInChoiceScope);
                    }
                    else throw new InvalidPatternStringException(string.Format("Expression incomplete for attribute at index {0} of pattern string {1}", location, patternString));
                }
                else if (c == '*') { bInfinite = true; characterCounter++; }
                else if (c == '+')
                {
                    if (bInfinite == true) throw new InvalidPatternStringException(string.Format("Invalid attribute definition {0} at index {1} of pattern string \"{2}\": repeated attribute matches can have only one element", patternString.Substring(location - characterCounter, characterCounter), location, patternString));
                    else
                    {
                        // Validate attribute
                        int lastPlusSignIndex = patternString.LastIndexOf('+', location - 1);
                        int attributeLength = location - 1 - lastPlusSignIndex - 1;
                        string tempAttribtue = patternString.Substring(lastPlusSignIndex, attributeLength);
                        WordAttribute testAttribute;
                        if (Enum.TryParse(tempAttribtue, out testAttribute))
                            if (Enum.IsDefined(typeof(WordAttribute), testAttribute) | testAttribute.ToString().Contains(","))
                                characterCounter++;
                            else
                                throw new InvalidPatternStringException(string.Format("Invalid attribute definition {0} at index {1} of pattern string \"{2}\": attribute symbol undefined", patternString.Substring(location - characterCounter, characterCounter), location, patternString));
                        else
                            throw new InvalidPatternStringException(string.Format("Invalid attribute definition {0} at index {1} of pattern string \"{2}\": attribute symbol undefined", patternString.Substring(location - characterCounter, characterCounter), location, patternString));
                    }
                }
                else throw new InvalidPatternStringException(string.Format("Invalid symbol \'{0}\' at index {1} of pattern string \"{2}\"", c, location, patternString));

                location++;
            }

            return null;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="patternString"></param>
        /// <param name="location"></param>
        /// <param name="parentPattern">The defining pattern current subpattern is being referenced by</param>
        /// <returns></returns>
        private PatternElement ExtractSubPattern(string patternString, ref int location, Pattern parentPattern)
        {
            int characterCounter = 0; // Count characters that make up a word
            bool bEndingSeperator = false;
            while (location <= patternString.Length)
            {
                char c = patternString[location];   // Analyze current character

                if (c == '\"')
                {
                    if (bEndingSeperator)   // Scoping check
                    {
                        string subPatternName = patternString.Substring(location - characterCounter, characterCounter); // Extract pattern name
                        if(parentPattern != null && parentPattern.Name == subPatternName)  // Recursive pattern definition
                        {
                            if(bInOptionalScope || bInChoiceScope) return new PatternElement(parentPattern, bInOptionalScope);
                            else throw new InvalidPatternStringException(string.Format("Recursive subpattern must be optional (or defined as a choice option) at index {1} of pattern string \"{2}\".", subPatternName, location - characterCounter, patternString));
                        }
                        else if (RecognizedPatterns.ContainsKey(subPatternName))    // Subpattern definition
                        {
                            characterCounter = 0;
                            bInSubPatternNameScope = false;
                            return new PatternElement(RecognizedPatterns[subPatternName], bInOptionalScope && !bInChoiceScope);
                        }
                        else // Invalid pattern definition
                        {
                            throw new InvalidPatternStringException(string.Format("Expression refers to undefined subpattern \'{0}\' at index {1} of pattern string \"{2}\"", subPatternName, location - characterCounter, patternString));
                        }
                    }
                    else
                    {
                        bEndingSeperator = true;
                    }
                }
                else characterCounter++;    // Pattern name can contain anything except "\""

                location++; // Continue extracting pattern name
            }
            return null;
        }
        private PatternElement ExtractChoice(string patternString, ref int location, Pattern parentPattern)
        {
            bool bEndingSeperator = false;
            while (location <= patternString.Length)
            {
                char c = patternString[location];

                if (c == '[')
                {
                    if (bEndingSeperator) { throw new InvalidPatternStringException(string.Format("Invalid double occurence of \'{0}\' at index {1} of pattern string \"{2}\"", c, location, patternString)); }
                    else { bEndingSeperator = true; location++; continue; }
                }
                else if (c == '|')
                {
                    location++;
                    continue;
                }
                else if (c == '(')
                {
                    throw new InvalidPatternStringException(string.Format("Invalid use of optional element definition at index {0} of pattern string {1}: choice options don't support optional because they are optional by default", location, patternString));
                }
                else if (c == ')')
                {
                    throw new InvalidPatternStringException(string.Format("Invalid use of optional element definition at index {0} of pattern string {1}: choice options don't support optional because they are optional by default", location, patternString));
                }
                else if (c == '\'' || englishCharacters.Contains(Char.ToLower(c)))
                {
                    if(c == '\'') bInWordPhraseScope = true;
                    PatternElement extractedElement = ExtractSpecificWord(patternString, ref location);
                    if (extractedElement != null)
                    {
                        choiceCandidates.Add(extractedElement);
                    }
                    else throw new InvalidPatternStringException(string.Format("Invalid state definition inside PatternExtractionState. SpecificWord inside choice at index {0} of pattern string {1}: this is a program logic error", location, patternString));
                }
                else if (c == '\"')
                {
                    bInSubPatternNameScope = true;
                    PatternElement extractedElement = ExtractSubPattern(patternString, ref location, parentPattern);
                    if (extractedElement != null)
                    {
                        choiceCandidates.Add(extractedElement);
                    }
                    else throw new InvalidPatternStringException(string.Format("Invalid state definition inside PatternExtractionState.SubPattern inside choice at index {0} of pattern string {1}: this is a program logic error", location, patternString));
                }
                else if (c == '~')
                {
                    PatternElement extractedElement = ExtractVarietyWord(patternString, ref location);
                    if (extractedElement != null)
                    {
                        choiceCandidates.Add(extractedElement);
                    }
                    else throw new InvalidPatternStringException(string.Format("Invalid state definition inside PatternExtractionState.VaerietyWord inside choice at index {0} of pattern string {1}: this is a program logic error", location, patternString));
                }
                else if (c == '<')
                {
                    bInAttributeScope = true;
                    PatternElement extractedElement = ExtractWordAttribute(patternString, ref location);
                    if (extractedElement != null)
                    {
                        choiceCandidates.Add(extractedElement);
                    }
                    else throw new InvalidPatternStringException(string.Format("Invalid state definition inside PatternExtractionState.WordAttribute at index {0} of pattern string {1}: this is a program logic error", location, patternString));
                }
                else if (c == ']')
                {
                    if (bInAttributeScope || bInSubPatternNameScope || bInWordPhraseScope) throw new InvalidPatternStringException(string.Format("Invalid choice definition at index {0} of pattern string {1}: incomplete scope detected", location, patternString));
                    bInChoiceScope = false;
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
                    PatternElement extractedElement = ExtractCategoryInclusive(patternString, ref location);
                    if (extractedElement != null)
                    {
                        choiceCandidates.Add(extractedElement);
                    }
                    else throw new InvalidPatternStringException(string.Format("Invalid state definition inside PatternExtractionState.CategoryInclusive inside choice at index {0} of pattern string {1}: this is a program logic error", location, patternString));
                }
                else if (c == '#')
                {
                    PatternElement extractedElement = ExtractTag(patternString, ref location);
                    if (extractedElement != null)
                    {
                        choiceCandidates.Add(extractedElement);
                    }
                    else throw new InvalidPatternStringException(string.Format("Invalid state definition inside PatternExtractionState.Tag inside choice at index {0} of pattern string {1}: this is a program logic error", location, patternString));
                }
                else if (c == '!')
                {
                    PatternElement extractedElement = ExtractPendingCategoryExclusive(patternString, ref location);
                    if (extractedElement != null)
                    {
                        choiceCandidates.Add(extractedElement);
                    }
                    else throw new InvalidPatternStringException(string.Format("Invalid state definition inside PatternExtractionState.PendingCategoryExclusive inside choice at index {0} of pattern string {1}: this is a program logic error", location, patternString));
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
                if ((location == patternString.Length - 1) && (bInOptionalScope || bInWordPhraseScope || bInSubPatternNameScope || bInChoiceScope || bInAttributeScope))
                {
                    throw new InvalidPatternStringException(string.Format("Unbalanced scope definition at end of line in pattern string {0}", patternString));
                }

                location++;
            }

            return null;
        }
        private PatternElement ExtractTag(string patternString, ref int location)
        {
            int characterCounter = 0; // Count characters that make up a word
            while (location <= patternString.Length)
            {
                char c = patternString[location];

                if (englishCharacters.Contains(Char.ToLower(c)))
                {
                    characterCounter++;
                    if (location == patternString.Length - 1) return new PatternElement(PatternElementType.Tag, patternString.Substring(location - characterCounter, characterCounter), true);
                }
                else if (c == ' ' || punctuationCharacters.Contains(c) || (bInChoiceScope && c == '|' || c == ']'))   // '|' for choice elements
                {
                    if (characterCounter != 0)
                    {
                        if (bInOptionalScope && !bInChoiceScope)
                        {
                            throw new InvalidPatternStringException(string.Format("Invalid tag definition {0} at index {1} of pattern string \"{2}\": unbalanced () scope", patternString.Substring(location - characterCounter, characterCounter), location, patternString));
                        }
                        else
                        {
                            PatternElement element = new PatternElement(PatternElementType.Tag, patternString.Substring(location - characterCounter, characterCounter), false);
                            location--;
                            return element;
                        }
                    }
                    else throw new InvalidPatternStringException(string.Format("Expression incomplete for tag at index {0} of pattern string {1}", location, patternString));
                }
                else if (c == ')')
                {
                    if (bInOptionalScope && !bInChoiceScope)
                    {
                        bInOptionalScope = false;
                        return new PatternElement(PatternElementType.Tag, patternString.Substring(location - characterCounter, characterCounter), true);
                    }
                    else throw new InvalidPatternStringException(string.Format("Invalid tag definition {0} at index {1} of pattern string \"{2}\": unbalanced () scope", patternString.Substring(location - characterCounter, characterCounter), location, patternString));
                }
                else throw new InvalidPatternStringException(string.Format("Invalid symbol \'{0}\' at index {1} of pattern string \"{2}\"", c, location, patternString));

                location++;
            }

            return null;
        }
        private PatternElement ExtractCategoryInclusive(string patternString, ref int location)
        {
            int characterCounter = 0; // Count characters that make up a word
            while (location <= patternString.Length)
            {
                char c = patternString[location];

                if(c == '@') { location++; continue; }
                else if (englishCharacters.Contains(Char.ToLower(c)))
                {
                    characterCounter++;
                    if (location == patternString.Length - 1) return new PatternElement(PatternElementType.CategoryInclude, patternString.Substring(location - characterCounter, characterCounter), true);
                }
                else if (c == ' ' || punctuationCharacters.Contains(c) || (bInChoiceScope && c == '|' || c == ']'))   // '|' for choice elements
                {
                    if (characterCounter != 0)
                    {
                        if (bInOptionalScope && !bInChoiceScope)
                        {
                            throw new InvalidPatternStringException(string.Format("Invalid category definition {0} at index {1} of pattern string \"{2}\": unbalanced () scope", patternString.Substring(location - characterCounter, characterCounter), location, patternString));
                        }
                        else
                        {
                            PatternElement element = new PatternElement(PatternElementType.CategoryInclude, patternString.Substring(location - characterCounter, characterCounter), false);
                            location--;  // Don't consume the location that doesn't belong to us
                            return element;
                        }
                    }
                    else throw new InvalidPatternStringException(string.Format("Expression incomplete for category at index {0} of pattern string {1}", location, patternString));
                }
                else if (c == ')')
                {
                    if (bInOptionalScope && !bInChoiceScope)
                    {
                        bInOptionalScope = false;
                        return new PatternElement(PatternElementType.CategoryInclude, patternString.Substring(location - characterCounter, characterCounter), true);
                    }
                    else throw new InvalidPatternStringException(string.Format("Invalid category definition {0} at index {1} of pattern string \"{2}\": unbalanced () scope", patternString.Substring(location - characterCounter, characterCounter), location, patternString));
                }
                else throw new InvalidPatternStringException(string.Format("Invalid symbol \'{0}\' at index {1} of pattern string \"{2}\"", c, location, patternString));

                location++;
            }

            return null;
        }
        private PatternElement ExtractPendingUnknown(string patternString, ref int location)
        {
            int characterCounter = 0; // Count characters that make up a word
            while (location <= patternString.Length)
            {
                char c = patternString[location];

                if (c == '?')
                {
                    characterCounter++;
                }
                // Ending condition: only if encounter white space, end of line, or other breaks to handle situations like ???? (redundant ? symbols)
                else if (c == ' ' || (location == patternString.Length - 1))
                {
                    if (bInOptionalScope && !bInChoiceScope) throw new InvalidPatternStringException(string.Format("Invalid unknown phrase definition {0} at index {1} of pattern string \"{2}\": unbalanced () scope", patternString.Substring(location - characterCounter, characterCounter), location, patternString));
                    if (characterCounter == 3)
                    {
                        PatternElement element = new PatternElement(PatternElementType.UnknownPhrase, "???", false);
                        if (!(location == patternString.Length - 1)) location--;    // Don't consume what doesn't belong to us
                        return element;
                    }
                    else return null;
                }
                else if (c == ')' && characterCounter == 3)
                {
                    if (bInOptionalScope && !bInChoiceScope)
                    {
                        return new PatternElement(PatternElementType.UnknownPhrase, "???", true);
                    }
                    else throw new InvalidPatternStringException(string.Format("Invalid unknown phrase definition {0} at index {1} of pattern string \"{2}\": cannot find beginning of () scope", patternString.Substring(location - characterCounter, characterCounter), location, patternString));
                }
                else
                {
                    if (c == '?') throw new InvalidPatternStringException(string.Format("Invalid definition of unknown symbol ??? at index {0} of pattern string {1}: used more '?'", location, patternString));
                    else if (c == ')' && characterCounter == 1) return null;    // A normal optional question mark
                    else throw new InvalidPatternStringException(string.Format("Invalid definition of unknown symbol ??? at index {0} of pattern string {1}: possibly missing a space as seperator", location, patternString));
                }

                location++;
            }
            return null;
        }
        private PatternElement ExtractPendingCategoryExclusive(string patternString, ref int location)
        {
            int characterCounter = 0; // Count characters that make up a word
            while (location <= patternString.Length)
            {
                char c = patternString[location];

                if (c == '@' && characterCounter == 0)
                {
                    // Safe and continue
                    location++;
                    continue;
                }
                else if (englishCharacters.Contains(Char.ToLower(c)))
                {
                    characterCounter++;
                    if (location == patternString.Length - 1) return new PatternElement(PatternElementType.CategoryExclude, patternString.Substring(location - characterCounter, characterCounter), true);
                }
                else if (c == ' ' || punctuationCharacters.Contains(c) || (bInChoiceScope && c == '|' || c == ']'))   // '|' for choice elements
                {
                    if (characterCounter != 0)
                    {
                        if (bInOptionalScope && !bInChoiceScope)
                        {
                            throw new InvalidPatternStringException(string.Format("Invalid category definition {0} at index {1} of pattern string \"{2}\": unbalanced () scope", patternString.Substring(location - characterCounter, characterCounter), location, patternString));
                        }
                        else
                        {
                            PatternElement element = new PatternElement(PatternElementType.CategoryExclude, patternString.Substring(location - characterCounter, characterCounter), false);
                            location--;    // Don't consume what doesn't belong to us
                            return element;
                        }
                    }
                    else throw new InvalidPatternStringException(string.Format("Expression incomplete for category at index {0} of pattern string {1}", location, patternString));
                }
                else if (c == ')')
                {
                    if (bInOptionalScope && !bInChoiceScope)
                    {
                        bInOptionalScope = false;
                        return new PatternElement(PatternElementType.CategoryExclude, patternString.Substring(location - characterCounter, characterCounter), true);
                    }
                    else throw new InvalidPatternStringException(string.Format("Invalid category definition {0} at index {1} of pattern string \"{2}\": unbalanced () scope", patternString.Substring(location - characterCounter, characterCounter), location, patternString));
                }
                else throw new InvalidPatternStringException(string.Format("Invalid symbol \'{0}\' at index {1} of pattern string \"{2}\"", c, location, patternString));

                location++;
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
