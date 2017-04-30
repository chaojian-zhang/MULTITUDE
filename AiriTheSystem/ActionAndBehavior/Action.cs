using Airi.TheSystem.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Airi.TheSystem.Instruction
{
    /// <summary>
    /// Provides facilities to acess certain aspects of memory by name in response to embeded "instructions"
    /// </summary>
    class Accessor
    {

    }

    /// <summary>
    /// Defines executable behaviros for the system in response to "instructions"
    /// Might encapsulate "Actiavator" to seperate Action's conceptual and behavior aspects
    /// </summary>
    class Action
    {
        // Instantaneous stack memory -- those two are the most frequently used meaning of pronoun
        static Action PreviousAction;
        static string PreviousSubject;

        // Execute the action
        // This should ideally be data driven and use Instructions for specific actions
        // An action might be textual and produce many text outputs as response
        static public List<string> Execute(string ActionName, PatternInstance instance)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Defines time-dependent executable behaviros for the system in response to "instructions"
    /// </summary>
    class Process
    {
    }
}
