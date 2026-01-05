using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Airi.TheSystem;
using System.IO;
using Airi.TheSystem.Memory;

namespace AiriTheSystemTest
{
    class Program
    {
        static void Main(string[] args)
        {
            // Test 1: Simple
            // SpeakingTest();
            // Test 2: Manual
            // VocabularyAndPatternTest();
            // Test 3: Auto + Manual
            // VocabularyAndPatternUnitTest();
            VocabularyAndPatternInternalUnitTest();
            VocabularyAndPatternTest();
            // Test 4: Comprehension
            ComprehensionTest();
        }

        private static void ComprehensionTest()
        {
            // Get story to read
            Console.Write("Enter path to file: ");
            string filePath = Console.ReadLine();
            string storyContent = File.ReadAllText(filePath);

            // Create a new Airi System
            TheSystem Airi = new TheSystem();
            Airi.ReadStory(storyContent);

            // Questions and answers
            while (true)
            {
                Console.Write("Q: ");
                Console.WriteLine("A: " + Airi.AskStory(Console.ReadLine()));
            }
        }

        private static void VocabularyAndPatternInternalUnitTest()
        {
            // Create a new Airi System
            TheSystem Airi = new TheSystem();

            Airi.SpeakNativeUnitTest();
        }

        static void SpeakingTest()
        {
            // Create a new Airi System
            TheSystem Airi = new TheSystem();
            // Airi.Learn(@"H:\P Projects\按项目分类 - 执行和创造用\-- Productions\SIS\AiriTheSystem_Training Materials\冰与火之歌1.txt");
            foreach (string path in Directory.EnumerateFiles(@"H:\P Projects\按项目分类 - 执行和创造用\-- Productions\SIS\AiriTheSystem_Training Materials\Friends"))
            {
                Airi.Learn(path);
            }
            // Task<List<string>> speakTask = Task<List<string>>.Factory.StartNew(() => Airi.Speak("What is your name"));
            // https://msdn.microsoft.com/en-us/library/dd537613(v=vs.110).aspx
            // http://stackoverflow.com/questions/25191512/async-await-return-task
            Console.WriteLine("Enter Text to communicate...");

            while (true)
            {
                Console.Write("Me: ");
                List<string> reply = Airi.Speak(Console.ReadLine(), "Passenger");
                Console.Write("Airi: ");
                Console.WriteLine(reply[0]);
            }
        }

        static void VocabularyAndPatternTest()
        {
            // Create a new Airi System
            TheSystem Airi = new TheSystem();

            // Don't learn from auxiliary materials, but focus on basic understanding of Airi
            Console.WriteLine("Enter Text to communicate...");

            while (true)
            {
                Console.Write("Me: ");
                List<string> replies = Airi.SpeakNative(Console.ReadLine(), "Passenger");
                if (replies == null)
                {
                    Console.Write("\n");
                    continue;
                }
                Console.Write("Airi: ");
                foreach (string reply in replies)
                {
                    Console.WriteLine(reply);
                }
            }
        }

