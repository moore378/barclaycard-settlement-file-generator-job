using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AutoDatabase
{
    public class PascalCaseToUnderscoreConverter : IStoredProcNameConverter
    {
        /// <summary>
        /// Matches a string of words, where a word is in the form XX or Xxx where X is an uppercase char and xx is one or more of lowercase alphanumeric char.
        /// </summary>
        static Regex pattern = new Regex("^([A-Z]([a-z0-9]+|[A-Z]|))+$");

        /// <summary>
        /// Converts C# camel-case to uppercase under-score separated names
        /// </summary>
        /// <remarks>
        /// SelSomething -> SEL_SOMETHING;
        /// SelIDNumber -> SEL_ID_NUMBER; Note this only works for 2-letter abreviations, as per C# recommended conventions
        /// SelANumber -> SEL_A_NUMBER
        /// SelAN -> SEL_AN
        /// SelGUIDNumber -> SEL_GU_ID_NUMBER; Note that SelGUIDNumber should actually be SelGuidNumber to become SEL_GUID_NUMBER
        /// Id5Number -> ID5_NUMBER
        /// AD5Number -> A_D5_NUMBER; Note that "5" can only occur where a lower-case would have occurred
        /// </remarks>
        public string ConvertToStoredProc(string functionName)
        {
            Match match = pattern.Match(functionName);
            if (!match.Success)
                throw new InvalidOperationException("\"" + functionName + "\" is not a convertible CamelCase name. Consider renaming or providing the name explicitly.");
            // The first group is the whole match, the second is word match
            return match.Groups[1].Captures.Cast<Capture>().Select(c => c.Value).Aggregate((a, s) => a + "_" + s).ToUpper();
        }
    }
}
