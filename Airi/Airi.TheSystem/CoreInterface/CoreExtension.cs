using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Airi.TheSystem
{
    // Provides similar input handling and memory, but differs in reply choice; Might restrict used themes
    public abstract class MentalState
    {
        public abstract void Respond(string input);
    }

    // More direct, and slower --> Not necessarily, this might actually be faster
    /// <summary>
    /// Rational mental state that respondes to inputs in an informative way, useful for handling common tasks and intelligent processing of information
    /// Not personal memory is accumulated during this process and as a matter of fact this is considered the "unconscious" state of Airi
    /// Consider making it just a swtich, or maybe a class is necessary
    /// </summary>
    public class PassiveState : MentalState   // Other names: InformativeMentalState, RationalMentalState
    {
        public override void Respond(string input)
        {
            throw new NotImplementedException();
        }
    }

    // Focus less on collecting information, thoughts more diverse
    /// <summary>
    /// A more active and diverse way of thinking enableing Airi to have self-awareness and accumulate experience and continuously learn
    /// "Emotions" or other forms of perceptions are fully activated
    /// </summary>
    public class ActiveState : MentalState  // Other names: EmotionalMentalState
    {
        public override void Respond(string input)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// A mental state enabling creative thinking and guided-random organization of memory
    /// </summary>
    public class DreamState : MentalState
    {
        public override void Respond(string input)
        {
            throw new NotImplementedException();
        }
    }

    // An Exception that is raised when destinated memory file for Airi doesn't exist yet user expect one
    [Serializable]
    internal class MemoryFileNotExistException : Exception
    {
        public MemoryFileNotExistException() { }
        public MemoryFileNotExistException(string message) : base(message) { }
        public MemoryFileNotExistException(string message, Exception inner) : base(message, inner) { }
        protected MemoryFileNotExistException(
          global::System.Runtime.Serialization.SerializationInfo info,
          global::System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// Encapsulates a context for identifying current Airi thread
    /// </summary>
    internal class Context
    {

    }

    // Helpers
    // Notice to enable portability and cleaness of source code we used string resources for this project:
    // https://msdn.microsoft.com/en-CA/library/3xhwfctz(v=vs.100).aspx
    // https://msdn.microsoft.com/en-us/library/d17ax2xk(v=vs.110).aspx
    // http://stackoverflow.com/questions/8982366/c-sharp-fastest-way-to-get-resource-string-from-assembly
}
