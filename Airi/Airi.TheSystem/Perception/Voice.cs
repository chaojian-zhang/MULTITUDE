using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech.Recognition;
using System.Speech.Synthesis;

namespace Airi.TheSystem.Perception
{
    /// <summary>
    /// Provides facilities for recognizing inputs speeches, and converting outputs into speeches; 
    /// This module isn't directly related to core aspects of Airi due to layer of abstraction so
    /// might be put into a seperate assembly when appropriate
    /// Speeches are identified for cancelling background information and for speaker specific memories
    /// The overall Voice perception design needs further elaboration currently it's not well seperated from depedent libraries
    /// </summary>
    /// <spec>
    /// Current implementation has two prerequisites:
    /// - Win8+ for System.Speech support
    /// - Female voice installation
    /// </spec>
    /// <Development> Unpublic this class
    public class Voice
    {
        public enum UpdateMode
        {
            Replace,    // Replace all existing grammars
            Append        // Append to current grammars
        }

        // The tone of speech, used for advanced humanoid speech simulation
        // Used to represent emotions
        // Well didn't I mention we don't have an "angry" emotion
        public enum SpeechTone
        {
            Normal,
            Joyous,
            Naughty,
            Soft
        }

        // The result of speech recognition
        public enum SpeechStatus
        {
            Recognized,
            /*Boomark,*/ // Not used because we could just manually do things we need at the caller
            Rejected
        }

        // Voice engine components
        private SpeechRecognitionEngine SpeechRecEngine = new SpeechRecognitionEngine();
        SpeechSynthesizer SpeechSynthesizer = new SpeechSynthesizer();

        // Envent Handling
        Grammar newGrammar = null;
        UpdateMode mode;
        public delegate void SpeechEventDelegate(SpeechStatus status, List<string> messages);
        SpeechEventDelegate SpeechHandler;

        public Voice()
        {
            // Initialize Speech Engine
            SpeechRecEngine.SetInputToDefaultAudioDevice(); // Set up input device
            // Default Grammar
            SpeechRecEngine.LoadGrammar(new DictationGrammar() { Name = "Default Grammar" });
            // Set up handlers
            SpeechRecEngine.SpeechRecognized += SpeechRecEngine_SpeechRecognized;
            SpeechRecEngine.RecognizerUpdateReached += SpeechRecEngine_RecognizerUpdateReached;
            SpeechRecEngine.SpeechRecognitionRejected += SpeechRecEngine_SpeechRecognitionRejected;
            // Activate it by default
            SpeechRecEngine.RecognizeAsync(RecognizeMode.Multiple);
        }

        public void Activate()
        {
            SpeechRecEngine.RecognizeAsync(RecognizeMode.Multiple);
        }

        public void Deactivate()
        {
            SpeechRecEngine.RecognizeAsyncStop();
        }

        ~Voice()
        {
            // Deactivate and dispose
            SpeechRecEngine.RecognizeAsyncStop();
            SpeechRecEngine.Dispose();
            SpeechSynthesizer.Dispose();
        }

        // Replace set of recognizable commands for next action
        // Only smallest subset of grammar will be active at a time to ensure accuracy; If speech engine can update resonably fast we might consider dynamically update grammar depending on previous speeches
        public void updateGrammar(List<string> choices, SpeechEventDelegate handler)
        {
            // Create a new grammar for the engine
            /// Gramar defines how speeches are defined
            Choices elements = new Choices();
            elements.Add(choices.ToArray());
            GrammarBuilder builder = new GrammarBuilder(); // Interface for setting up speech engine
            builder.Append(elements);
            Grammar grammar = new Grammar(builder);
            grammar.Name = "Commands";

            // Setup engine
            SpeechHandler = handler;
            newGrammar = grammar;
            mode = UpdateMode.Replace;
            SpeechRecEngine.RequestRecognizerUpdate();
        }

        //// <Improve> Incomplete implementation
        //// Complicated recognizers constructor per pattern recognization, this should ideally be implemented in Airi core
        //public void ConstructChoice()
        //{
        //    throw new NotImplementedException();
        //}