        static void VocabularyAndPatternUnitTest()
        {
            // Create a new Airi System
            TheSystem Airi = new TheSystem();

            // Test Cases
            VocabularyAndPatternUnitTestHelper(Airi, "is", @"1: Specifics of pattern [Be]
Signature: [be|is|was|are|were]
Parameters: is
"); // "Be"
            VocabularyAndPatternUnitTestHelper(Airi, "have lunch", @"1: Specifics of pattern [Verb phrase]
Signature: [do|<verb>] <noun>
Parameters: have lunch
");   // "Verb phrase"
            VocabularyAndPatternUnitTestHelper(Airi, "do sport", @"1: Specifics of pattern [Verb phrase]
Signature: [do|<verb>] <noun>
Parameters: do sport
");   // "Verb phrase"
            VocabularyAndPatternUnitTestHelper(Airi, "in", @"1: Specifics of pattern [Spatial prep]
Signature: [on|above|in|below|behind|inside]
Parameters: in
");   // "Spatial prep"
            VocabularyAndPatternUnitTestHelper(Airi, "deliciously cooked dinner", @"1: Specifics of pattern [Descriptive Component]
Signature: (<art>) (<*adv>) (<*adj>) <noun>
Parameters: deliciously cooked dinner
");   // "Descriptive Component"
            VocabularyAndPatternUnitTestHelper(Airi, "apple is fruit", @"1: Specifics of pattern [is Construct]
Signature: <noun> ""Be"" <noun>
Parameters: apple is fruit
");   // " is Construct"
            VocabularyAndPatternUnitTestHelper(Airi, "carpet is red", @"1: Specifics of pattern [is Construct]
Signature: <noun> ""Be"" <noun>
Parameters: carpet is red
2: Specifics of pattern [state Construct]
Signature: <noun> ""Be"" <adj>
Parameters: carpet is red
");   // "state Construct"
            VocabularyAndPatternUnitTestHelper(Airi, "computer on desk", @"1: Specifics of pattern [on Constrct]
Signature: <noun> ""Spatial prep"" <noun>
Parameters: computer on desk
");   // "on Construct"
            VocabularyAndPatternUnitTestHelper(Airi, "friend of me", @"1: Specifics of pattern [of Constrct]
Signature: <noun> of <noun>
Parameters: friend of me
");   // "of Construct"
            VocabularyAndPatternUnitTestHelper(Airi, "London is west to Toronto", @"1: Specifics of pattern [Direction Construct]
Signature: <noun> ""Be"" [north|south|east|west|near|close] to <noun>
Parameters: london is west to toronto
");   // "Direction Construct"
            VocabularyAndPatternUnitTestHelper(Airi, "What is happiness", @"1: Specifics of pattern [Curiosity Quest Certain]
Signature: What is <noun>
Parameters: What is happiness
");   // "Curiosity Quest Certain"
            VocabularyAndPatternUnitTestHelper(Airi, "What does computer aided design mean?", "");   // "Curiosity Quest Uncertain"
            VocabularyAndPatternUnitTestHelper(Airi, "Hi", @"1: Specifics of pattern [Descriptive Component]
Signature: (<art>) (<*adv>) (<*adj>) <noun>
Parameters: hi
2: Specifics of pattern [Interruption 1]
Signature: [Hi|Hey|Sorry]
Parameters: Hi
3: Specifics of pattern [Courtesy Interrupt]
Signature: [Interruption 1|Interruption 2|Interruption 3]
Parameters: Hi
");   // "Interruption 1"
            VocabularyAndPatternUnitTestHelper(Airi, "Excuse me", @"1: Specifics of pattern [Verb phrase]
Signature: [do|<verb>] <noun>
Parameters: excuse me
2: Specifics of pattern [Interruption 2]
Signature: Excuse (me)
Parameters: Excuse me
3: Specifics of pattern [Courtesy Interrupt]
Signature: [Interruption 1|Interruption 2|Interruption 3]
Parameters: Excuse me
");   // "Interruption 2"
            VocabularyAndPatternUnitTestHelper(Airi, "Sorry to bother you", "");   // "Interruption 3" // "Courtesy Interrupt"
            VocabularyAndPatternUnitTestHelper(Airi, "Evening", @"1: Specifics of pattern [Descriptive Component]
Signature: (<art>) (<*adv>) (<*adj>) <noun>
Parameters: evening
2: Specifics of pattern [Time]
Signature: [Morning|Afternoon|Evening|Night]
Parameters: Evening
");   // "Time"
            VocabularyAndPatternUnitTestHelper(Airi, "Good Night", @"1: Specifics of pattern [Verb phrase]
Signature: [do|<verb>] <noun>
Parameters: good night
2: Specifics of pattern [Descriptive Component]
Signature: (<art>) (<*adv>) (<*adj>) <noun>
Parameters: good night
3: Specifics of pattern [Greeting]
Signature: Good ""Time""
Parameters: Good Night
");   // "Greeting"
            VocabularyAndPatternUnitTestHelper(Airi, "Stealth is messy business", "");   // "Definition"
            VocabularyAndPatternUnitTestHelper(Airi, "From now on, Knife means mission", @"1: Specifics of pattern [Advanced language concept]
Signature: ['From now on'|'In the future we will'] (,) (use) <noun> [means|indicates] [<noun>|<verb>]
Parameters: From now on , knife means mission
");   // "Advanced language concept"
            VocabularyAndPatternUnitTestHelper(Airi, "Do you like me?", "");   // "Ask Experience"
            VocabularyAndPatternUnitTestHelper(Airi, "Excuse me, where is London", "");   // "Location Query"
            VocabularyAndPatternUnitTestHelper(Airi, "How is weather", @"1: Specifics of pattern [Weather Query]
Signature: How is weather
Parameters: How is weather
");   // "Weather Query"
            VocabularyAndPatternUnitTestHelper(Airi, "Airi, play Music", "");   // "Airi Run Command"
            VocabularyAndPatternUnitTestHelper(Airi, "Why he escaped from school is what we do not know yet", "");   // "从句表达句"
            // VocabularyAndPatternUnitTestHelper(Airi, "", "");   // ""

            /* Reports:
            1. Notice currently friends, sports etc. are not in the dictionary
            */
            // Courtesy
            // Console.ReadLine();
        }

        static bool VocabularyAndPatternUnitTestHelper(TheSystem Airi, string testString, string expectedOutput)
        {
            // Set up console output stream
            var originalConsoleOut = Console.Out; // preserve the original stream
            using (var writer = new StringWriter())
            {
                Console.SetOut(writer);

                // Do a test output
                Airi.SpeakNative(testString, "Passenger");

                // Get output string from redirected console
                writer.Flush();
                string outputString = writer.GetStringBuilder().ToString();

                // Compared and issue warning/errors
                if (outputString != expectedOutput || outputString == "")
                {
                    Console.SetOut(originalConsoleOut); // restore Console.Out
                    Console.WriteLine("[Error] Failed test: " + testString);
                    return false;
                }

                // Clear stream for next output
                writer.GetStringBuilder().Clear();
            }
            Console.SetOut(originalConsoleOut); // restore Console.Out
            // Console.WriteLine("[Success] Passed test: " + testString);
            return true;
        }
    }
}
