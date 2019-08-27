using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Airi.TheVoice
{
    /// <summary>
    /// TheVoice has synthesization and recognition components; TheVoice isn't targeted to be light weight but accurate and realistic and in real-time
    /// Synthesization: 
    ///     1. Providing natural text language based formatted text input (natural language tense, using BOLD, Italic, Underline and punctuations) for speech thethesization (Eng + Chn)
    ///     2. PinYin or Just characters based enouncition
    /// Recognition:
    ///     1. Programmable state-machine based reocnition pattern with callback for practical applications
    ///     2. General purpose recognition with background noise recognition and cancellinig (incluidng system's play sound)
    ///     3. Customization support
    /// Design Reference: MS speech API, Voiceloid engine III
    /// </summary>
    class Core
    {
    }
}