        // Add set of recognizable commands for next action
        public void AddGrammar(List<string> choices, SpeechEventDelegate handler)
        {
            // Create a new grammar for the engine
            /// Gramar defines how speeches are defined
            Choices commands = new Choices();
            commands.Add(choices.ToArray());
            GrammarBuilder builder = new GrammarBuilder(); // Interface for setting up speech engine
            builder.Append(commands);
            Grammar grammar = new Grammar(builder);
            grammar.Name = "Commands";

            // Setup engine
            SpeechHandler = handler;
            newGrammar = grammar;
            mode = UpdateMode.Append;
            SpeechRecEngine.RequestRecognizerUpdate();
        }

        /// <summary>
        /// Synthesize simple sounds for testing purpose
        /// </summary>
        /// <param name="content"></param>
        public void SynthesizeSpeech(string content)
        {
            SpeechSynthesizer.Speak(content);
        }

        // Say something
        // We will use human-recoded voices for more realistic speech generation purpose
        // Such speeches are designer-specifed voice cues rather than synsthesization based methods, used specifically for specific responses
        /// <summary>
        /// Speak some sentence in a natural tone using recorded voices
        /// </summary>
        /// <param name="content"></param>
        /// <param name="tone"></param>
        public void ProduceSpeech(string content, SpeechTone tone)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Synthesize more elaborated speeches using prompts
        /// </summary>
        /// <param name="sentences"></param>
        // <Improvement> Incomplete implementation
        // Automatically determine proper output generation so Core doesn't need to bother because anyway Voice facility isn't part of Core
        // Utilize PromptRate, PromptVolume, PromptEmphasis, Voice, and Pause(break) when appropriate, according to sentence structure, tone, phrase type (Content type, SayAa()) and other information
        public void BuildSpeech(List<Tuple<string, SpeechTone>> sentences)
        {
            PromptBuilder builder = new PromptBuilder();
            builder.StartVoice(VoiceGender.Female, VoiceAge.Adult);

            foreach (Tuple<string, SpeechTone> sentence in sentences)
            {
                builder.StartSentence();
                switch (sentence.Item2)
                {
                    case SpeechTone.Normal:
                        builder.AppendText(sentence.Item1);
                        break;
                    case SpeechTone.Joyous:
                        builder.AppendText(sentence.Item1);
                        break;
                    case SpeechTone.Naughty:
                        builder.AppendText(sentence.Item1);
                        break;
                    case SpeechTone.Soft:
                        builder.StartStyle(new PromptStyle(PromptVolume.ExtraSoft));
                        builder.AppendText(sentence.Item1);
                        builder.EndStyle();
                        break;
                    default:
                        break;
                }
                builder.EndSentence();
            }

            builder.EndVoice();

            SpeechSynthesizer.Speak(builder);
        }

        private void SpeechRecEngine_RecognizerUpdateReached(object sender, RecognizerUpdateReachedEventArgs e)
        {
            switch (mode)
            {
                case UpdateMode.Replace:
                    SpeechRecEngine.UnloadAllGrammars();
                    SpeechRecEngine.LoadGrammarAsync(newGrammar);  // Set up new contents to recognize
                    break;
                case UpdateMode.Append:
                    SpeechRecEngine.LoadGrammar(newGrammar);
                    break;
                default:
                    break;
            }
        }

        private void SpeechRecEngine_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            // Auxiliary (Augmented Realism) Parameters
            int SpeakerIdentification;  // An unique id for specific sound produced by one human speaker
            float SpeachStrength;   // The loudness level of incoming speach so Airi can adjust her voice accordingly

            if (SpeechHandler != null && e.Result.Confidence > 0.85)
                SpeechHandler(SpeechStatus.Recognized, new List<string>() { e.Result.Text });    // Do we need to check it's not null?
            // Might want to pass more useful data as well
        }

        private void SpeechRecEngine_SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            if(e.Result.Alternates.Count >= 0)
            {
                List<string> alternatives = e.Result.Alternates.Select(phrase => phrase.Text).ToList();
                SpeechHandler(SpeechStatus.Rejected, alternatives);
            }
        }
    }
}
