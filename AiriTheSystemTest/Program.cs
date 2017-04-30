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
            // Test 1: SpeakingTest();
            // Test 2: VocabularyAndPatternTest();
            VocabularyAndPatternTest();
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
                Console.Write("Airi: ");
                if (replies == null)
                {
                    Console.Write("\n");
                    continue;
                }
                foreach (string reply in replies)
                {
                    Console.WriteLine(reply);
                }
            }
        }
    }
}
